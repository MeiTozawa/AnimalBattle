using System;
using System.Drawing;
using AnimalBattle.Abilities;
using AnimalBattle.Characters;
using AnimalBattle.Characters.Boss;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Systems.Render;

/// <summary>
///     Boss の前揺予兆線（Telegraph）、虚弱隙（Vulnerable）演出、およびフェーズ遷移無敵結界を描画する ECS システム。
/// </summary>
public static class BossTelegraphRenderSystem {
    /// <summary>
    ///     Boss 予兆・特殊状態描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddBossTelegraphRenderSystem(this World world)
    {
        var playerQuery = world.QueryBuilder<PlayerPawn>().Build();
        var bossQuery = world.QueryBuilder<BaseBossAgent>().Build();

        world.System("BossTelegraphRenderSystem")
            .Kind(Ecs.OnStore)
            .Iter((Iter it) =>
            {
                var w = it.World();
                ref readonly var flow = ref w.Get<GameFlow>();
                if (flow.State != GameState.Battle) return;

                BaseBossAgent? boss = null;
                bossQuery.Each((ref BaseBossAgent b) => boss = b);
                if (boss == null) return;
                if (!boss.Entity.IsAlive() || !boss.IsAlive || !boss.Sprite.IsVisible || boss.Sprite.GraphHandle < 0) return;

                var playerCfg = w.GetActivePlayerConfig();
                var playerX = playerCfg.SpawnX;
                var playerY = playerCfg.SpawnY;

                playerQuery.Each((ref PlayerPawn player) =>
                {
                    if (player.Entity.IsAlive())
                    {
                        ref readonly var pt = ref player.Transform;
                        playerX = pt.X;
                        playerY = pt.Y;
                    }
                });

                ref readonly var renderCfg = ref w.Get<RenderConfig>();
                var bossFx = renderCfg.BossEffects;

                ref readonly var bTrans = ref boss.Transform;
                ref readonly var bSprite = ref boss.Sprite;
                var bx = (int)(bTrans.X + bSprite.JitterOffsetX);
                var by = (int)(bTrans.Y + bSprite.JitterOffsetY);

                // 1. 予兆前揺（Telegraph）描画
                if (boss.CurrentState.IsTelegraph)
                {
                    var progress = boss.AbilitySystem.PhaseProgress;
                    DrawBossTelegraph(boss, playerX, playerY, progress);
                }

                var flipX = !boss.FacingRight;
                var rotation = bTrans.Rotation;

                // 2. 虚弱（Vulnerable）状態の脈動オーラとマーカー
                if (boss.IsVulnerable)
                {
                    DrawBossVulnerableEffects(
                        bx, by, bSprite.SrcRect, bTrans.ScaleX, bTrans.ScaleY, rotation,
                        bSprite.GraphHandle, flipX, boss.StateTimer, bossFx);
                }

                // 3. フェーズ遷移無敵結界演出
                if (boss.CurrentState.IsPhaseTransition)
                {
                    DrawBossPhaseTransitionEffects(
                        bx, by, bSprite.SrcRect, bTrans.ScaleX, bTrans.ScaleY, rotation,
                        bSprite.GraphHandle, flipX, boss.StateTimer, bossFx);
                }
            });

        return world;
    }

    private static (float x, float y) ResolveTargetPosition(float targetX, float targetY, float playerX, float playerY)
    {
        return (
            targetX != 0.0f || targetY != 0.0f ? targetX : playerX,
            targetX != 0.0f || targetY != 0.0f ? targetY : playerY
        );
    }

    private static void DrawBossTelegraph(
        BaseBossAgent boss,
        float playerX,
        float playerY,
        float progress)
    {
        var currentAbility = boss.AbilitySystem.CurrentAbility;
        if (currentAbility == null) return;

        var (finalTargetX, finalTargetY) = ResolveTargetPosition(boss.TargetX, boss.TargetY, playerX, playerY);
        ref readonly var bTrans = ref boss.Transform;
        var bHeight = boss.CollisionBox.Height;
        AbilityTelegraphContext ctx = new(bTrans.X, bTrans.Y, finalTargetX, finalTargetY, boss.FacingRight, progress, boss.StateTimer, bHeight);
        currentAbility.DrawTelegraph(in ctx);
    }

    private static void DrawBossVulnerableEffects(
        int bx,
        int by,
        in Rect srcRect,
        float scaleX,
        float scaleY,
        double angle,
        int graphHandle,
        bool flipX,
        float stateTimer,
        BossEffectsRenderConfig bossFx)
    {
        var pulse = 0.5f + 0.5f * MathF.Sin(stateTimer * bossFx.VulnerablePulseSpeed);
        var vulnAlpha = (int)(bossFx.VulnerableAuraBaseAlpha + bossFx.VulnerableAuraPulseAlpha * pulse);
        GameEngine.SetDrawBlendMode(BlendMode.Add, vulnAlpha);
        GameEngine.DrawRectRotaGraph3(
            bx,
            by,
            srcRect.X1,
            srcRect.Y1,
            srcRect.Width,
            srcRect.Height,
            scaleX * bossFx.VulnerableAuraScale,
            scaleY * bossFx.VulnerableAuraScale,
            angle,
            graphHandle,
            true,
            flipX);
        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        var bobbing = MathF.Sin(stateTimer * bossFx.ChanceMarkerBobbingSpeed) * bossFx.ChanceMarkerBobbingAmplitude;
        var textWidth = bossFx.ChanceMarkerText.Length * bossFx.ChanceMarkerCharWidth;
        var textX = bx - textWidth / 2;
        var textY = (int)(by - bossFx.ChanceMarkerOffsetY + bobbing);
        GameEngine.DrawString(textX, textY, bossFx.ChanceMarkerText, bossFx.ChanceMarkerColor);
    }

    private static void DrawBossPhaseTransitionEffects(
        int bx,
        int by,
        in Rect srcRect,
        float scaleX,
        float scaleY,
        double angle,
        int graphHandle,
        bool flipX,
        float stateTimer,
        BossEffectsRenderConfig bossFx)
    {
        var pulse = 0.5f + 0.5f * MathF.Sin(stateTimer * bossFx.InvinciblePulseSpeed);
        var goldAlpha = (int)(bossFx.InvincibleAuraBaseAlpha + bossFx.InvincibleAuraPulseAlpha * pulse);
        GameEngine.SetDrawBright(bossFx.InvincibleAuraColor.R, bossFx.InvincibleAuraColor.G, bossFx.InvincibleAuraColor.B);
        GameEngine.SetDrawBlendMode(BlendMode.Add, goldAlpha);
        GameEngine.DrawRectRotaGraph3(
            bx,
            by,
            srcRect.X1,
            srcRect.Y1,
            srcRect.Width,
            srcRect.Height,
            scaleX * bossFx.InvincibleAuraScale,
            scaleY * bossFx.InvincibleAuraScale,
            angle,
            graphHandle,
            true,
            flipX);
        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
        GameEngine.SetDrawBright(255, 255, 255);

        var baseRadius = bossFx.InvincibleShieldRadius;
        var shieldBreath = MathF.Sin(stateTimer * bossFx.InvincibleBreathSpeed) * bossFx.InvincibleBreathAmplitude;
        var currentShieldRadius = (int)(baseRadius + shieldBreath);

        var shieldFillAlpha = (int)(bossFx.InvincibleFillBaseAlpha + bossFx.InvincibleFillPulseAlpha * pulse);
        GameEngine.SetDrawBlendMode(BlendMode.Add, shieldFillAlpha);
        GameEngine.DrawCircle(bx, by, currentShieldRadius, ColorPalette.Gold, true);

        var shieldRingAlpha = (int)(bossFx.InvincibleRingBaseAlpha + bossFx.InvincibleRingPulseAlpha * pulse);
        GameEngine.SetDrawBlendMode(BlendMode.Add, shieldRingAlpha);
        GameEngine.DrawCircle(bx, by, currentShieldRadius, ColorPalette.GoldLight, false);
        if (currentShieldRadius > 1)
        {
            GameEngine.DrawCircle(bx, by, currentShieldRadius - 1, ColorPalette.Gold, false);
        }

        var ripple1 = (stateTimer * bossFx.InvincibleRippleSpeed) % 1.0f;
        var ripple1Radius = (int)(currentShieldRadius * (bossFx.InvincibleRippleStartRatio + bossFx.InvincibleRippleExpandRatio * ripple1));
        var ripple1Alpha = (int)(bossFx.InvincibleRippleMaxAlpha * (1.0f - ripple1));
        if (ripple1Alpha > 0)
        {
            GameEngine.SetDrawBlendMode(BlendMode.Add, ripple1Alpha);
            GameEngine.DrawCircle(bx, by, ripple1Radius, ColorPalette.GoldLight, false);
        }

        var ripple2 = (stateTimer * bossFx.InvincibleRippleSpeed + 0.5f) % 1.0f;
        var ripple2Radius = (int)(currentShieldRadius * (bossFx.InvincibleRippleStartRatio + bossFx.InvincibleRippleExpandRatio * ripple2));
        var ripple2Alpha = (int)(bossFx.InvincibleRippleMaxAlpha * (1.0f - ripple2));
        if (ripple2Alpha > 0)
        {
            GameEngine.SetDrawBlendMode(BlendMode.Add, ripple2Alpha);
            GameEngine.DrawCircle(bx, by, ripple2Radius, ColorPalette.White, false);
        }

        var orbitAngle = stateTimer * bossFx.InvincibleOrbitSpeed;
        const float angleStep = MathF.PI * 0.5f;

        Span<int> nodeXs = stackalloc int[4];
        Span<int> nodeYs = stackalloc int[4];
        for (var i = 0; i < 4; i++)
        {
            var theta = orbitAngle + i * angleStep;
            nodeXs[i] = (int)(bx + MathF.Cos(theta) * currentShieldRadius);
            nodeYs[i] = (int)(by + MathF.Sin(theta) * currentShieldRadius);
        }

        var prismAlpha = (int)(bossFx.InvinciblePrismAlphaBase + bossFx.InvinciblePrismAlphaPulse * pulse);
        GameEngine.SetDrawBlendMode(BlendMode.Add, prismAlpha);
        for (var i = 0; i < 4; i++)
        {
            var next = (i + 1) % 4;
            GameEngine.DrawLine(nodeXs[i], nodeYs[i], nodeXs[next], nodeYs[next], ColorPalette.GoldLight, bossFx.InvinciblePrismThickness);
        }

        for (var i = 0; i < 4; i++)
        {
            GameEngine.SetDrawBlendMode(BlendMode.Add, bossFx.InvincibleNodeAlpha);
            GameEngine.DrawCircle(nodeXs[i], nodeYs[i], bossFx.InvincibleNodeOuterRadius, ColorPalette.GoldLight, true);
            GameEngine.DrawCircle(nodeXs[i], nodeYs[i], bossFx.InvincibleNodeInnerRadius, ColorPalette.White, true);
        }

        var crossAngle = stateTimer * bossFx.InvincibleCrossAngleSpeed;
        var flareLength = currentShieldRadius * bossFx.InvincibleCrossLengthRatio;
        var crossAlpha = (int)(bossFx.InvincibleCrossAlphaBase + bossFx.InvincibleCrossAlphaPulse * pulse);
        GameEngine.SetDrawBlendMode(BlendMode.Add, crossAlpha);
        for (var i = 0; i < 4; i++)
        {
            var theta = crossAngle + i * angleStep;
            var fx = (int)(bx + MathF.Cos(theta) * flareLength);
            var fy = (int)(by + MathF.Sin(theta) * flareLength);
            GameEngine.DrawLine(bx, by, fx, fy, ColorPalette.GoldLight, bossFx.InvincibleCrossThickness);
        }

        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
    }
}
