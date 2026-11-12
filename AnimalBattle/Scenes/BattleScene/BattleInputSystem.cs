using AnimalBattle.Ai;

namespace AnimalBattle.Scenes.Battle;

/// <summary>
///     World 単位で独立して保持される戦闘進行入力処理システム用コンテキスト。
/// </summary>
internal sealed class BattleInputSystemContext : IDisposable
{
    public required Query PlayerEntitiesQuery { get; init; }
    public required Query<Health, BossCombatState, BossBrainComponent> BossQuery { get; init; }

    public void Dispose()
    {
        PlayerEntitiesQuery.Dispose();
        BossQuery.Dispose();
    }
}

/// <summary>
///     戦闘シーンにおける入力受付、自機操作、および戦闘リセット・シーン復帰を司る ECS システム。
///     GameState.Battle 状態時にのみアクティブとなり、PreUpdate フェーズでプレイヤーの進行入力を処理します。
/// </summary>
public static class BattleInputSystem
{
    /// <summary>
    ///     戦闘入力処理システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddBattleInputSystem(this World world)
    {
        var playerEntitiesQuery = world.QueryBuilder()
            .With<PlayerTag>()
            .Build();

        var bossQuery = world.QueryBuilder<Health, BossCombatState, BossBrainComponent>()
            .With<BossTag>()
            .Build();

        world.SetDisposable(new BattleInputSystemContext
        {
            PlayerEntitiesQuery = playerEntitiesQuery,
            BossQuery = bossQuery
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
            GameManager.Current.SwitchToStart(enableTutorialOverride: false);
            return;
        }

        // R キーで戦闘リセット
        if (InputService.IsPressed(InputAction.Restart))
        {
            ResetCombat(w, ctx);
        }

        // ゲームオーバー時は自機の操作入力を停止：PlayerInputSystem 側でも同様に判定
        if (w.Has<GameOverTag>()) return;
    }

    private static void ResetCombat(World world, BattleInputSystemContext ctx)
    {
        // ゲームオーバー状態の解除：Remove は冪等のため事前の Has チェック不要
        world.Remove<GameOverTag>();

        CombatCleanupService.ClearTransientCombatEntitiesForReset(world);
        CameraService.Reset();
        world.Set(new AimReticleState());

        ctx.BossQuery.Each(ResetBossEntity);
        ctx.PlayerEntitiesQuery.Each(ResetPlayerEntity);
        PlayerInputSystem.Reset();

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
        boss.GetMut<CombatProfile>().Phase = Phase.Phase1;
        state.CurrentSkillTag = StringId.Empty;
        state.StateTimer = 0.0f;
        state.TargetPosition = Vec2f.Zero;
        state.FacingRight = true;

        boss.Set(new GameplayTagState(brainComp.Brain.AbilitySystem.Tags.RawTags));
        boss.Set(new TimedEffectsState());

        boss.GetMut<Hitbox>().IsActive = true;

        // 一時状態・エフェクト・死亡タグ・スポーンバッファの一括安全削除：Flecs ネイティブ API による冪等操作
        boss.Remove<HitstopState>()
            .Remove<SpriteJitter>()
            .Remove<DeadTag>()
            .Remove<HitFlashEffect>()
            .Remove<InvincibleBlinkEffect>()
            .Remove<SpawnBufferState>()
            .Remove<CannotMoveTag>()
            .Remove<CannotAttackTag>();
        
        ref var sprite = ref boss.GetMut<Sprite>();
        sprite.IsVisible = true;
        sprite.JitterOffset = Vec2f.Zero;
    }

    internal static void ResetPlayerEntity(Entity player)
    {
        PlayerPrefabs.ResetPlayerEntity(player);
    }
}
