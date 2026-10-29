using AnimalBattle.Ai;
using AnimalBattle.Enemies.Services;

namespace AnimalBattle.Scenes.Battle;

/// <summary>
///     World 単位で独立して保持される戦闘入力処理システム用コンテキスト。
/// </summary>
internal sealed class BattleInputSystemContext : IDisposable
{
    public required Query PlayerBulletsQuery { get; init; }
    public required Query VfxQuery { get; init; }
    public required Query PlayerEntitiesQuery { get; init; }
    public required Query<GameplayTagState> PlayerTagStateQuery { get; init; }
    public required Query<Health, BossCombatState, BossBrainComponent> BossQuery { get; init; }
    public required Query<Transform, Sprite> PlayerQuery { get; init; }

    public void Dispose()
    {
        PlayerBulletsQuery.Dispose();
        VfxQuery.Dispose();
        PlayerEntitiesQuery.Dispose();
        PlayerTagStateQuery.Dispose();
        BossQuery.Dispose();
        PlayerQuery.Dispose();
    }
}

/// <summary>
///     戦闘シーンにおける入力受付、自機操作、および戦闘リセット・シーン復帰を司る ECS システム。
///     GameState.Battle 状態時にのみアクティブとなり、PreUpdate フェーズでプレイヤーの入力を処理します。
/// </summary>
public static class BattleInputSystem
{
    private static bool _prevF1;
    private static bool _prevF2;
    private static bool _prevF3;
    private static bool _prevF4;
    private static bool _prevF5;
    private static bool _prevF6;
    private static bool _prevF7;
    private static bool _prevF8;

    private static Phase _targetDebugPhase;

    /// <summary>
    ///     戦闘入力処理システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddBattleInputSystem(this World world)
    {
        var playerBulletsQuery = world.QueryBuilder()
            .With<PlayerBulletTag>()
            .Build();

        var vfxQuery = world.QueryBuilder()
            .With<VfxTag>()
            .Build();

        var playerEntitiesQuery = world.QueryBuilder()
            .With<PlayerTag>()
            .Build();
        var playerTagStateQuery = world.QueryBuilder<GameplayTagState>()
            .With<PlayerTag>()
            .Build();
        var bossQuery = world.QueryBuilder<Health, BossCombatState, BossBrainComponent>()
            .With<BossTag>()
            .Build();
        var playerQuery = world.QueryBuilder<Transform, Sprite>()
            .With<PlayerTag>()
            .Without<DeadTag>()
            .Build();

        world.Set(new BattleInputSystemContext
        {
            PlayerBulletsQuery = playerBulletsQuery,
            VfxQuery = vfxQuery,
            PlayerEntitiesQuery = playerEntitiesQuery,
            PlayerTagStateQuery = playerTagStateQuery,
            BossQuery = bossQuery,
            PlayerQuery = playerQuery
        });

        world.System("BattleInputSystem")
            .Kind(Ecs.PreUpdate)
            .Iter(ExecuteBattleInput);

        return world;
    }

    /// <summary>
    ///     戦闘シーンの入力受付パイプライン。
    /// </summary>
    public static void ExecuteBattleInput(Iter it)
    {
        var w = it.World();
        ref readonly var flow = ref w.Get<GameFlow>();
        if (flow.State != GameState.Battle) return;

        var ctx = w.Get<BattleInputSystemContext>();

        // ESC キーでタイトルまたはキャラクター選択画面へ復帰
        if (InputService.IsPressed(InputAction.Cancel))
        {
            GameManager.Current.SwitchToStart();
            return;
        }

        // R キーで戦闘リセット
        if (InputService.IsPressed(InputAction.Restart))
        {
            ResetCombat(w, ctx);
        }

        // デバッグ入力：F1 トグル、F2〜F4 フェーズ切替、F5 空シーン切替の処理
        var renderCfg = w.Get<RenderConfig>();
        var dbgCfg = renderCfg.Debug;
        if (dbgCfg.DebugMode)
        {
            ProcessDebugInput(w, dbgCfg, ctx);
        }

        // ゲームオーバー時は自機の操作入力を停止：PlayerInputSystem 側でも同様に判定
        if (w.Has<GameOverTag>()) return;
    }

    private static void ResetCombat(World world, BattleInputSystemContext ctx)
    {
        // ゲームオーバー状態の解除：Remove は冪等のため事前の Has チェック不要
        world.Remove<GameOverTag>();

        BulletSpawner.ClearAllBossBulletsImmediate(world);
        MeleeSpawner.ClearAllBossMeleeImmediate(world);
        MinionSpawner.ClearAllMinionsImmediate(world);
        ctx.PlayerBulletsQuery.Each(CombatPoolService.Recycle);
        ctx.VfxQuery.Each(CombatPoolService.Recycle);
        CameraService.Reset();
        world.Set(new AimReticleState());

        ctx.BossQuery.Each(ResetBossEntity);
        ctx.PlayerEntitiesQuery.Each(ResetPlayerEntity);

        EventBus.Publish(new CombatResetEvent());
    }

    internal static void ResetBossEntity(Entity boss, ref Health hp, ref BossCombatState state, ref BossBrainComponent brainComp)
    {
        if (!boss.IsAlive()) return;

        var w = boss.CsWorld();
        var bossType = state.BossType;
        var (spawn, maxHp) = bossType switch {
            BossType.BossSnake => (w.Get<BossSnakeConfig>().Spawn, w.Get<BossSnakeConfig>().MaxHp),
            BossType.BossLion => (w.Get<BossLionConfig>().Spawn, w.Get<BossLionConfig>().MaxHp),
            _ => (Vec2f.Zero, 1000f)
        };
        
        ref var trans = ref boss.GetMut<Transform>();
        trans.Position = spawn;
        trans.Rotation = 0.0f;

        boss.Set(new Velocity(Vec2f.Zero));

        hp.Current = maxHp;
        hp.Max = maxHp;

        brainComp.Brain.Reset(spawn);
        state.Phase = Phase.Phase1;
        state.CurrentSkillTag = StringId.Empty;
        state.StateTimer = 0.0f;
        state.TargetPosition = Vec2f.Zero;
        state.FacingRight = true;

        boss.Set(new GameplayTagState(brainComp.Brain.AbilitySystem.Tags.RawTags));
        boss.Set(new TimedEffectsState());

        // 一時状態・エフェクト・死亡タグの一括安全削除：Flecs ネイティブ API による冪等操作
        boss.Remove<HitstopState>()
            .Remove<SpriteJitter>()
            .Remove<DeadTag>()
            .Remove<HitFlashEffect>()
            .Remove<InvincibleBlinkEffect>();
        
        ref var sprite = ref boss.GetMut<Sprite>();
        sprite.IsVisible = true;
        sprite.JitterOffset = Vec2f.Zero;
    }

    internal static void ResetPlayerEntity(Entity player)
    {
        if (!player.IsAlive()) return;

        var w = player.CsWorld();
        var type = player.Has<CharacterTypeComponent>()
            ? player.Get<CharacterTypeComponent>().Value
            : CharacterType.Cat;
        var cfg = w.GetPlayerConfig(type);

        player.Set(new Transform(cfg.Spawn, cfg.Scale));
        player.Set(new Velocity(Vec2f.Zero));
        player.Set(new Health(cfg.MaxHp));
        player.Set(new MovementIntent());
        player.Set(new WeaponIntent());
        player.Set(new DashState());
        player.Set(JumpCapability.FromPlayerConfig(cfg));
        player.Set(new GameplayTagState());
        player.Set(new TimedEffectsState());
        player.Set(new SpringScale(Vec2f.One, cfg.SpringFrequency, cfg.SpringDampingRatio));

        // 一時状態・エフェクト・死亡タグの一括安全削除：Flecs ネイティブ API による冪等操作
        player.Remove<HitstopState>()
            .Remove<SpriteJitter>()
            .Remove<DeadTag>()
            .Remove<HitFlashEffect>()
            .Remove<InvincibleBlinkEffect>();

        var weaponCfg = w.GetWeaponConfig(cfg.Weapon);
        if (weaponCfg is MeleeWeaponConfig meleeCfg)
        {
            player.Set(MeleeWeaponComponent.FromConfig(meleeCfg));
            var melee = player.Target<EquippedWeapon>();
            if (melee.IsAlive())
            {
                melee.Disable();
                melee.Set(new HitHistory());
            }
        }
        else if (weaponCfg is RangedWeaponConfig rangedCfg)
        {
            player.Set(RangedWeaponComponent.FromConfig(rangedCfg));
        }

        ref var sprite = ref player.GetMut<Sprite>();
        sprite.IsVisible = true;
        sprite.JitterOffset = Vec2f.Zero;
        sprite.Scale = Vec2f.One;
        sprite.SpringOffsetY = 0.0f;
    }

    private static void ProcessDebugInput(World world, DebugRenderConfig dbgCfg, BattleInputSystemContext ctx)
    {
        var isF1 = GameEngine.CheckHitKey((int)InputButton.KeyF1) || GameEngine.CheckHitKey((int)InputButton.KeyH);
        if (isF1 && !_prevF1)
        {
            dbgCfg.IsActive = !dbgCfg.IsActive;
            dbgCfg.Enabled = dbgCfg.IsActive;
        }
        _prevF1 = isF1;

        // F6 キー：地面近接追撃怪のデバッグ生成
        var isF6 = GameEngine.CheckHitKey((int)InputButton.KeyF6);
        if (isF6 && !_prevF6)
        {
            SpawnDebugMinion(world, MinionType.Ground);
        }
        _prevF6 = isF6;

        // F7 キー：空中浮遊弾幕怪のデバッグ生成
        var isF7 = GameEngine.CheckHitKey((int)InputButton.KeyF7);
        if (isF7 && !_prevF7)
        {
            SpawnDebugMinion(world, MinionType.Flying);
        }
        _prevF7 = isF7;

        // F8 キー：プレイヤー無敵（無限生命）状態のトグル切り替え
        var isF8 = GameEngine.CheckHitKey((int)InputButton.KeyF8);
        if (isF8 && !_prevF8)
        {
            TogglePlayerInvincible(ctx);
        }
        _prevF8 = isF8;

        if (!dbgCfg.IsActive) return;

        // 1/2/3/4 キーによる Boss およびキャラクターの迅速切替：InputAction 抽象経由
        ref readonly var flow = ref world.Get<GameFlow>();
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
        var isF5 = GameEngine.CheckHitKey((int)InputButton.KeyF5);
        if (isF5 && !_prevF5)
        {
            GameManager.Current.SwitchToBattle(flow.SelectedPlayer, BossType.Empty);
            _prevF5 = isF5;
            return;
        }
        _prevF5 = isF5;

        // ゲームオーバー時はフェーズ切り替えを無効化します
        if (world.Has<GameOverTag>()) return;

        var isF2 = GameEngine.CheckHitKey((int)InputButton.KeyF2);
        var isF3 = GameEngine.CheckHitKey((int)InputButton.KeyF3);
        var isF4 = GameEngine.CheckHitKey((int)InputButton.KeyF4);

        if ((isF2 && !_prevF2) || (isF3 && !_prevF3) || (isF4 && !_prevF4))
        {
            _targetDebugPhase = (isF2 && !_prevF2) ? Phase.Phase1 : ((isF3 && !_prevF3) ? Phase.Phase2 : Phase.Phase3);
            ctx.BossQuery.Each(ApplyDebugPhase);
        }

        _prevF2 = isF2;
        _prevF3 = isF3;
        _prevF4 = isF4;
    }

    private static void ApplyDebugPhase(Entity boss, ref Health hp, ref BossCombatState state, ref BossBrainComponent brainComp)
    {
        if (!boss.IsAlive() || boss.Has<DeadTag>() || hp.Current <= 0.0f) return;
        brainComp.Brain.DebugSetPhase(_targetDebugPhase);

        var w = boss.CsWorld();
        var (p2, p3) = state.BossType switch {
            BossType.BossSnake => (w.Get<BossSnakeConfig>().Phase2.HpThreshold, w.Get<BossSnakeConfig>().Phase3.HpThreshold),
            BossType.BossLion => (w.Get<BossLionConfig>().Phase2.HpThreshold, w.Get<BossLionConfig>().Phase3.HpThreshold),
            _ => (0.7f, 0.3f)
        };
        var ratio = _targetDebugPhase switch {
            Phase.Phase1 => 1.0f,
            Phase.Phase2 => p2,
            Phase.Phase3 => p3,
            _ => 1.0f
        };
        hp.Current = Math.Clamp((float)Math.Round(hp.Max * ratio), 1.0f, hp.Max);
        BulletSpawner.ClearAllBossBullets(w);
        MeleeSpawner.ClearAllBossMelee(w);
        state.Phase = _targetDebugPhase;
    }

    private static void SpawnDebugMinion(World world, MinionType type)
    {
        var root = world.Lookup(BattleSceneSpawner.RootName);
        if (!root.IsAlive()) return;

        var spawnPos = MinionSpawnService.SelectBestSpawnPoint(world, type);
        MinionSpawner.SpawnMinion(world, root, type, spawnPos);
    }

    /// <summary>
    ///     プレイヤーの無敵タグ（GameplayTag.Invincible）をトグル切り替えします。
    /// </summary>
    private static void TogglePlayerInvincible(BattleInputSystemContext ctx)
    {
        ctx.PlayerTagStateQuery.Each(static (ref GameplayTagState tagState) =>
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
}
