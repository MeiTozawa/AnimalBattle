using System.Drawing;
using AnimalBattle.Framework.Pooling;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Core.Geometry;
using Flecs.NET.Core;

namespace AnimalBattle.Effects;

/// <summary>
///     純粋な視覚演出・パーティクル・残像エフェクトの生成を一元管理する静的サービスです。
///     ダメージ判定（Hitbox）を持たない瞬態エフェクトエンティティの生成を担当します。
/// </summary>
public static class EffectSpawner
{
    /// <summary>
    ///     二段ジャンプ時の空気リングエフェクトを生成します。
    /// </summary>
    public static void SpawnJumpRingEffect(World world, float x, float y)
    {
        var pCfg = world.GetActivePlayerConfig();
        CombatPoolService.RentRingEffect()
           .Set(new Transform(x, y))
           .Set(new RingEffect(pCfg.JumpRingInitialRadius, pCfg.JumpRingMaxRadius, ColorPalette.White))
           .Set(new FadeEffect(pCfg.JumpRingDuration));
    }

    /// <summary>
    ///     ダッシュ高速移動時の半透明速度残像（Ghost Trail）エフェクトを生成します。
    /// </summary>
    public static void SpawnGhostTrail(
        World world,
        float x,
        float y,
        float scaleX,
        float scaleY,
        float rotation,
        int graphHandle,
        Rect srcRect,
        bool flipX,
        Color color,
        float duration)
    {
        var pCfg = world.GetActivePlayerConfig();
        CombatPoolService.RentAreaEffect()
           .Set(new Transform(x, y, scaleX, scaleY, rotation))
           .Set(new Velocity(0.0f))
           .Set(new Hitbox(default, 0.0f, Faction.Friendly, false))
           .Set(new Sprite(graphHandle, flipX, true, srcRect, color))
           .Set(new FadeEffect(duration, pCfg.DashGhostTrailAlpha, 0.0f));
    }

    /// <summary>
    ///     射撃出膛時のマズルフラッシュ（銃口閃光）エフェクトを生成します。
    /// </summary>
    public static void SpawnMuzzleFlash(World world, float x, float y)
    {
        var pCfg = world.GetActivePlayerConfig();
        CombatPoolService.RentSparkEffect()
           .Set(new Transform(x, y))
           .Set(new Velocity(0.0f))
           .Set(new Bullet(pCfg.MuzzleFlashRadius, ColorPalette.MuzzleFlashFill, ColorPalette.White))
           .Set(new FadeEffect(pCfg.MuzzleFlashDuration));
    }

    /// <summary>
    ///     被弾時のヒットスパークエフェクトを生成します。
    /// </summary>
    public static void SpawnHitSparkEffect(World world, float x, float y)
    {
        var vfxCfg = world.Get<RenderConfig>().VisualEffects;
        for (var i = 0; i < vfxCfg.HitSparkCount; i++)
        {
            var angle = Random.Shared.NextSingle() * MathF.PI * 2.0f;
            var speed = vfxCfg.HitSparkMinSpeed + Random.Shared.NextSingle() * vfxCfg.HitSparkSpeedRange;
            CombatPoolService.RentSparkEffect()
               .Set(new Transform(x, y))
               .Set(new Velocity(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed))
               .Set(new Bullet(vfxCfg.HitSparkRadius, ColorPalette.SparkYellow, ColorPalette.White))
               .Set(new FadeEffect(vfxCfg.HitSparkDuration));
        }
    }

    /// <summary>
    ///     弾丸が Boss に命中・撃砕された際の飛散破片パーティクル群（Impact Splatter）を生成します。
    /// </summary>
    public static void SpawnBulletImpactSplatter(World world, float x, float y, float hitDirX, float hitDirY)
    {
        var pCfg = world.GetActivePlayerConfig();
        var count = Random.Shared.Next(pCfg.ImpactSplatterMinCount, pCfg.ImpactSplatterMaxCount + 1);
        var baseAngle = MathF.Atan2(-hitDirY, -hitDirX);

        for (var i = 0; i < count; i++)
        {
            var spreadAngle = baseAngle + (Random.Shared.NextSingle() - 0.5f) * MathF.PI * 0.8f;
            var speed = pCfg.ImpactSplatterMinSpeed + Random.Shared.NextSingle() * (pCfg.ImpactSplatterMaxSpeed - pCfg.ImpactSplatterMinSpeed);
            var vx = MathF.Cos(spreadAngle) * speed;
            var vy = MathF.Sin(spreadAngle) * speed;

            CombatPoolService.RentSparkEffect()
               .Set(new Transform(x, y))
               .Set(new Velocity(vx, vy))
               .Set(new Bullet(pCfg.ImpactSplatterParticleRadius, ColorPalette.ImpactSplatterFill, ColorPalette.White))
               .Set(new FadeEffect(pCfg.ImpactSplatterDuration));
        }
    }

    /// <summary>
    ///     プレイヤー死亡時の飛散パーティクルを生成します。
    /// </summary>
    public static void SpawnPlayerDeathParticles(World world, float x, float y)
    {
        var vfxCfg = world.Get<RenderConfig>().VisualEffects;
        for (var i = 0; i < vfxCfg.PlayerDeathParticleCount; i++)
        {
            var angle = i / (float)vfxCfg.PlayerDeathParticleCount * MathF.PI * 2.0f;
            var speed = vfxCfg.PlayerDeathParticleBaseSpeed + i % 3 * vfxCfg.PlayerDeathParticleSpeedStep;
            CombatPoolService.RentSparkEffect()
               .Set(new Transform(x, y))
               .Set(new Velocity(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed))
               .Set(new Bullet(vfxCfg.PlayerDeathParticleRadius, ColorPalette.Red, ColorPalette.White))
               .Set(new FadeEffect(vfxCfg.PlayerDeathParticleDuration));
        }
    }

    /// <summary>
    ///     Boss 撃破勝利時の祝賀爆発パーティクルを生成します。
    /// </summary>
    public static void SpawnVictoryExplosions(World world, float x, float y)
    {
        var vfxCfg = world.Get<RenderConfig>().VisualEffects;
        for (var i = 0; i < vfxCfg.VictoryExplosionCount; i++)
        {
            var angle = i / (float)vfxCfg.VictoryExplosionCount * MathF.PI * 2.0f;
            var speed = vfxCfg.VictoryExplosionBaseSpeed + i % 5 * vfxCfg.VictoryExplosionSpeedStep;
            CombatPoolService.RentSparkEffect()
               .Set(new Transform(x, y))
               .Set(new Velocity(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed))
               .Set(new Bullet(vfxCfg.VictoryExplosionRadius, ColorPalette.Gold, ColorPalette.GoldLight))
               .Set(new FadeEffect(vfxCfg.VictoryExplosionDuration));
        }
    }

    /// <summary>
    ///     Phase 遷移時・着地衝撃時の清屏衝撃波エフェクト（視覚リング）を生成します。
    /// </summary>
    public static void SpawnShockwave(World world, float x, float y)
    {
        var vfxCfg = world.Get<RenderConfig>().VisualEffects;
        CombatPoolService.RentRingEffect()
           .Set(new Transform(x, y))
           .Set(new RingEffect(vfxCfg.ShockwaveInitialRadius, vfxCfg.ShockwaveMaxRadius, ColorPalette.Cyan))
           .Set(new FadeEffect(vfxCfg.ShockwaveDuration));
    }
}
