using System.Drawing;
using AnimalBattle.Configs;
using AnimalBattle.Events;
using AnimalBattle.Framework.Pooling;
using AnimalBattle.Logging;
using AnimalBattle.Presentation.Vfx.Components;

namespace AnimalBattle.Presentation.Vfx;

/// <summary>
///     ゲーム内ドメインイベントを購読し、対応する視覚演出・パーティクル・衝撃波エフェクトを
///     戦闘エンティティプールから生成・配置する表現層視覚フィードバックマネージャー。
///     コンストラクタでイベント購読を開始し、<see cref="Dispose"/> で確実に解除します。
/// </summary>
public sealed class VfxFeedback : IDisposable
{
    private World _world;
    private bool _isDisposed;

    /// <summary>
    ///     <see cref="VfxFeedback"/> の新しいインスタンスを初期化し、全視覚ドメインイベントの購読を開始します。
    /// </summary>
    /// <param name="world">エフェクトエンティティが所属する Flecs.NET ワールド。</param>
    public VfxFeedback(World world)
    {
        _world = world;
        RegisterEventHandlers();
        Log.Info("VfxFeedback", "[VfxFeedback] 視覚演出イベント購読を開始しました。");
    }

    /// <summary>
    ///     全ドメインイベントの購読を解除し、リソースを解放します。
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;
        UnregisterEventHandlers();
        _isDisposed = true;
        Log.Info("VfxFeedback", "[VfxFeedback] 視覚演出イベント購読を解除しました。");
    }

    private void RegisterEventHandlers()
    {
        UnregisterEventHandlers();

        EventBus.Subscribe<PlayerDoubleJumpedEvent>(OnPlayerDoubleJumped);
        EventBus.Subscribe<PlayerShotFiredEvent>(OnPlayerShotFired);
        EventBus.Subscribe<CombatHitLandedEvent>(OnCombatHitLanded);
        EventBus.Subscribe<BulletCancelledEvent>(OnBulletCancelled);
        EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
        EventBus.Subscribe<BossDiedEvent>(OnBossDied);
        EventBus.Subscribe<BossDiveImpactEvent>(OnBossDiveImpact);
        EventBus.Subscribe<SuperArmorDeflectedEvent>(OnSuperArmorDeflected);
    }

    private void UnregisterEventHandlers()
    {
        EventBus.Unsubscribe<PlayerDoubleJumpedEvent>(OnPlayerDoubleJumped);
        EventBus.Unsubscribe<PlayerShotFiredEvent>(OnPlayerShotFired);
        EventBus.Unsubscribe<CombatHitLandedEvent>(OnCombatHitLanded);
        EventBus.Unsubscribe<BulletCancelledEvent>(OnBulletCancelled);
        EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
        EventBus.Unsubscribe<BossDiedEvent>(OnBossDied);
        EventBus.Unsubscribe<BossDiveImpactEvent>(OnBossDiveImpact);
        EventBus.Unsubscribe<SuperArmorDeflectedEvent>(OnSuperArmorDeflected);
    }

    // --- イベントハンドラー ---

    private void OnPlayerDoubleJumped(PlayerDoubleJumpedEvent e)
    {
        if (_isDisposed || _world == default) return;

        var pCfg = _world.GetActivePlayerConfig();
        var vfxCfg = _world.Get<VisualEffectsRenderConfig>();
        CombatPoolService.RentRingEffect()
           .Set(new Transform(e.Position))
           .Set(new RingEffect(pCfg.Jump.JumpRingInitialRadius, pCfg.Jump.JumpRingMaxRadius, vfxCfg.JumpRingColor))
           .Set(new FadeEffect(pCfg.Jump.JumpRingDuration));
    }

    private void OnPlayerShotFired(PlayerShotFiredEvent e)
    {
        if (_isDisposed || _world == default) return;

        var vfxCfg = _world.Get<VisualEffectsRenderConfig>();
        CombatPoolService.RentSparkEffect()
           .Set(new Transform(e.SpawnPos))
           .Set(new Velocity(Vec2f.Zero))
           .Set(new VfxParticle(e.MuzzleFlashRadius, vfxCfg.MuzzleFlashFillColor, vfxCfg.MuzzleFlashOutlineColor))
           .Set(new FadeEffect(e.MuzzleFlashDuration));
    }

    private void OnCombatHitLanded(CombatHitLandedEvent e)
    {
        if (_isDisposed || _world == default) return;

        SpawnHitSpark(e.Position);

        if (e.HitDirection.LengthSquared() > 0.0001f)
        {
            SpawnBulletImpactSplatter(e.Position, e.HitDirection);
        }
    }

    private void OnBulletCancelled(BulletCancelledEvent e)
    {
        if (_isDisposed || _world == default) return;
        SpawnHitSpark(e.Position);
    }

    private void OnPlayerDied(PlayerDiedEvent e)
    {
        if (_isDisposed || _world == default) return;

        var vfxCfg = _world.Get<VisualEffectsRenderConfig>();
        for (var i = 0; i < vfxCfg.PlayerDeathParticleCount; i++)
        {
            var angle = i / (float)vfxCfg.PlayerDeathParticleCount * MathF.PI * 2.0f;
            var speed = vfxCfg.PlayerDeathParticleBaseSpeed + i % 3 * vfxCfg.PlayerDeathParticleSpeedStep;
            var vel = new Vec2f(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed);
            CombatPoolService.RentSparkEffect()
               .Set(new Transform(e.Position))
               .Set(new Velocity(vel))
               .Set(new VfxParticle(vfxCfg.PlayerDeathParticleRadius, vfxCfg.PlayerDeathParticleFillColor, vfxCfg.PlayerDeathParticleOutlineColor))
               .Set(new FadeEffect(vfxCfg.PlayerDeathParticleDuration));
        }
    }

    private void OnBossDied(BossDiedEvent e)
    {
        if (_isDisposed || _world == default) return;

        var vfxCfg = _world.Get<VisualEffectsRenderConfig>();
        for (var i = 0; i < vfxCfg.VictoryExplosionCount; i++)
        {
            var angle = i / (float)vfxCfg.VictoryExplosionCount * MathF.PI * 2.0f;
            var speed = vfxCfg.VictoryExplosionBaseSpeed + i % 5 * vfxCfg.VictoryExplosionSpeedStep;
            var vel = new Vec2f(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed);
            CombatPoolService.RentSparkEffect()
               .Set(new Transform(e.Position))
               .Set(new Velocity(vel))
               .Set(new VfxParticle(vfxCfg.VictoryExplosionRadius, vfxCfg.VictoryExplosionFillColor, vfxCfg.VictoryExplosionOutlineColor))
               .Set(new FadeEffect(vfxCfg.VictoryExplosionDuration));
        }
    }

    private void OnBossDiveImpact(BossDiveImpactEvent e)
    {
        if (_isDisposed || _world == default) return;

        var vfxCfg = _world.Get<VisualEffectsRenderConfig>();
        CombatPoolService.RentRingEffect()
           .Set(new Transform(e.Position))
           .Set(new RingEffect(vfxCfg.ShockwaveInitialRadius, vfxCfg.ShockwaveMaxRadius, vfxCfg.ShockwaveRingColor))
           .Set(new FadeEffect(vfxCfg.ShockwaveDuration));
    }

    private void OnSuperArmorDeflected(SuperArmorDeflectedEvent e)
    {
        if (_isDisposed || _world == default) return;

        // 1. 小型防御衝撃リング
        CombatPoolService.RentRingEffect()
           .Set(new Transform(e.Position))
           .Set(new RingEffect(8.0f, 32.0f, Color.FromArgb(255, 255, 140, 40)))
           .Set(new FadeEffect(0.18f));

        // 2. 偏向スパーク火花
        for (var i = 0; i < 4; i++)
        {
            var angle = Random.Shared.NextSingle() * MathF.PI * 2.0f;
            var speed = 100.0f + Random.Shared.NextSingle() * 100.0f;
            var vel = new Vec2f(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed);
            CombatPoolService.RentSparkEffect()
               .Set(new Transform(e.Position))
               .Set(new Velocity(vel))
               .Set(new VfxParticle(3.5f, Color.FromArgb(255, 255, 210, 80), Color.FromArgb(255, 255, 80, 30)))
               .Set(new FadeEffect(0.22f));
        }
    }

    // --- 内部ヘルパー ---

    private void SpawnHitSpark(Vec2f pos)
    {
        var vfxCfg = _world.Get<VisualEffectsRenderConfig>();
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

    private void SpawnBulletImpactSplatter(Vec2f pos, Vec2f hitDir)
    {
        var pCfg = _world.GetActivePlayerConfig();
        var weaponCfg = _world.GetWeaponConfig(pCfg.Weapon);
        if (weaponCfg is not RangedWeaponConfig rangedCfg) return;

        var vfxCfg = _world.Get<VisualEffectsRenderConfig>();
        var count = Random.Shared.Next(rangedCfg.ImpactSplatterMinCount, rangedCfg.ImpactSplatterMaxCount + 1);
        var baseAngle = MathF.Atan2(-hitDir.Y, -hitDir.X);

        for (var i = 0; i < count; i++)
        {
            var spreadAngle = baseAngle + (Random.Shared.NextSingle() - 0.5f) * MathF.PI * 0.8f;
            var speed = rangedCfg.ImpactSplatterMinSpeed + Random.Shared.NextSingle() * (rangedCfg.ImpactSplatterMaxSpeed - rangedCfg.ImpactSplatterMinSpeed);
            var vel = new Vec2f(MathF.Cos(spreadAngle) * speed, MathF.Sin(spreadAngle) * speed);

            CombatPoolService.RentSparkEffect()
               .Set(new Transform(pos))
               .Set(new Velocity(vel))
               .Set(new VfxParticle(rangedCfg.ImpactSplatterParticleRadius, vfxCfg.ImpactSplatterFillColor, vfxCfg.ImpactSplatterOutlineColor))
               .Set(new FadeEffect(rangedCfg.ImpactSplatterDuration));
        }
    }
}
