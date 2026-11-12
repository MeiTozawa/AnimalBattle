using System.Drawing;

namespace AnimalBattle.Presentation.Rendering;

/// <summary>
///     自機プレイヤー HUD（左上肉球 HP バッジ・エネルギーゲージ・頭上マイクロ HP インジケータ）の描画を担当するレンダラー。
/// </summary>
internal static class PlayerHudRenderer
{
    /// <summary>
    ///     自機 HP 演出用タイマーおよび被弾トリガーを更新します。
    /// </summary>
    public static void UpdateAnimTimers(
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

    /// <summary>
    ///     左上戦術 HUD：自機ステータスと肉球バッジ群を描画します。
    /// </summary>
    public static void DrawPlayerBadgeHud(
        in HudRenderSystemContext.PlayerHudData? player,
        PlayerHpBadgeRenderConfig badgeCfg,
        HudRenderSystemContext ctx)
    {
        if (player == null) return;
        var p = player.Value;

        var maxHp = (int)Math.Max(1.0f, p.Health.Max);
        var currentHp = Math.Clamp((int)p.Health.Current, 0, maxHp);

        var charColor = badgeCfg.GetCharacterFillColor(p.CharacterType);
        var glowColor = badgeCfg.GetCharacterGlowColor(p.CharacterType);

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
        DrawPlayerEnergyGauge(p, badgeCfg, charColor, glowColor, ctx.LowHpPulseTime);
    }

    /// <summary>
    ///     左上戦術 HUD：自機の特殊能力エネルギーゲージ（3スロット）を描画します。
    /// </summary>
    public static void DrawPlayerEnergyGauge(
        in HudRenderSystemContext.PlayerHudData p,
        PlayerHpBadgeRenderConfig badgeCfg,
        Color fillColor,
        Color glowBorderColor,
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
    public static void DrawPawBadge(
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
    public static void DrawPlayerOverheadHp(
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

        var baseColor = badgeCfg.GetCharacterFillColor(p.CharacterType);
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
}
