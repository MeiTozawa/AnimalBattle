using System.Drawing;

namespace AnimalBattle.Presentation.Vfx;

/// <summary>
///     純粋な視覚演出・パーティクル・残像エフェクトの生成を一元管理する静的サービスです。
///     ダメージ判定：Hitboxを持たない瞬態エフェクトエンティティの生成を担当します。
/// </summary>
public static class EffectSpawner
{
    /// <summary>
    ///     二段ジャンプ時の空気リングエフェクトを生成します。
    /// </summary>
    public static void SpawnJumpRingEffect(World world, Vec2f pos)
    {
        var pCfg = world.GetActivePlayerConfig();
        var vfxCfg = world.Get<VisualEffectsRenderConfig>();
        CombatPoolService.RentRingEffect()
           .Set(new Transform(pos))
           .Set(new RingEffect(pCfg.JumpRingInitialRadius, pCfg.JumpRingMaxRadius, vfxCfg.JumpRingColor))
           .Set(new FadeEffect(pCfg.JumpRingDuration));
    }

    /// <summary>
    ///     ダッシュ高速移動時の半透明速度残像：Ghost Trailエフェクトを生成します。
    /// </summary>
    public static void SpawnGhostTrail(
        World world,
        Vec2f pos,
        Vec2f scale,
        float rotation,
        int graphHandle,
        Rect srcRect,
        bool flipX,
        Color color,
        float duration)
    {
        var pCfg = world.GetActivePlayerConfig();
        CombatPoolService.RentAreaEffect()
           .Set(new Transform(pos, scale, rotation))
           .Set(new Sprite(graphHandle, flipX, true, srcRect, color))
           .Set(new FadeEffect(duration, pCfg.DashGhostTrailAlpha, 0.0f));
    }

    /// <summary>
    ///     射撃出膛時のマズルフラッシュ：銃口閃光エフェクトを生成します。
    /// </summary>
    public static void SpawnMuzzleFlash(World world, Vec2f pos, float radius, float duration)
    {
        var vfxCfg = world.Get<VisualEffectsRenderConfig>();
        CombatPoolService.RentSparkEffect()
           .Set(new Transform(pos))
           .Set(new Velocity(Vec2f.Zero))
           .Set(new VfxParticle(radius, vfxCfg.MuzzleFlashFillColor, vfxCfg.MuzzleFlashOutlineColor))
           .Set(new FadeEffect(duration));
    }

    /// <summary>
    ///     被弾時のヒットスパークエフェクトを生成します。
    /// </summary>
    public static void SpawnHitSparkEffect(World world, Vec2f pos)
    {
        var vfxCfg = world.Get<VisualEffectsRenderConfig>();
        for (var i = 0; i < vfxCfg.HitSparkCount; i++)
        {
            var angle = Random.Shared.NextSingle() * MathF.PI * 2.0f;
            var speed = vfxCfg.HitSparkMinSpeed + Random.Shared.NextSingle() * vfxCfg.HitSparkSpeedRange;
            var vel = new Vec2f(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed);
            CombatPoolService.RentSparkEffect()
               .Set(new Transform(pos))
               .Set(new Velocity(vel))
               .Set(new VfxParticle(vfxCfg.HitSparkRadius, vfxCfg.HitSparkFillColor, vfxCfg.HitSparkOutlineColor))
               .Set(new FadeEffect(vfxCfg.HitSparkDuration));
        }
    }

    /// <summary>
    ///     弾丸が Boss に命中・撃砕された際の飛散破片パーティクル群：Impact Splatterを生成します。
    /// </summary>
    public static void SpawnBulletImpactSplatter(World world, Vec2f pos, Vec2f hitDir)
    {
        var pCfg = world.GetActivePlayerConfig();
        var vfxCfg = world.Get<VisualEffectsRenderConfig>();
        var count = Random.Shared.Next(pCfg.ImpactSplatterMinCount, pCfg.ImpactSplatterMaxCount + 1);
        var baseAngle = MathF.Atan2(-hitDir.Y, -hitDir.X);

        for (var i = 0; i < count; i++)
        {
            var spreadAngle = baseAngle + (Random.Shared.NextSingle() - 0.5f) * MathF.PI * 0.8f;
            var speed = pCfg.ImpactSplatterMinSpeed + Random.Shared.NextSingle() * (pCfg.ImpactSplatterMaxSpeed - pCfg.ImpactSplatterMinSpeed);
            var vel = new Vec2f(MathF.Cos(spreadAngle) * speed, MathF.Sin(spreadAngle) * speed);

            CombatPoolService.RentSparkEffect()
               .Set(new Transform(pos))
               .Set(new Velocity(vel))
               .Set(new VfxParticle(pCfg.ImpactSplatterParticleRadius, vfxCfg.ImpactSplatterFillColor, vfxCfg.ImpactSplatterOutlineColor))
               .Set(new FadeEffect(pCfg.ImpactSplatterDuration));
        }
    }

    /// <summary>
    ///     プレイヤー死亡時の飛散パーティクルを生成します。
    /// </summary>
    public static void SpawnPlayerDeathParticles(World world, Vec2f pos)
    {
        var vfxCfg = world.Get<VisualEffectsRenderConfig>();
        for (var i = 0; i < vfxCfg.PlayerDeathParticleCount; i++)
        {
            var angle = i / (float)vfxCfg.PlayerDeathParticleCount * MathF.PI * 2.0f;
            var speed = vfxCfg.PlayerDeathParticleBaseSpeed + i % 3 * vfxCfg.PlayerDeathParticleSpeedStep;
            var vel = new Vec2f(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed);
            CombatPoolService.RentSparkEffect()
               .Set(new Transform(pos))
               .Set(new Velocity(vel))
               .Set(new VfxParticle(vfxCfg.PlayerDeathParticleRadius, vfxCfg.PlayerDeathParticleFillColor, vfxCfg.PlayerDeathParticleOutlineColor))
               .Set(new FadeEffect(vfxCfg.PlayerDeathParticleDuration));
        }
    }

    /// <summary>
    ///     Boss 撃破勝利時の祝賀爆発パーティクルを生成します。
    /// </summary>
    public static void SpawnVictoryExplosions(World world, Vec2f pos)
    {
        var vfxCfg = world.Get<VisualEffectsRenderConfig>();
        for (var i = 0; i < vfxCfg.VictoryExplosionCount; i++)
        {
            var angle = i / (float)vfxCfg.VictoryExplosionCount * MathF.PI * 2.0f;
            var speed = vfxCfg.VictoryExplosionBaseSpeed + i % 5 * vfxCfg.VictoryExplosionSpeedStep;
            var vel = new Vec2f(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed);
            CombatPoolService.RentSparkEffect()
               .Set(new Transform(pos))
               .Set(new Velocity(vel))
               .Set(new VfxParticle(vfxCfg.VictoryExplosionRadius, vfxCfg.VictoryExplosionFillColor, vfxCfg.VictoryExplosionOutlineColor))
               .Set(new FadeEffect(vfxCfg.VictoryExplosionDuration));
        }
    }

    /// <summary>
    ///     Phase 遷移時・着地衝撃時の清屏衝撃波エフェクト：視覚リングを生成します。
    /// </summary>
    public static void SpawnShockwave(World world, Vec2f pos)
    {
        var vfxCfg = world.Get<VisualEffectsRenderConfig>();
        CombatPoolService.RentRingEffect()
           .Set(new Transform(pos))
           .Set(new RingEffect(vfxCfg.ShockwaveInitialRadius, vfxCfg.ShockwaveMaxRadius, vfxCfg.ShockwaveRingColor))
           .Set(new FadeEffect(vfxCfg.ShockwaveDuration));
    }
    /// <summary>
    ///     スーパーアーマー（覇体）中のボスが被弾した際の装甲偏向・反震火花エフェクトを生成します。
    /// </summary>
    public static void SpawnSuperArmorDeflectEffect(World world, Vec2f pos)
    {
        // 1. 小型防御衝撃リング
        CombatPoolService.RentRingEffect()
           .Set(new Transform(pos))
           .Set(new RingEffect(8.0f, 32.0f, System.Drawing.Color.FromArgb(255, 255, 140, 40)))
           .Set(new FadeEffect(0.18f));

        // 2. 偏向スパーク火花
        for (var i = 0; i < 4; i++)
        {
            var angle = Random.Shared.NextSingle() * MathF.PI * 2.0f;
            var speed = 100.0f + Random.Shared.NextSingle() * 100.0f;
            var vel = new Vec2f(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed);
            CombatPoolService.RentSparkEffect()
               .Set(new Transform(pos))
               .Set(new Velocity(vel))
               .Set(new VfxParticle(3.5f, System.Drawing.Color.FromArgb(255, 255, 210, 80), System.Drawing.Color.FromArgb(255, 255, 80, 30)))
               .Set(new FadeEffect(0.22f));
        }
    }
}