using System;
using System.Drawing;
using AnimalBattle.Abilities;
using AnimalBattle.Characters;
using AnimalBattle.Characters.Boss;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Render;
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
                var playerPos = playerCfg.Spawn;

                playerQuery.Each((ref PlayerPawn player) =>
                {
                    if (player.Entity.IsAlive())
                    {
                        playerPos = player.Transform.Position;
                    }
                });

                ref readonly var renderCfg = ref w.Get<RenderConfig>();
                var bossFx = renderCfg.BossEffects;
                ref readonly var arenaCfg = ref w.Get<ArenaConfig>();

                TelegraphRenderHelper.CurrentEffects = bossFx;
                TelegraphRenderHelper.CurrentArena = arenaCfg;

                ref readonly var bTrans = ref boss.Transform;
                ref readonly var bSprite = ref boss.Sprite;
                var bPos = (bTrans.Position + bSprite.JitterOffset).ToVec2i();

                // 1. 予兆前揺（Telegraph）描画
                if (boss.CurrentState.IsTelegraph)
                {
                    var progress = boss.AbilitySystem.PhaseProgress;
                    DrawBossTelegraph(boss, playerPos, progress);
                }

                var flipX = !boss.FacingRight;
                var rotation = bTrans.Rotation;

                // 2. 虚弱（Vulnerable）状態の脈動オーラとマーカー
                if (boss.IsVulnerable)
                {
                    DrawBossVulnerableEffects(
                        bPos, bSprite.SrcRect, bTrans.Scale, rotation,
                        bSprite.GraphHandle, flipX, boss.StateTimer, bossFx);
                }

                // 3. フェーズ遷移無敵結界演出
                if (boss.CurrentState.IsPhaseTransition)
                {
                    DrawBossPhaseTransitionEffects(
                        bPos, bSprite.SrcRect, bTrans.Scale, rotation,
                        bSprite.GraphHandle, flipX, boss.StateTimer, bossFx);
                }
            });

        return world;
    }

    private static void DrawBossTelegraph(
        BaseBossAgent boss,
        Vec2f playerPos,
        float progress)
    {
        var currentAbility = boss.AbilitySystem.CurrentAbility;
        if (currentAbility == null) return;

        var finalTarget = boss.TargetPosition != Vec2f.Zero ? boss.TargetPosition : playerPos;
        ref readonly var bTrans = ref boss.Transform;
        var bHeight = boss.CollisionBox.Size.Y;
        AbilityTelegraphContext ctx = new(bTrans.Position, finalTarget, boss.FacingRight, progress, boss.StateTimer, bHeight);
        currentAbility.DrawTelegraph(in ctx);
    }

    private static void DrawBossVulnerableEffects(
        Vec2i bPos,
        in Rect srcRect,
        Vec2f scale,
        double angle,
        int graphHandle,
        bool flipX,
        float stateTimer,
        BossEffectsRenderConfig bossFx)
    {
        var pulse = 0.5f + 0.5f * MathF.Sin(stateTimer * bossFx.VulnerablePulseSpeed);
        var vulnAlpha = (int)(bossFx.VulnerableAuraBaseAlpha + bossFx.VulnerableAuraPulseAlpha * pulse);
        GameEngine.SetDrawBlendMode(BlendMode.Add, vulnAlpha);
        GameEngine.DrawRectRotaGraph(
            bPos,
            new Vec2i(srcRect.X1, srcRect.Y1),
            new Vec2i(srcRect.Width, srcRect.Height),
            scale * bossFx.VulnerableAuraScale,
            angle,
            graphHandle,
            true,
            flipX);
        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        var bobbing = MathF.Sin(stateTimer * bossFx.ChanceMarkerBobbingSpeed) * bossFx.ChanceMarkerBobbingAmplitude;
        var textWidth = bossFx.ChanceMarkerText.Length * bossFx.ChanceMarkerCharWidth;
        var textPos = new Vec2i(bPos.X - textWidth / 2, (int)(bPos.Y - bossFx.ChanceMarkerOffsetY + bobbing));
        GameEngine.DrawString(textPos, bossFx.ChanceMarkerText, bossFx.ChanceMarkerColor);
    }

    private static void DrawBossPhaseTransitionEffects(
        Vec2i bPos,
        in Rect srcRect,
        Vec2f scale,
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
        GameEngine.DrawRectRotaGraph(
            bPos,
            new Vec2i(srcRect.X1, srcRect.Y1),
            new Vec2i(srcRect.Width, srcRect.Height),
            scale * bossFx.InvincibleAuraScale,
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
        GameEngine.DrawCircle(bPos, currentShieldRadius, ColorPalette.Gold, true);

        var shieldRingAlpha = (int)(bossFx.InvincibleRingBaseAlpha + bossFx.InvincibleRingPulseAlpha * pulse);
        GameEngine.SetDrawBlendMode(BlendMode.Add, shieldRingAlpha);
        GameEngine.DrawCircle(bPos, currentShieldRadius, ColorPalette.GoldLight, false);
        if (currentShieldRadius > 1)
        {
            GameEngine.DrawCircle(bPos, currentShieldRadius - 1, ColorPalette.Gold, false);
        }

        var ripple1 = (stateTimer * bossFx.InvincibleRippleSpeed) % 1.0f;
        var ripple1Radius = (int)(currentShieldRadius * (bossFx.InvincibleRippleStartRatio + bossFx.InvincibleRippleExpandRatio * ripple1));
        var ripple1Alpha = (int)(bossFx.InvincibleRippleMaxAlpha * (1.0f - ripple1));
        if (ripple1Alpha > 0)
        {
            GameEngine.SetDrawBlendMode(BlendMode.Add, ripple1Alpha);
            GameEngine.DrawCircle(bPos, ripple1Radius, ColorPalette.GoldLight, false);
        }

        var ripple2 = (stateTimer * bossFx.InvincibleRippleSpeed + 0.5f) % 1.0f;
        var ripple2Radius = (int)(currentShieldRadius * (bossFx.InvincibleRippleStartRatio + bossFx.InvincibleRippleExpandRatio * ripple2));
        var ripple2Alpha = (int)(bossFx.InvincibleRippleMaxAlpha * (1.0f - ripple2));
        if (ripple2Alpha > 0)
        {
            GameEngine.SetDrawBlendMode(BlendMode.Add, ripple2Alpha);
            GameEngine.DrawCircle(bPos, ripple2Radius, ColorPalette.White, false);
        }

        var orbitAngle = stateTimer * bossFx.InvincibleOrbitSpeed;
        const float angleStep = MathF.PI * 0.5f;

        Span<Vec2i> nodes = stackalloc Vec2i[4];
        for (var i = 0; i < 4; i++)
        {
            var theta = orbitAngle + i * angleStep;
            nodes[i] = new Vec2i(
                (int)(bPos.X + MathF.Cos(theta) * currentShieldRadius),
                (int)(bPos.Y + MathF.Sin(theta) * currentShieldRadius));
        }

        var prismAlpha = (int)(bossFx.InvinciblePrismAlphaBase + bossFx.InvinciblePrismAlphaPulse * pulse);
        GameEngine.SetDrawBlendMode(BlendMode.Add, prismAlpha);
        for (var i = 0; i < 4; i++)
        {
            var next = (i + 1) % 4;
            GameEngine.DrawLine(nodes[i], nodes[next], ColorPalette.GoldLight, bossFx.InvinciblePrismThickness);
        }

        for (var i = 0; i < 4; i++)
        {
            GameEngine.SetDrawBlendMode(BlendMode.Add, bossFx.InvincibleNodeAlpha);
            GameEngine.DrawCircle(nodes[i], bossFx.InvincibleNodeOuterRadius, ColorPalette.GoldLight, true);
            GameEngine.DrawCircle(nodes[i], bossFx.InvincibleNodeInnerRadius, ColorPalette.White, true);
        }

        var crossAngle = stateTimer * bossFx.InvincibleCrossAngleSpeed;
        var flareLength = currentShieldRadius * bossFx.InvincibleCrossLengthRatio;
        var crossAlpha = (int)(bossFx.InvincibleCrossAlphaBase + bossFx.InvincibleCrossAlphaPulse * pulse);
        GameEngine.SetDrawBlendMode(BlendMode.Add, crossAlpha);
        for (var i = 0; i < 4; i++)
        {
            var theta = crossAngle + i * angleStep;
            var flarePos = new Vec2i(
                (int)(bPos.X + MathF.Cos(theta) * flareLength),
                (int)(bPos.Y + MathF.Sin(theta) * flareLength));
            GameEngine.DrawLine(bPos, flarePos, ColorPalette.GoldLight, bossFx.InvincibleCrossThickness);
        }

        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
    }
}
