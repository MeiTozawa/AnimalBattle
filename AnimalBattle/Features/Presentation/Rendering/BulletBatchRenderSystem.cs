using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Presentation.Rendering;

/// <summary>
///     弾丸エンティティの一括描画を行う ECS システム。
///     弾丸の描画処理を独立したクエリ・パスに集約し、
///     ビューポート外のカリングと一括描画コマンド投入を行います。
/// </summary>
public static class BulletBatchRenderSystem {
    // 毎フレームのGCアロケーションを防ぐための再利用可能なコンパクトバッファ
    private static CompactCirclePacket[] _circleBatchBuffer = new CompactCirclePacket[2048];
    private static int _circleBatchCount;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EnsureBatchBufferSize(int required)
    {
        if (_circleBatchBuffer.Length < required)
        {
            var newCap = Math.Max(_circleBatchBuffer.Length * 2, required);
            Array.Resize(ref _circleBatchBuffer, newCap);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PushBatchCircle(Vec2i center, int radius, uint color, bool fill)
    {
        EnsureBatchBufferSize(_circleBatchCount + 1);
        _circleBatchBuffer[_circleBatchCount++] = new CompactCirclePacket(center, radius, color, fill);
    }
    private static int _clipMinX;
    private static int _clipMaxX;
    private static int _clipMinY;
    private static int _clipMaxY;

    private static int _pOuterRadius;
    private static Color _pOuterColor;
    private static int _pInnerRadius;
    private static Color _pInnerColor;
    private static float _pTrailOffset;
    private static Color _pTrailColor1;
    private static Color _pTrailColor2;
    private static Color _pTrailColor3;
    private static int _pTrailRadius1;
    private static int _pTrailRadius2;
    private static int _pTrailRadius3;
    private static float _pTrailOffset1;
    private static float _pTrailOffset2;
    private static float _pTrailOffset3;

    private static int _bOutlinePadding;
    private static float _bCoreRadiusRatio;
    private static int _bCoreMinRadius;
    private static Color _bCoreColor;

    /// <summary>
    ///     弾丸描画システム：自機弾丸、Boss 弾丸を Flecs.NET ワールドへ登録します。
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
        _clipMinX = (int)arenaCfg.BulletDespawnMinX - bulletCfg.ViewportCullingMargin.X;
        _clipMaxX = (int)arenaCfg.BulletDespawnMaxX + bulletCfg.ViewportCullingMargin.X;
        _clipMinY = (int)arenaCfg.BulletDespawnMinY - bulletCfg.ViewportCullingMargin.Y;
        _clipMaxY = (int)arenaCfg.BulletDespawnMaxY + bulletCfg.ViewportCullingMargin.Y;

        _pOuterRadius = (int)bulletCfg.PlayerBulletOuterRadius;
        _pOuterColor = bulletCfg.PlayerBulletOuterColor;
        _pInnerRadius = (int)bulletCfg.PlayerBulletInnerRadius;
        _pInnerColor = bulletCfg.PlayerBulletInnerColor;
        _pTrailOffset = bulletCfg.PlayerBulletTrailHalfSize.X;

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
        _circleBatchCount = 0;

        for (var i = 0; i < count; ++i)
        {
            var p = trans[i].Position.ToVec2i();
            if (p.X < _clipMinX || p.X > _clipMaxX || p.Y < _clipMinY || p.Y > _clipMaxY) continue;

            var v = vel[i].Value;
            var speedSq = v.LengthSquared();
            if (speedSq > 1.0f)
            {
                var dir = v.Normalized();

                PushBatchCircle((p.ToVec2f() - dir * _pTrailOffset3).RoundToVec2i(), _pTrailRadius3, _pTrailColor3.ToUint(), true);
                PushBatchCircle((p.ToVec2f() - dir * _pTrailOffset2).RoundToVec2i(), _pTrailRadius2, _pTrailColor2.ToUint(), true);
                PushBatchCircle((p.ToVec2f() - dir * _pTrailOffset1).RoundToVec2i(), _pTrailRadius1, _pTrailColor1.ToUint(), true);
            }

            PushBatchCircle(p, _pOuterRadius, _pOuterColor.ToUint(), true);
            PushBatchCircle(p, _pInnerRadius, _pInnerColor.ToUint(), true);
        }

        if (_circleBatchCount > 0)
        {
            GameEngine.DrawCirclesBatch(_circleBatchBuffer.AsSpan(0, _circleBatchCount));
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
        _circleBatchCount = 0;

        for (var i = 0; i < count; ++i)
        {
            var bp = trans[i].Position.ToVec2i();
            if (bp.X < _clipMinX || bp.X > _clipMaxX || bp.Y < _clipMinY || bp.Y > _clipMaxY) continue;

            var b = bullet[i];
            ref readonly var mod = ref modifier[i];
            var radius = (int)b.Radius;
            var outlineRadius = radius + _bOutlinePadding;
            var coreRadius = Math.Max(_bCoreMinRadius, (int)(radius * _bCoreRadiusRatio));

            if (mod.Type == BulletType.Normal)
            {
                PushBatchCircle(bp, outlineRadius, b.OutlineColor.ToUint(), true);
                PushBatchCircle(bp, radius, b.FillColor.ToUint(), true);
                PushBatchCircle(bp, coreRadius, _bCoreColor.ToUint(), true);
            }
            else
            {
                DrawSpecialBossBullet(bp, in b, in mod, radius, outlineRadius, coreRadius, _bOutlinePadding, _bCoreColor, bulletCfg);
            }
        }

        if (_circleBatchCount > 0)
        {
            GameEngine.DrawCirclesBatch(_circleBatchBuffer.AsSpan(0, _circleBatchCount));
        }
    }

    private static void DrawSpecialBossBullet(
        Vec2i bp,
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
                PushBatchCircle(bp, (int)pulseAura, auraColor.ToUint(), false);
                PushBatchCircle(bp, outlineRadius, b.OutlineColor.ToUint(), true);
                PushBatchCircle(bp, radius, b.FillColor.ToUint(), true);
                PushBatchCircle(bp, coreRadius, coreColor.ToUint(), true);
                break;
            }

            case BulletType.Yellow: {
                PushBatchCircle(bp, outlineRadius, b.OutlineColor.ToUint(), true);
                PushBatchCircle(bp, radius, b.FillColor.ToUint(), true);
                PushBatchCircle(bp, coreRadius, coreColor.ToUint(), true);
                break;
            }

            case BulletType.Cyan: {
                PushBatchCircle(bp, outlineRadius + bulletCfg.SpecialCyanRingPadding, bulletCfg.SpecialCyanRingColor.ToUint(), false);
                PushBatchCircle(bp, outlineRadius, b.OutlineColor.ToUint(), true);
                PushBatchCircle(bp, radius, b.FillColor.ToUint(), true);
                PushBatchCircle(bp, coreRadius, coreColor.ToUint(), true);
                break;
            }

            case BulletType.Black: {
                var pulseAura = radius * (bulletCfg.SpecialBlackAuraBaseRatio + bulletCfg.SpecialBlackAuraPulseRatio * MathF.Sin(mod.ElapsedTimer * bulletCfg.SpecialBlackPulseSpeed));
                PushBatchCircle(bp, (int)pulseAura, bulletCfg.SpecialBlackAuraColor.ToUint(), false);

                var orbitDist = radius * bulletCfg.SpecialBlackSatelliteOrbitRatio;
                var angle = mod.ElapsedTimer * bulletCfg.SpecialBlackSatelliteOrbitSpeed;
                var satPos = (bp.ToVec2f() + new Vec2f(MathF.Cos(angle) * orbitDist, MathF.Sin(angle) * orbitDist)).RoundToVec2i();
                var satRadius = Math.Max(bulletCfg.SpecialBlackSatelliteMinRadius, (int)(radius * bulletCfg.SpecialBlackSatelliteRadiusRatio));
                var satOutline = satRadius + outlinePadding;

                PushBatchCircle(satPos, satOutline, b.OutlineColor.ToUint(), true);
                PushBatchCircle(satPos, satRadius, b.FillColor.ToUint(), true);

                PushBatchCircle(bp, outlineRadius, b.OutlineColor.ToUint(), true);
                PushBatchCircle(bp, radius, b.FillColor.ToUint(), true);
                PushBatchCircle(bp, Math.Max(bulletCfg.SpecialBlackCoreInnerMinRadius, coreRadius / bulletCfg.SpecialBlackCoreInnerDivisor), bulletCfg.SpecialBlackCoreInnerColor.ToUint(), true);
                break;
            }

            default: {
                PushBatchCircle(bp, outlineRadius, b.OutlineColor.ToUint(), true);
                PushBatchCircle(bp, radius, b.FillColor.ToUint(), true);
                PushBatchCircle(bp, coreRadius, coreColor.ToUint(), true);
                break;
            }
        }
    }
}
