using System;
using System.Drawing;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Systems.Render;

/// <summary>
///     自機弾および Boss 弾幕（通常弾・特殊弾幕）を高速一括描画する ECS システム。
///     零 GC コールバックおよび視口カリングを活用し、海量弾幕を描画バッファへ直接記録します。
/// </summary>
public static class BulletBatchRenderSystem {
    private static int _clipMinX;
    private static int _clipMaxX;
    private static int _clipMinY;
    private static int _clipMaxY;

    private static int _pOuterRadius;
    private static Color _pOuterColor;
    private static int _pInnerRadius;
    private static Color _pInnerColor;
    private static int _pTrailOffset;
    private static int _pTrailRadius1;
    private static int _pTrailRadius2;
    private static int _pTrailRadius3;
    private static Color _pTrailColor1;
    private static Color _pTrailColor2;
    private static Color _pTrailColor3;
    private static float _pTrailOffset1;
    private static float _pTrailOffset2;
    private static float _pTrailOffset3;

    private static int _bOutlinePadding;
    private static float _bCoreRadiusRatio;
    private static int _bCoreMinRadius;
    private static Color _bCoreColor;

    /// <summary>
    ///     弾幕バッチ描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddBulletBatchRenderSystem(this World world)
    {
        world.System<Transform, Velocity>("PlayerBulletRenderSystem")
            .With<PlayerBulletTag>()
            .Without<RecycleTag>()
            .Kind(Ecs.OnStore)
            .Iter(DrawPlayerBulletsBatch);

        world.System<Transform, Bullet, BulletModifier>("BossBulletRenderSystem")
            .With<BossBulletTag>()
            .Without<RecycleTag>()
            .Kind(Ecs.OnStore)
            .Iter(DrawBossBulletsBatch);

        return world;
    }

    private static void UpdateContext(BulletRenderConfig bulletCfg, ArenaConfig arenaCfg)
    {
        _clipMinX = (int)arenaCfg.BulletDespawnMinX - bulletCfg.ViewportCullingMarginX;
        _clipMaxX = (int)arenaCfg.BulletDespawnMaxX + bulletCfg.ViewportCullingMarginX;
        _clipMinY = (int)arenaCfg.BulletDespawnMinY - bulletCfg.ViewportCullingMarginY;
        _clipMaxY = (int)arenaCfg.BulletDespawnMaxY + bulletCfg.ViewportCullingMarginY;

        _pOuterRadius = (int)bulletCfg.PlayerBulletOuterRadius;
        _pOuterColor = bulletCfg.PlayerBulletOuterColor;
        _pInnerRadius = (int)bulletCfg.PlayerBulletInnerRadius;
        _pInnerColor = bulletCfg.PlayerBulletInnerColor;
        _pTrailOffset = bulletCfg.PlayerBulletTrailHalfWidth;

        var trailBase = bulletCfg.PlayerBulletTrailColor.A > 0 ? bulletCfg.PlayerBulletTrailColor : _pOuterColor;
        var tr = trailBase.R;
        var tg = trailBase.G;
        var tb = trailBase.B;

        _pTrailColor1 = Color.FromArgb(bulletCfg.PlayerBulletTrailAlpha1, tr, tg, tb);
        _pTrailColor2 = Color.FromArgb(bulletCfg.PlayerBulletTrailAlpha2, tr, tg, tb);
        _pTrailColor3 = Color.FromArgb(bulletCfg.PlayerBulletTrailAlpha3, tr, tg, tb);

        _pTrailRadius1 = Math.Max(1, (int)MathF.Round(_pOuterRadius * bulletCfg.PlayerBulletTrailRadiusRatio1));
        _pTrailRadius2 = Math.Max(1, (int)MathF.Round(_pOuterRadius * bulletCfg.PlayerBulletTrailRadiusRatio2));
        _pTrailRadius3 = Math.Max(1, (int)MathF.Round(_pOuterRadius * bulletCfg.PlayerBulletTrailRadiusRatio3));

        _pTrailOffset1 = _pTrailOffset * bulletCfg.PlayerBulletTrailOffsetRatio1;
        _pTrailOffset2 = _pTrailOffset * bulletCfg.PlayerBulletTrailOffsetRatio2;
        _pTrailOffset3 = _pTrailOffset * bulletCfg.PlayerBulletTrailOffsetRatio3;

        _bOutlinePadding = bulletCfg.BossBulletOutlinePadding;
        _bCoreRadiusRatio = bulletCfg.BossBulletCoreRadiusRatio;
        _bCoreMinRadius = bulletCfg.BossBulletCoreMinRadius;
        _bCoreColor = bulletCfg.BossBulletCoreColor;
    }

    private static void DrawPlayerBulletsBatch(Iter it, Field<Transform> trans, Field<Velocity> vel)
    {
        var w = it.World();
        if (w.Get<GameFlow>().State != GameState.Battle) return;

        var count = it.Count();
        if (count == 0) return;

        UpdateContext(w.Get<RenderConfig>().Bullets, w.Get<ArenaConfig>());

        for (var i = 0; i < count; ++i)
        {
            var px = (int)trans[i].X;
            var py = (int)trans[i].Y;
            if (px < _clipMinX || px > _clipMaxX || py < _clipMinY || py > _clipMaxY) continue;

            var vx = vel[i].Vx;
            var vy = vel[i].Vy;
            var speedSq = vx * vx + vy * vy;
            if (speedSq > 1.0f)
            {
                var invSpeed = 1.0f / MathF.Sqrt(speedSq);
                var dirX = vx * invSpeed;
                var dirY = vy * invSpeed;

                GameEngine.DrawCircle(
                    (int)(px - dirX * _pTrailOffset3),
                    (int)(py - dirY * _pTrailOffset3),
                    _pTrailRadius3, _pTrailColor3, true);

                GameEngine.DrawCircle(
                    (int)(px - dirX * _pTrailOffset2),
                    (int)(py - dirY * _pTrailOffset2),
                    _pTrailRadius2, _pTrailColor2, true);

                GameEngine.DrawCircle(
                    (int)(px - dirX * _pTrailOffset1),
                    (int)(py - dirY * _pTrailOffset1),
                    _pTrailRadius1, _pTrailColor1, true);
            }

            GameEngine.DrawCircle(px, py, _pOuterRadius, _pOuterColor, true);
            GameEngine.DrawCircle(px, py, _pInnerRadius, _pInnerColor, true);
        }
    }

    private static void DrawBossBulletsBatch(Iter it, Field<Transform> trans, Field<Bullet> bullet, Field<BulletModifier> modifier)
    {
        var w = it.World();
        if (w.Get<GameFlow>().State != GameState.Battle) return;

        var count = it.Count();
        if (count == 0) return;

        var bulletCfg = w.Get<RenderConfig>().Bullets;
        UpdateContext(bulletCfg, w.Get<ArenaConfig>());

        for (var i = 0; i < count; ++i)
        {
            var bx = (int)trans[i].X;
            var by = (int)trans[i].Y;
            if (bx < _clipMinX || bx > _clipMaxX || by < _clipMinY || by > _clipMaxY) continue;

            var b = bullet[i];
            ref readonly var mod = ref modifier[i];
            var radius = (int)b.Radius;
            var outlineRadius = radius + _bOutlinePadding;
            var coreRadius = Math.Max(_bCoreMinRadius, (int)(radius * _bCoreRadiusRatio));

            if (mod.Type == BulletType.Normal)
            {
                GameEngine.DrawCircle(bx, by, outlineRadius, b.OutlineColor, true);
                GameEngine.DrawCircle(bx, by, radius, b.FillColor, true);
                GameEngine.DrawCircle(bx, by, coreRadius, _bCoreColor, true);
            }
            else
            {
                DrawSpecialBossBullet(bx, by, in b, in mod, radius, outlineRadius, coreRadius, _bOutlinePadding, _bCoreColor, bulletCfg);
            }
        }
    }

    private static void DrawSpecialBossBullet(
        int bx,
        int by,
        in Bullet b,
        in BulletModifier mod,
        int radius,
        int outlineRadius,
        int coreRadius,
        int outlinePadding,
        Color coreColor,
        BulletRenderConfig bulletCfg)
    {
        switch (mod.Type)
        {
            case BulletType.Magenta: {
                var pulseAura = radius * (bulletCfg.SpecialMagentaAuraBaseRatio + bulletCfg.SpecialMagentaAuraPulseRatio * MathF.Sin(mod.ElapsedTimer * bulletCfg.SpecialMagentaPulseSpeed));
                var auraColor = Color.FromArgb(bulletCfg.SpecialMagentaAuraAlpha, b.FillColor.R, b.FillColor.G, b.FillColor.B);
                GameEngine.DrawCircle(bx, by, (int)pulseAura, auraColor, false);
                GameEngine.DrawCircle(bx, by, outlineRadius, b.OutlineColor, true);
                GameEngine.DrawCircle(bx, by, radius, b.FillColor, true);
                GameEngine.DrawCircle(bx, by, coreRadius, coreColor, true);
                break;
            }

            case BulletType.Yellow: {
                GameEngine.DrawCircle(bx, by, outlineRadius, b.OutlineColor, true);
                GameEngine.DrawCircle(bx, by, radius, b.FillColor, true);
                GameEngine.DrawCircle(bx, by, coreRadius, coreColor, true);
                break;
            }

            case BulletType.Cyan: {
                GameEngine.DrawCircle(bx, by, outlineRadius + bulletCfg.SpecialCyanRingPadding, bulletCfg.SpecialCyanRingColor, false);
                GameEngine.DrawCircle(bx, by, outlineRadius, b.OutlineColor, true);
                GameEngine.DrawCircle(bx, by, radius, b.FillColor, true);
                GameEngine.DrawCircle(bx, by, coreRadius, coreColor, true);
                break;
            }

            case BulletType.Black: {
                var pulseAura = radius * (bulletCfg.SpecialBlackAuraBaseRatio + bulletCfg.SpecialBlackAuraPulseRatio * MathF.Sin(mod.ElapsedTimer * bulletCfg.SpecialBlackPulseSpeed));
                GameEngine.DrawCircle(bx, by, (int)pulseAura, bulletCfg.SpecialBlackAuraColor, false);

                var orbitDist = radius * bulletCfg.SpecialBlackSatelliteOrbitRatio;
                var angle = mod.ElapsedTimer * bulletCfg.SpecialBlackSatelliteOrbitSpeed;
                var satX = (int)(bx + MathF.Cos(angle) * orbitDist);
                var satY = (int)(by + MathF.Sin(angle) * orbitDist);
                var satRadius = Math.Max(bulletCfg.SpecialBlackSatelliteMinRadius, (int)(radius * bulletCfg.SpecialBlackSatelliteRadiusRatio));
                var satOutline = satRadius + outlinePadding;

                GameEngine.DrawCircle(satX, satY, satOutline, b.OutlineColor, true);
                GameEngine.DrawCircle(satX, satY, satRadius, b.FillColor, true);

                GameEngine.DrawCircle(bx, by, outlineRadius, b.OutlineColor, true);
                GameEngine.DrawCircle(bx, by, radius, b.FillColor, true);
                GameEngine.DrawCircle(bx, by, Math.Max(bulletCfg.SpecialBlackCoreInnerMinRadius, coreRadius / bulletCfg.SpecialBlackCoreInnerDivisor), bulletCfg.SpecialBlackCoreInnerColor, true);
                break;
            }

            default: {
                GameEngine.DrawCircle(bx, by, outlineRadius, b.OutlineColor, true);
                GameEngine.DrawCircle(bx, by, radius, b.FillColor, true);
                GameEngine.DrawCircle(bx, by, coreRadius, coreColor, true);
                break;
            }
        }
    }
}
