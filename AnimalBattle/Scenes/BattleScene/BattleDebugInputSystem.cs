using AnimalBattle.Ai;
using AnimalBattle.Enemies.Services;

namespace AnimalBattle.Scenes.Battle;

/// <summary>
///     World 単位で独立して保持される戦闘デバッグ入力処理システム用コンテキスト。
/// </summary>
internal sealed class BattleDebugInputContext : IDisposable
{
    public required Query<GameplayTagState> PlayerTagStateQuery { get; init; }
    public required Query<Health, BossCombatState, BossBrainComponent> ActiveBossQuery { get; init; }
    public required Query<EnergyGauge> PlayerEnergyQuery { get; init; }
    public Phase TargetDebugPhase;

    public void Dispose()
    {
        PlayerTagStateQuery.Dispose();
        ActiveBossQuery.Dispose();
        PlayerEnergyQuery.Dispose();
    }
}

/// <summary>
///     戦闘シーンにおける開発・検証用デバッグショートカット入力（F1〜F9、ボス・キャラ即時切替）を処理する ECS システム。
///     通常の戦闘進行入力（BattleInputSystem）から責務を完全分離します。
/// </summary>
public static class BattleDebugInputSystem
{
    /// <summary>
    ///     戦闘デバッグ入力システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    public static World AddBattleDebugInputSystem(this World world)
    {
        var playerTagStateQuery = world.QueryBuilder<GameplayTagState>()
            .With<PlayerTag>()
            .Build();
        var activeBossQuery = world.QueryBuilder<Health, BossCombatState, BossBrainComponent>()
            .With<BossTag>()
            .Without<DeadTag>()
            .Build();
        var playerEnergyQuery = world.QueryBuilder<EnergyGauge>()
            .With<PlayerTag>()
            .Without<DeadTag>()
            .Build();

        world.SetDisposable(new BattleDebugInputContext
        {
            PlayerTagStateQuery = playerTagStateQuery,
            ActiveBossQuery = activeBossQuery,
            PlayerEnergyQuery = playerEnergyQuery
        });

        world.System("BattleDebugInputSystem")
            .Kind(Ecs.PreUpdate)
            .Iter(ExecuteBattleDebugInput);

        return world;
    }

    private static void ExecuteBattleDebugInput(Iter it)
    {
        var world = it.World();
        ref readonly var flow = ref world.Get<GameFlow>();
        if (flow.State != GameState.Battle) return;

        var dbgCfg = world.Get<DebugRenderConfig>();
        if (!dbgCfg.DebugMode) return;

        var ctx = world.Get<BattleDebugInputContext>();

        // F1 または H キー：デバッグオーバーレイ・ギズモ表示トグル
        if (InputService.IsButtonPressed(InputButton.KeyF1) || InputService.IsButtonPressed(InputButton.KeyH))
        {
            dbgCfg.IsActive = !dbgCfg.IsActive;
        }

        // F6 キー：地面近接追撃怪のデバッグ生成
        if (InputService.IsButtonPressed(InputButton.KeyF6))
        {
            SpawnDebugMinion(world, MinionType.Ground);
        }

        // F7 キー：空中浮遊弾幕怪のデバッグ生成
        if (InputService.IsButtonPressed(InputButton.KeyF7))
        {
            SpawnDebugMinion(world, MinionType.Flying);
        }

        // F8 キー：プレイヤー無敵状態のトグル切り替え
        if (InputService.IsButtonPressed(InputButton.KeyF8))
        {
            TogglePlayerInvincible(ctx);
        }

        // F9 キー：プレイヤー特殊能力エネルギーを1スロット増加
        if (InputService.IsButtonPressed(InputButton.KeyF9))
        {
            AddDebugEnergy(ctx);
        }

        if (!dbgCfg.IsActive) return;

        // 1/2/3/4 キーによる Boss およびキャラクターの迅速切替
        if (InputService.IsPressed(InputAction.SelectBossSnake))
        {
            GameManager.Current.SwitchToBattle(flow.SelectedPlayer, BossType.BossSnake);
            return;
        }
        if (InputService.IsPressed(InputAction.SelectBossLion))
        {
            GameManager.Current.SwitchToBattle(flow.SelectedPlayer, BossType.BossLion);
            return;
        }
        if (InputService.IsPressed(InputAction.SelectCharacter1))
        {
            GameManager.Current.SwitchToBattle(CharacterType.Cat, flow.SelectedBoss);
            return;
        }
        if (InputService.IsPressed(InputAction.SelectCharacter2))
        {
            GameManager.Current.SwitchToBattle(CharacterType.Bunny, flow.SelectedBoss);
            return;
        }

        // F5 キー：手触り検証用の空シーン BossType.Empty へ即時切替
        if (InputService.IsButtonPressed(InputButton.KeyF5))
        {
            GameManager.Current.SwitchToBattle(flow.SelectedPlayer, BossType.Empty);
            return;
        }

        if (world.Has<GameOverTag>()) return;

        var f2 = InputService.IsButtonPressed(InputButton.KeyF2);
        var f3 = InputService.IsButtonPressed(InputButton.KeyF3);
        var f4 = InputService.IsButtonPressed(InputButton.KeyF4);

        if (f2 || f3 || f4)
        {
            ctx.TargetDebugPhase = f2 ? Phase.Phase1 : (f3 ? Phase.Phase2 : Phase.Phase3);
            ctx.ActiveBossQuery.Each(ApplyDebugPhase);
        }
    }

    private static void ApplyDebugPhase(Entity boss, ref Health hp, ref BossCombatState state, ref BossBrainComponent brainComp)
    {
        if (!boss.IsAlive() || hp.Current <= 0.0f) return;

        var w = boss.CsWorld();
        var targetPhase = w.Get<BattleDebugInputContext>().TargetDebugPhase;
        brainComp.Brain.DebugSetPhase(targetPhase);

        var cfg = BossPrefabs.GetConfig(w, state.BossType);
        var p2 = cfg.Phase2HpThreshold;
        var p3 = cfg.Phase3HpThreshold;
        var ratio = targetPhase switch
        {
            Phase.Phase1 => 1.0f,
            Phase.Phase2 => p2,
            Phase.Phase3 => p3,
            _ => 1.0f
        };
        hp.Current = Math.Clamp((float)Math.Round(hp.Max * ratio), 1.0f, hp.Max);
        Combat.BulletSpawner.ClearAllEnemyBullets(w);
        Combat.MeleeSpawner.ClearAllEnemyMelee(w);
        state.Phase = targetPhase;
        boss.GetMut<CombatProfile>().Phase = targetPhase;
    }

    private static void SpawnDebugMinion(World world, MinionType type)
    {
        var root = world.Lookup(BattleSceneSpawner.RootName);
        if (!root.IsAlive()) return;

        var spawnPos = MinionSpawnService.SelectBestSpawnPoint(world, type);
        Enemies.MinionSpawner.SpawnMinion(world, root, type, spawnPos);
    }

    private static void TogglePlayerInvincible(BattleDebugInputContext ctx)
    {
        ctx.PlayerTagStateQuery.Each(static (ref tagState) =>
        {
            if (tagState.HasTag(GameplayTag.Invincible))
            {
                tagState.RemoveActionTag(GameplayTag.Invincible);
            }
            else
            {
                tagState.AddActionTag(GameplayTag.Invincible);
            }
        });
    }

    private static void AddDebugEnergy(BattleDebugInputContext ctx)
    {
        ctx.PlayerEnergyQuery.Each(static (ref EnergyGauge gauge) =>
        {
            gauge.CurrentEnergy = MathF.Min(gauge.MaxEnergy, gauge.CurrentEnergy + gauge.SegmentCapacity);
        });
    }
}
