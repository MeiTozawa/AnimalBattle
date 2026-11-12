namespace AnimalBattle.Presentation.Rendering;

/// <summary>
///     World 単位で独立して保持される HUD 描画システム用コンテキスト。
/// </summary>
internal sealed class HudRenderSystemContext : IDisposable
{
    public readonly record struct PlayerHudData(
        Health Health,
        EnergyGauge Energy,
        CharacterType CharacterType,
        bool IsInvincible,
        bool IsAlive,
        Vec2f Position);

    public readonly record struct BossHudData(
        Health Health,
        BossCombatState CombatState,
        (float Phase2Threshold, float Phase3Threshold) PhaseThresholds);

    public required Query<Health, EnergyGauge, CharacterTypeComponent, GameplayTagState, Transform> PlayerQuery { get; init; }

    /// <summary>前フレームの自機 HP（被弾検知用）</summary>
    public float LastPlayerHp = -1f;

    /// <summary>被弾振動アニメーション残り時間（秒）</summary>
    public float DamageShakeTimer;

    /// <summary>被弾フラッシュアニメーション残り時間（秒）</summary>
    public float DamageFlashTimer;

    /// <summary>頭上インジケータ覚醒表示残り時間（秒）</summary>
    public float OverheadWakeTimer;

    /// <summary>瀕死時脈動アニメーションの経過時間（秒）</summary>
    public float LowHpPulseTime;
    public required Query<Health, BossCombatState> BossQuery { get; init; }
    public PlayerHudData? CachedPlayer;
    public BossHudData? CachedBoss;

    public void Dispose()
    {
        PlayerQuery.Dispose();
        BossQuery.Dispose();
    }
}

/// <summary>
///     自機ステータス・Boss HP バー・各種モーダル UI の描画を統括する ECS オーケストレーションシステム。
///     描画詳細は <see cref="PlayerHudRenderer"/>、<see cref="BossHudRenderer"/>、<see cref="BattleModalRenderer"/> に委譲します。
/// </summary>
public static class HudRenderSystem {
    /// <summary>
    ///     HUD 描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddHudRenderSystem(this World world)
    {
        var playerQuery = world.QueryBuilder<Health, EnergyGauge, CharacterTypeComponent, GameplayTagState, Transform>()
            .With<PlayerTag>()
            .Build();
        var bossQuery = world.QueryBuilder<Health, BossCombatState>()
            .With<BossTag>()
            .Build();

        world.SetDisposable(new HudRenderSystemContext
        {
            PlayerQuery = playerQuery,
            BossQuery = bossQuery
        });

        world.System("HudRenderSystem")
            .Kind(Ecs.OnStore)
            .Iter(ExecuteHudRender);

        return world;
    }

    /// <summary>
    ///     HUD 描画パイプライン：名前付き静的メソッド。
    /// </summary>
    public static void ExecuteHudRender(Iter it)
    {
        var w = it.World();
        ref readonly var flow = ref w.Get<GameFlow>();
        if (!flow.IsGameplay) return;

        var ctx = w.Get<HudRenderSystemContext>();
        ctx.CachedPlayer = null;
        ctx.CachedBoss = null;

        ctx.PlayerQuery.Iter(CollectPlayer);
        ctx.BossQuery.Iter(CollectBoss);

        var player = ctx.CachedPlayer;
        var boss = ctx.CachedBoss;
        ctx.CachedPlayer = null;
        ctx.CachedBoss = null;

        var badgeCfg = w.Get<PlayerHpBadgeRenderConfig>();
        var hudCfg = w.Get<HudRenderConfig>();
        var hpCfg = w.Get<BossHpBarRenderConfig>();
        var modalCfg = w.Get<ModalRenderConfig>();
        var dt = it.DeltaTime();
        if (dt <= 0.0f) dt = 1.0f / 60.0f;
        PlayerHudRenderer.UpdateAnimTimers(ctx, player, badgeCfg, dt);

        Render(ctx, player, boss, hudCfg, hpCfg, modalCfg, badgeCfg, flow.State == GameState.Tutorial);
    }

    /// <summary>
    ///     自機 HUD データを無割当：Zero Allocationで抽出します。
    /// </summary>
    private static void CollectPlayer(
        Iter it,
        Field<Health> hps,
        Field<EnergyGauge> gauges,
        Field<CharacterTypeComponent> charTypes,
        Field<GameplayTagState> tags,
        Field<Transform> transforms)
    {
        var count = it.Count();
        if (count == 0) return;

        var ctx = it.World().Get<HudRenderSystemContext>();
        var idx = count - 1;
        ref readonly var hp = ref hps[idx];
        ref readonly var gauge = ref gauges[idx];
        ctx.CachedPlayer = new HudRenderSystemContext.PlayerHudData(
            hp,
            gauge,
            charTypes[idx].Value,
            tags[idx].HasAnyTag(GameplayTag.Invincible | GameplayTag.Untargetable),
            hp.Current > 0.0f,
            transforms[idx].Position);
    }

    /// <summary>
    ///     Boss HUD データを無割当：Zero Allocationで抽出します。
    /// </summary>
    private static void CollectBoss(Iter it, Field<Health> hps, Field<BossCombatState> states)
    {
        var count = it.Count();
        if (count == 0) return;

        var world = it.World();
        var ctx = world.Get<HudRenderSystemContext>();
        var idx = count - 1;
        ref readonly var state = ref states[idx];
        var cfg = BossPrefabs.GetConfig(world, state.BossType);
        var p2 = cfg.Phase2HpThreshold;
        var p3 = cfg.Phase3HpThreshold;
        ctx.CachedBoss = new HudRenderSystemContext.BossHudData(hps[idx], state, (p2, p3));
    }

    private static void Render(
        HudRenderSystemContext ctx,
        in HudRenderSystemContext.PlayerHudData? player,
        in HudRenderSystemContext.BossHudData? boss,
        HudRenderConfig hudCfg,
        BossHpBarRenderConfig hpCfg,
        ModalRenderConfig modalCfg,
        PlayerHpBadgeRenderConfig badgeCfg,
        bool isTutorial = false)
    {
        Debug.Assert(GameEngine.CameraOffset == Vec2i.Zero, "[HudRenderSystem] HUDはスクリーン空間固定で描画される必要があり、CameraOffsetはゼロでなければなりません。");

        PlayerHudRenderer.DrawPlayerBadgeHud(player, badgeCfg, ctx);
        PlayerHudRenderer.DrawPlayerOverheadHp(player, badgeCfg, ctx);
        if (isTutorial) return;

        if (boss != null)
        {
            BossHudRenderer.DrawBossStatus(boss, hpCfg, modalCfg);
        }
        else
        {
            BattleModalRenderer.DrawTrainingModeBanner(hudCfg);
        }
        BattleModalRenderer.DrawGameOverModal(player, modalCfg);
    }
}
