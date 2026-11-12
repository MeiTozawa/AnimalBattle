using System.Drawing;
using AnimalBattle.Ai;
using AnimalBattle.Ai.BossSnake;
using AnimalBattle.Ai.BossLion;

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
///     自機ステータス：HP、二段ジャンプ残数、Boss 大 HP バー、および操作ガイドを描画する ECS システム。
///     戦闘状態：GameState.Battle時にスクリーン空間へ直接描画します。
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

        world.Set(new HudRenderSystemContext
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
        UpdateAnimTimers(ctx, player, badgeCfg, dt);

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
        var (p2, p3) = state.BossType switch
        {
            BossType.BossSnake => (world.Get<BossSnakeConfig>().Phase2.HpThreshold, world.Get<BossSnakeConfig>().Phase3.HpThreshold),
            BossType.BossLion => (world.Get<BossLionConfig>().Phase2.HpThreshold, world.Get<BossLionConfig>().Phase3.HpThreshold),
            _ => (0.7f, 0.3f)
        };
        ctx.CachedBoss = new HudRenderSystemContext.BossHudData(hps[idx], state, (p2, p3));
    }

    /// <summary>
    ///     HUD および各種ステータス UI をスクリーン空間に描画します。
    /// </summary>
    /// <param name="ctx">コンテクスト</param>
    /// <param name="player">現在アクティブな自機データ。</param>
    /// <param name="renderCfg">描画設定。</param>
    /// <param name="dt">デルタタイム</param>
    /// <summary>
    ///     自機 HP 演出用タイマーおよび被弾トリガーを更新します。
    /// </summary>
    private static void UpdateAnimTimers(
        HudRenderSystemContext ctx,
        in HudRenderSystemContext.PlayerHudData? player,
        PlayerHpBadgeRenderConfig badgeCfg,
        float dt)
    {
        if (ctx.DamageShakeTimer > 0.0f)
            ctx.DamageShakeTimer = Math.Max(0.0f, ctx.DamageShakeTimer - dt);
        if (ctx.DamageFlashTimer > 0.0f)
            ctx.DamageFlashTimer = Math.Max(0.0f, ctx.DamageFlashTimer - dt);
        if (ctx.OverheadWakeTimer > 0.0f)
            ctx.OverheadWakeTimer = Math.Max(0.0f, ctx.OverheadWakeTimer - dt);

        ctx.LowHpPulseTime += dt;

        if (player == null) return;
        var curHp = player.Value.Health.Current;
        if (curHp <= 0.0f)
        {
            // 死亡時：頭上インジケータ表示タイマーを即座にリセット
            ctx.OverheadWakeTimer = 0.0f;
        }
        else if (ctx.LastPlayerHp >= 0.0f && curHp < ctx.LastPlayerHp)
        {
            // 被弾検知：振動・点滅および頭上インジケータを覚醒
            ctx.DamageShakeTimer = badgeCfg.ShakeDuration;
            ctx.DamageFlashTimer = badgeCfg.FlashDuration;
            ctx.OverheadWakeTimer = badgeCfg.OverheadFadeTime;
        }
        ctx.LastPlayerHp = curHp;
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

        DrawPlayerBadgeHud(player, badgeCfg, ctx);
        DrawPlayerOverheadHp(player, badgeCfg, ctx);
        if (isTutorial) return;

        if (boss != null)
        {
            DrawBossStatus(boss, hpCfg, modalCfg);
        }
        else
        {
            DrawTrainingModeBanner(hudCfg);
        }
        DrawGameOverModal(player, modalCfg);
    }

    /// <summary>
    ///     左上戦術 HUD：自機ステータスと肉球バッジ群を描画します。
    /// </summary>
    private static void DrawPlayerBadgeHud(
        in HudRenderSystemContext.PlayerHudData? player,
        PlayerHpBadgeRenderConfig badgeCfg,
        HudRenderSystemContext ctx)
    {
        if (player == null) return;
        var p = player.Value;

        var maxHp = (int)Math.Max(1.0f, p.Health.Max);
        var currentHp = Math.Clamp((int)p.Health.Current, 0, maxHp);

        var isBunny = p.CharacterType == CharacterType.Bunny;
        var charColor = isBunny ? badgeCfg.BunnyFillColor : badgeCfg.CatFillColor;
        var glowColor = isBunny ? badgeCfg.BunnyGlowColor : badgeCfg.CatGlowColor;

        // 被弾フラッシュ判定（高周波点滅）
        var isFlashing = ctx.DamageFlashTimer > 0.0f && ((int)(ctx.DamageFlashTimer * 30.0f) % 2 == 0);
        var activeFillColor = isFlashing ? badgeCfg.DamagedFlashColor : charColor;

        // キャラクター名ラベルと無敵ステータス表示
        var charName = EnumStringLut.Get(p.CharacterType);
        Span<char> nameBuf = stackalloc char[96];
        int charsWritten;
        if (p.IsInvincible)
        {
            nameBuf.TryWrite($"[{charName}] HP: {currentHp}/{maxHp} [INVINCIBLE]", out charsWritten);
        }
        else
        {
            nameBuf.TryWrite($"[{charName}] HP: {currentHp}/{maxHp}", out charsWritten);
        }
        var labelPos = new Vec2i(badgeCfg.Origin.X - (int)badgeCfg.BadgeRadius, badgeCfg.Origin.Y - (int)badgeCfg.BadgeRadius - 16);
        GameEngine.DrawLeftString(labelPos, nameBuf[..charsWritten], p.IsInvincible ? Color.Gold : charColor);

        // 被弾振動オフセットの算出
        var shakeOffset = Vec2i.Zero;
        if (ctx.DamageShakeTimer > 0.0f)
        {
            var sx = (int)MathF.Round(MathF.Sin(ctx.DamageShakeTimer * 60.0f) * badgeCfg.ShakeAmplitude);
            var sy = (int)MathF.Round(MathF.Cos(ctx.DamageShakeTimer * 45.0f) * badgeCfg.ShakeAmplitude);
            shakeOffset = new Vec2i(sx, sy);
        }

        // 各肉球バッジの描画
        var isLowHp = currentHp == 1;
        for (var i = 0; i < maxHp; i++)
        {
            var center = new Vec2i(
                badgeCfg.Origin.X + (int)(i * badgeCfg.BadgeSpacing),
                badgeCfg.Origin.Y);

            var isFilled = i < currentHp;
            var isDamagedSlot = i == currentHp; // 直前に破壊されたスロット

            var badgeCenter = (isDamagedSlot && ctx.DamageShakeTimer > 0.0f) ? center + shakeOffset : center;
            DrawPawBadge(badgeCenter, badgeCfg, activeFillColor, glowColor, isFilled, isLowHp && isFilled, ctx.LowHpPulseTime);
        }

        // エネルギーゲージ（3スロット）と発動ガイドの描画
        DrawPlayerEnergyGauge(p, badgeCfg, isBunny, ctx.LowHpPulseTime);
    }

    /// <summary>
    ///     左上戦術 HUD：自機の特殊能力エネルギーゲージ（3スロット）を描画します。
    /// </summary>
    private static void DrawPlayerEnergyGauge(
        in HudRenderSystemContext.PlayerHudData p,
        PlayerHpBadgeRenderConfig badgeCfg,
        bool isBunny,
        float pulseTime)
    {
        var segCapacity = MathF.Max(1.0f, p.Energy.SegmentCapacity);
        var maxSegments = Math.Max(1, (int)MathF.Round(p.Energy.MaxEnergy / segCapacity));
        var currentEnergy = Math.Clamp(p.Energy.CurrentEnergy, 0.0f, p.Energy.MaxEnergy);
        var readySegments = p.Energy.ReadySegments;

        const int slotWidth = 36;
        const int slotHeight = 7;
        const int slotGap = 4;

        var startX = badgeCfg.Origin.X - (int)badgeCfg.BadgeRadius;
        var startY = badgeCfg.Origin.Y + (int)badgeCfg.BadgeRadius + 14;

        var fillColor = isBunny ? badgeCfg.BunnyFillColor : badgeCfg.CatFillColor;
        var glowBorderColor = isBunny ? badgeCfg.BunnyGlowColor : badgeCfg.CatGlowColor;
        var emptyBgColor = badgeCfg.EmptyFillColor;
        var emptyBorderColor = badgeCfg.EmptyBorderColor;

        for (var s = 0; s < maxSegments; s++)
        {
            var p1 = new Vec2i(startX + s * (slotWidth + slotGap), startY);
            var p2 = new Vec2i(p1.X + slotWidth, p1.Y + slotHeight);

            // スロット背景
            GameEngine.DrawBox(p1, p2, emptyBgColor, true);
            GameEngine.DrawBox(p1, p2, emptyBorderColor, false);

            var slotStart = s * segCapacity;
            var slotCurrent = Math.Clamp(currentEnergy - slotStart, 0.0f, segCapacity);
            var ratio = slotCurrent / segCapacity;

            if (ratio > 0.0f)
            {
                var fillW = Math.Clamp((int)MathF.Round(slotWidth * ratio), 1, slotWidth);
                var fillP2 = new Vec2i(p1.X + fillW, p2.Y);

                GameEngine.DrawBox(p1, fillP2, fillColor, true);

                // 満タン時の発光枠線演出
                if (ratio >= 1.0f)
                {
                    GameEngine.DrawBox(p1, p2, glowBorderColor, false);
                }
            }
        }

        // 全スロット満タン時の全体パルス微光枠
        if (readySegments >= maxSegments)
        {
            var pulse = (MathF.Sin(pulseTime * 8.0f) + 1.0f) * 0.5f;
            var alpha = (int)(120 + pulse * 135);
            var pulseColor = Color.FromArgb(alpha, glowBorderColor.R, glowBorderColor.G, glowBorderColor.B);
            var totalW = maxSegments * slotWidth + (maxSegments - 1) * slotGap;
            var fullP1 = new Vec2i(startX - 1, startY - 1);
            var fullP2 = new Vec2i(startX + totalW + 1, startY + slotHeight + 1);
            GameEngine.DrawBox(fullP1, fullP2, pulseColor, false);
        }

        // 右クリック操作ガイド
        var promptX = startX + maxSegments * (slotWidth + slotGap) + 4;
        var promptY = startY - 2;
        var canCast = p.Energy.CanUseAbility;
        var textColor = canCast ? glowBorderColor : emptyBorderColor;

        GameEngine.DrawLeftString(new Vec2i(promptX, promptY), "[右クリック]", textColor);
    }

    /// <summary>
    ///     肉球バッジ単体をスクリーン空間へ幾何描画します。
    /// </summary>
    private static void DrawPawBadge(
        Vec2i center,
        PlayerHpBadgeRenderConfig badgeCfg,
        Color fillColor,
        Color glowColor,
        bool isFilled,
        bool isLowHpPulsing,
        float pulseTime)
    {
        var radius = (int)badgeCfg.BadgeRadius;
        var palmRadius = (int)badgeCfg.PalmRadius;
        var toeRadius = (int)badgeCfg.ToeRadius;

        if (isFilled && isLowHpPulsing)
        {
            // 瀕死時：わずかに肉球サイズを脈動変調
            var pulse = (int)MathF.Round(MathF.Sin(pulseTime * badgeCfg.LowHpPulseSpeed) * 1.5f);
            palmRadius = Math.Max(2, palmRadius + pulse);
            toeRadius = Math.Max(1, toeRadius + pulse / 2);
        }

        // 外枠と底盤の描画
        var bgFill = isFilled ? Color.FromArgb(140, 15, 20, 30) : badgeCfg.EmptyFillColor;
        var borderCol = isFilled ? glowColor : badgeCfg.EmptyBorderColor;
        GameEngine.DrawCircle(center, radius, bgFill, true);
        GameEngine.DrawCircle(center, radius, borderCol, false);

        // 肉球全体の基準中心（PawOffsetY による全体シフト）
        var pawCenter = new Vec2i(center.X, center.Y + (int)badgeCfg.PawOffsetY);

        // 掌球（主肉球）の描画
        var palmCenter = new Vec2i(pawCenter.X, pawCenter.Y + (int)badgeCfg.PalmOffsetY);
        var padCol = isFilled ? fillColor : badgeCfg.EmptyBorderColor;
        GameEngine.DrawCircle(palmCenter, palmRadius, padCol, isFilled);

        // 指球（爪先肉球群）の描画
        var angles = badgeCfg.ToeAnglesDeg;
        if (angles is { Count: > 0 })
        {
            for (var j = 0; j < angles.Count; j++)
            {
                var rad = angles[j] * (MathF.PI / 180.0f);
                var tx = pawCenter.X + (int)MathF.Round(badgeCfg.ToeDistance * MathF.Sin(rad));
                var ty = pawCenter.Y - (int)MathF.Round(badgeCfg.ToeDistance * MathF.Cos(rad));
                GameEngine.DrawCircle(new Vec2i(tx, ty), toeRadius, padCol, isFilled);
            }
        }
    }

    /// <summary>
    ///     自機頭上に追従するマイクロ HP インジケータを描画します。
    ///     被弾時のみ覚醒し、一定時間後に滑らかにフェードアウトします。
    /// </summary>
    private static void DrawPlayerOverheadHp(
        in HudRenderSystemContext.PlayerHudData? player,
        PlayerHpBadgeRenderConfig badgeCfg,
        HudRenderSystemContext ctx)
    {
        if (player == null || !badgeCfg.OverheadEnabled) return;
        if (!player.Value.IsAlive || player.Value.Health.Current <= 0.0f) return;
        if (ctx.OverheadWakeTimer <= 0.0f) return;

        var p = player.Value;
        var maxHp = (int)Math.Max(1.0f, p.Health.Max);
        var currentHp = Math.Clamp((int)p.Health.Current, 0, maxHp);
        if (currentHp <= 0) return;

        // 自機スクリーン座標の算出
        var headScreenPos = new Vec2i((int)p.Position.X, (int)(p.Position.Y + badgeCfg.OverheadOffsetY)) + CameraService.RenderOffset;

        // フェードアウト不透明度の算出
        var alphaRatio = Math.Clamp(ctx.OverheadWakeTimer / Math.Max(0.01f, badgeCfg.OverheadFadeTime), 0.0f, 1.0f);
        var alpha = (byte)Math.Clamp((int)(badgeCfg.OverheadActiveAlpha * alphaRatio), 0, 255);
        if (alpha == 0) return;

        var isBunny = p.CharacterType == CharacterType.Bunny;
        var baseColor = isBunny ? badgeCfg.BunnyFillColor : badgeCfg.CatFillColor;
        var fillPipColor = Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);
        var emptyPipColor = Color.FromArgb(alpha / 3, badgeCfg.EmptyBorderColor.R, badgeCfg.EmptyBorderColor.G, badgeCfg.EmptyBorderColor.B);

        var totalWidth = (maxHp - 1) * badgeCfg.OverheadPipSpacing;
        var startX = headScreenPos.X - (int)(totalWidth * 0.5f);
        var pipRadius = (int)badgeCfg.OverheadPipRadius;

        for (var i = 0; i < maxHp; i++)
        {
            var pipCenter = new Vec2i(startX + (int)(i * badgeCfg.OverheadPipSpacing), headScreenPos.Y);
            var isFilled = i < currentHp;
            GameEngine.DrawCircle(pipCenter, pipRadius, isFilled ? fillPipColor : emptyPipColor, true);
        }
    }

    private static void DrawBossStatus(in HudRenderSystemContext.BossHudData? boss, BossHpBarRenderConfig hpCfg, ModalRenderConfig modalCfg)
    {
        if (boss == null) return;
        var b = boss.Value;

        var phase = ToLegacyPhase(b.CombatState.Phase);
        var bossTitle = GetBossTitle(b.CombatState.BossType, phase);
        var bossHp = b.Health;
        DrawBossHpBar(hpCfg, modalCfg, bossTitle, bossHp.Current, bossHp.Max, phase, b.PhaseThresholds);
    }

    /// <summary>
    ///     トレーニングモード（ボス不在時）の簡易案内バナーを描画します。
    /// </summary>
    private static void DrawTrainingModeBanner(HudRenderConfig hudCfg)
    {
        if (!string.IsNullOrEmpty(hudCfg.TrainingModeTitleText))
        {
            GameEngine.DrawLeftString(hudCfg.TrainingModeTitlePos, hudCfg.TrainingModeTitleText, hudCfg.TrainingModeTitleColor);
        }
        if (!string.IsNullOrEmpty(hudCfg.TrainingModeGuideText))
        {
            GameEngine.DrawLeftString(hudCfg.TrainingModeGuidePos, hudCfg.TrainingModeGuideText, hudCfg.TrainingModeGuideColor);
        }
    }

    /// <summary>
    ///     Boss の称号およびフェーズ表記を取得します：パターンマッチにより固定文字列を返し 0-GC で描画。
    /// </summary>
    private static string GetBossTitle(BossType bossType, int phase)
    {
        var p = Math.Clamp(phase, 1, 3);
        return (bossType, p) switch {
            (BossType.BossSnake, 1) => "BOSS: BossSnake [PHASE 1/3]",
            (BossType.BossSnake, 2) => "BOSS: BossSnake [PHASE 2/3]",
            (BossType.BossSnake, 3) => "BOSS: BossSnake [PHASE 3/3]",
            (BossType.BossLion, 1) => "BOSS: BossLion [PHASE 1/3]",
            (BossType.BossLion, 2) => "BOSS: BossLion [PHASE 2/3]",
            (BossType.BossLion, 3) => "BOSS: BossLion [PHASE 3/3]",
            (BossType.Empty, _) => "TRAINING DUMMY",
            _ => $"BOSS: {bossType} [PHASE {p}/3]"
        };
    }

    private static void DrawGameOverModal(in HudRenderSystemContext.PlayerHudData? player, ModalRenderConfig modalCfg)
    {
        if (player == null || player.Value.IsAlive) return;
        GameEngine.DrawBox(modalCfg.GameOverPos1, modalCfg.GameOverPos2, modalCfg.GameOverBgColor, true);
        GameEngine.DrawBox(modalCfg.GameOverPos1, modalCfg.GameOverPos2, modalCfg.GameOverOuterBorderColor, false);
        var pad = new Vec2i(modalCfg.InnerBorderPadding, modalCfg.InnerBorderPadding);
        GameEngine.DrawBox(modalCfg.GameOverPos1 + pad, modalCfg.GameOverPos2 - pad, modalCfg.GameOverInnerBorderColor, false);
        var modalCenterX = (modalCfg.GameOverPos1.X + modalCfg.GameOverPos2.X) / 2;
        GameEngine.DrawCenterString(new Vec2i(modalCenterX, modalCfg.GameOverTitlePos.Y), modalCfg.GameOverTitleText, modalCfg.GameOverTitleColor);
        GameEngine.DrawCenterString(new Vec2i(modalCenterX, modalCfg.GameOverRetryPos.Y), modalCfg.GameOverRetryText, modalCfg.GameOverRetryColor);
    }

    private static int ToLegacyPhase(Phase phase) => phase switch {
        Phase.Phase1 => 1,
        Phase.Phase2 => 2,
        Phase.Phase3 => 3,
        _ => AssertHelper.FailWithDefault("[HUD 警告] 未知のフェーズ", 1)
    };

    internal static void FormatBossStateInfo(
        Span<char> destination,
        out int charsWritten,
        BossCombatState combatState,
        IBossBrain? brain,
        BossHpBarRenderConfig hpCfg,
        out Color badgeColor)
    {
        var currentAbility = brain?.AbilitySystem.CurrentAbility;
        var abilityName = currentAbility?.DisplayName;
        if (string.IsNullOrEmpty(abilityName))
        {
            abilityName = !combatState.CurrentSkillTag.IsEmpty ? combatState.CurrentSkillTag.ToString() : "None";
        }

        var tags = brain?.AbilitySystem.Tags ?? default;
        var isDead = brain?.IsDead ?? false;

        if (isDead)
        {
            badgeColor = hpCfg.StateDefeatedColor;
            destination.TryWrite($"[STATE: DEFEATED]", out charsWritten);
            return;
        }
        if (tags.HasTag(GameplayTag.Phasing))
        {
            badgeColor = hpCfg.StatePhaseTransitionColor;
            destination.TryWrite($"[STATE: PHASE TRANSITION (INVINCIBLE)]", out charsWritten);
            return;
        }
        if (tags.HasTag(GameplayTag.Recovery))
        {
            badgeColor = hpCfg.StateRecoveringColor;
            destination.TryWrite($"[STATE: RECOVERING]", out charsWritten);
            return;
        }
        if (tags.HasTag(GameplayTag.Stunned))
        {
            badgeColor = hpCfg.StateStunnedColor;
            destination.TryWrite($"[STATE: STUNNED (VULNERABLE)]", out charsWritten);
            return;
        }
        if (tags.HasTag(GameplayTag.Telegraph))
        {
            badgeColor = hpCfg.StateTelegraphingColor;
            destination.TryWrite($"[STATE: TELEGRAPHING (WIND-UP: {abilityName})]", out charsWritten);
            return;
        }
        if (tags.HasTag(GameplayTag.Active))
        {
            badgeColor = hpCfg.StateCastingColor;
            destination.TryWrite($"[STATE: EXECUTING ({abilityName})]", out charsWritten);
            return;
        }

        badgeColor = hpCfg.StateDefaultColor;
        switch (brain)
        {
            case BossSnakeBrain b1:
                switch (b1.State)
                {
                    case BossSnakeState.Hover:
                        destination.TryWrite($"[STATE: HOVERING (IDLE)]", out charsWritten);
                        return;
                    default:
                        destination.TryWrite($"[STATE: UNKNOWN]", out charsWritten);
                        return;
                }
            case BossLionBrain b2:
                switch (b2.State)
                {
                    case BossLionState.Idle:
                        destination.TryWrite($"[STATE: IDLE / ALERT]", out charsWritten);
                        return;
                    case BossLionState.PatrolChase:
                        destination.TryWrite($"[STATE: CHASE (NEXT: {abilityName})]", out charsWritten);
                        return;
                    case BossLionState.PlatformTraverse:
                        destination.TryWrite($"[STATE: PLATFORM TRAVERSE (AIRBORNE)]", out charsWritten);
                        return;
                    default:
                        destination.TryWrite($"[STATE: UNKNOWN]", out charsWritten);
                        return;
                }
            default:
                destination.TryWrite($"[STATE: UNKNOWN]", out charsWritten);
                return;
        }
    }

    private static void DrawBossHpBar(
        BossHpBarRenderConfig hpCfg,
        ModalRenderConfig modalCfg,
        string bossTitle,
        float currentHp,
        float maxHp,
        int phase,
        (float Phase2Threshold, float Phase3Threshold) phaseThresholds)
    {
        var barP1 = hpCfg.BarPos1;
        var barP2 = hpCfg.BarPos2;
        var totalWidth = barP2.X - barP1.X;

        // ボス情報ヘッダー：HPバー上部
        Span<char> hpBuf = stackalloc char[64];
        hpBuf.TryWrite($"HP: {currentHp:F0} / {maxHp:F0}", out var hpCharsWritten);

        var titlePos = barP1 + (hpCfg.TitleOffset != Vec2i.Zero
            ? hpCfg.TitleOffset
            : new Vec2i(0, -hpCfg.HeaderOffsetY));
        GameEngine.DrawLeftString(titlePos, bossTitle, hpCfg.TitleColor);

        var hpY = barP1.Y - (hpCfg.HpTextOffsetY != 0 ? hpCfg.HpTextOffsetY : hpCfg.HeaderOffsetY);
        GameEngine.DrawRightString(new Vec2i(barP2.X, hpY), hpBuf[..hpCharsWritten], hpCfg.HpTextColor);

        // Boss HP バー背景および枠線
        var bgPad = new Vec2i(hpCfg.BgPadding, hpCfg.BgPadding);
        GameEngine.DrawBox(barP1 - bgPad, barP2 + bgPad, hpCfg.BgColor, true);
        GameEngine.DrawBox(barP1 - bgPad, barP2 + bgPad, hpCfg.BorderColor, false);

        // Boss HP ゲージ塗りつぶし
        Color hpFillColor;
        switch (phase)
        {
            case 1:
                hpFillColor = hpCfg.Phase1Color;
            break;
            case 2:
                hpFillColor = hpCfg.Phase2Color;
            break;
            case 3:
                hpFillColor = hpCfg.Phase3Color;
            break;
            default:
                Debug.Fail("[HUD 警告] 未知のフェーズ");
                hpFillColor = hpCfg.Phase1Color;
            break;
        }

        var hpRatio = maxHp > 0.0f ? Math.Clamp(currentHp / maxHp, 0.0f, 1.0f) : 0.0f;
        var fillWidth = (int)(totalWidth * hpRatio);
        if (fillWidth > 0)
        {
            GameEngine.DrawBox(barP1, new Vec2i(barP1.X + fillWidth, barP2.Y), hpFillColor, true);
            GameEngine.DrawBox(barP1, new Vec2i(barP1.X + fillWidth, barP1.Y + hpCfg.TopHighlightThickness),
                hpCfg.TopHighlightColor, true);
        }

        // Phase 分割マーカー：設定ファイル駆動の HpThreshold に基づく動的描画
        var p2Ratio = Math.Clamp(phaseThresholds.Phase2Threshold, 0.0f, 1.0f);
        var p3Ratio = Math.Clamp(phaseThresholds.Phase3Threshold, 0.0f, 1.0f);

        var phase2MarkerX = barP1.X + (int)(totalWidth * p2Ratio);
        var phase3MarkerX = barP1.X + (int)(totalWidth * p3Ratio);

        GameEngine.DrawBox(new Vec2i(phase2MarkerX - hpCfg.PhaseSplitWidth, barP1.Y), new Vec2i(phase2MarkerX + hpCfg.PhaseSplitWidth, barP2.Y), hpCfg.BorderColor, true);
        GameEngine.DrawBox(new Vec2i(phase3MarkerX - hpCfg.PhaseSplitWidth, barP1.Y), new Vec2i(phase3MarkerX + hpCfg.PhaseSplitWidth, barP2.Y), hpCfg.BorderColor, true);

        // 勝利バナー
        if (currentHp > 0.0f) return;
        GameEngine.DrawBox(modalCfg.VictoryPos1, modalCfg.VictoryPos2, modalCfg.VictoryBgColor, true);
        GameEngine.DrawBox(modalCfg.VictoryPos1, modalCfg.VictoryPos2, modalCfg.VictoryOuterBorderColor, false);
        var vicPad = new Vec2i(modalCfg.InnerBorderPadding, modalCfg.InnerBorderPadding);
        GameEngine.DrawBox(modalCfg.VictoryPos1 + vicPad, modalCfg.VictoryPos2 - vicPad, modalCfg.VictoryInnerBorderColor, false);
        var vicCenterX = (modalCfg.VictoryPos1.X + modalCfg.VictoryPos2.X) / 2;
        GameEngine.DrawCenterString(new Vec2i(vicCenterX, modalCfg.VictoryTitlePos.Y), modalCfg.VictoryTitleText, modalCfg.VictoryTitleColor);
        GameEngine.DrawCenterString(new Vec2i(vicCenterX, modalCfg.VictoryRetryPos.Y), modalCfg.VictoryRetryText, modalCfg.VictoryRetryColor);
    }
}
