using AnimalBattle.Events;
using Flecs.NET.Core;

namespace AnimalBattle.Combat;

/// <summary>
///     設定駆動型の弾幕および波次生成を一元管理する静的サービスです。
/// </summary>
public static class BulletEmitter
{
    /// <summary>
    ///     Boss 1 の技能に応じた弾幕波次を生成します。
    /// </summary>
    /// <param name="world">対象のワールド。</param>
    /// <param name="skill">発動技能識別タグ。</param>
    /// <param name="origin">発射元座標。</param>
    /// <param name="target">目標座標。</param>
    /// <param name="angleOffset">回転角オフセット参照。</param>
    public static void EmitBoss1(
        World world,
        StringId skill,
        Vec2f origin,
        Vec2f target,
        ref float angleOffset)
    {
        Debug.Assert(world != default, "World インスタンスが有効である必要があります。");

        var patternCfg = ConfigManager.Boss1Patterns;
        if (patternCfg.TryGetSkill(skill, out var pattern))
        {
            pattern.EmitBullet(world, origin, target, ref angleOffset, 1.0f, 0.0f);
        }
    }

    /// <summary>
    ///     Boss 2 の技能に応じた弾幕および衝撃波を生成します。
    /// </summary>
    /// <param name="world">対象のワールド。</param>
    /// <param name="skill">発動技能識別タグ。</param>
    /// <param name="origin">発射元座標。</param>
    /// <param name="target">目標座標。</param>
    /// <param name="forwardDir">前方向き。</param>
    /// <param name="bodyHeight">衝突判定高さ。</param>
    public static void EmitBoss2(
        World world,
        StringId skill,
        Vec2f origin,
        Vec2f target,
        float forwardDir,
        float bodyHeight)
    {
        Debug.Assert(world != default, "World インスタンスが有効である必要があります。");

        var patternCfg = ConfigManager.Boss2Patterns;
        if (patternCfg.TryGetSkill(skill, out var pattern))
        {
            float dummyOffset = 0.0f;
            pattern.EmitBullet(world, origin, target, ref dummyOffset, forwardDir, bodyHeight);
        }
    }

    /// <summary>自機狙い扇状弾幕を生成します。</summary>
    internal static void EmitAimedSpread(World world, Aimed3ShotConfig cfg, Vec2f origin, Vec2f target)
    {
        var dx = target.X - origin.X;
        var dy = target.Y - origin.Y;
        var baseAngle = MathF.Atan2(dy, dx);

        var count = Math.Max(1, cfg.BulletCount);
        var halfSpread = cfg.SpreadAngle / 2.0f;
        var startAngle = baseAngle - halfSpread;
        var angleStep = count > 1 ? cfg.SpreadAngle / (count - 1) : 0.0f;
        var bType = BossPatternConfigContainerBase.ParseBulletType(cfg.BulletType, BulletType.Cyan);

        for (var i = 0; i < count; i++)
        {
            var angle = startAngle + i * angleStep;
            var vx = MathF.Cos(angle) * cfg.BulletSpeed;
            var vy = MathF.Sin(angle) * cfg.BulletSpeed;

            BulletSpawner.SpawnBossSpecialBullet(
                world,
                origin.X,
                origin.Y,
                vx,
                vy,
                cfg.BulletRadius,
                bType,
                cfg.FillColor,
                cfg.OutlineColor);
        }
    }

    /// <summary>高速狙撃弾幕を生成します。</summary>
    internal static void EmitSniper(World world, FastSniperConfig cfg, Vec2f origin, Vec2f target)
    {
        var dx = target.X - origin.X;
        var dy = target.Y - origin.Y;
        var angle = MathF.Atan2(dy, dx);
        var vx = MathF.Cos(angle) * cfg.BulletSpeed;
        var vy = MathF.Sin(angle) * cfg.BulletSpeed;
        var bType = BossPatternConfigContainerBase.ParseBulletType(cfg.BulletType, BulletType.Black);

        BulletSpawner.SpawnBossSpecialBullet(
            world,
            origin.X,
            origin.Y,
            vx,
            vy,
            cfg.BulletRadius,
            bType,
            cfg.FillColor,
            cfg.OutlineColor);
    }

    /// <summary>全方位円環弾幕を生成します。</summary>
    internal static void EmitRadialRing(World world, RingBurstConfig cfg, Vec2f origin, ref float angleOffset)
    {
        var count = Math.Max(1, cfg.BulletCount);
        var step = cfg.AngleStep > 0.0f ? cfg.AngleStep : MathF.PI * 2.0f / count;
        var bType = BossPatternConfigContainerBase.ParseBulletType(cfg.BulletType, BulletType.Magenta);

        for (var i = 0; i < count; i++)
        {
            var angle = angleOffset + i * step;
            var vx = MathF.Cos(angle) * cfg.BulletSpeed;
            var vy = MathF.Sin(angle) * cfg.BulletSpeed;

            BulletSpawner.SpawnBossSpecialBullet(
                world,
                origin.X,
                origin.Y,
                vx,
                vy,
                cfg.BulletRadius,
                bType,
                cfg.FillColor,
                cfg.OutlineColor);
        }

        angleOffset += 0.15f;
    }

    /// <summary>二重螺旋弾幕を生成します。</summary>
    internal static void EmitDoubleSpiral(World world, DoubleSpiralConfig cfg, Vec2f origin, ref float angleOffset)
    {
        var bType = BossPatternConfigContainerBase.ParseBulletType(cfg.BulletType, BulletType.Yellow);

        var vx1 = MathF.Cos(angleOffset) * cfg.BulletSpeed;
        var vy1 = MathF.Sin(angleOffset) * cfg.BulletSpeed;
        BulletSpawner.SpawnBossSpecialBullet(
            world,
            origin.X,
            origin.Y,
            vx1,
            vy1,
            cfg.BulletRadius,
            bType,
            cfg.FillColor1,
            cfg.OutlineColor1);

        var angle2 = angleOffset + MathF.PI;
        var vx2 = MathF.Cos(angle2) * cfg.BulletSpeed;
        var vy2 = MathF.Sin(angle2) * cfg.BulletSpeed;
        BulletSpawner.SpawnBossSpecialBullet(
            world,
            origin.X,
            origin.Y,
            vx2,
            vy2,
            cfg.BulletRadius,
            bType,
            cfg.FillColor2,
            cfg.OutlineColor2);

        angleOffset += cfg.AngleStep;
    }

    /// <summary>十字交差弾幕を生成します。</summary>
    internal static void EmitCrossChaos(World world, CrossChaosConfig cfg, Vec2f origin, ref float angleOffset)
    {
        var bType = BossPatternConfigContainerBase.ParseBulletType(cfg.BulletType, BulletType.Magenta);

        for (var k = 0; k < 4; k++)
        {
            var angle = angleOffset + k * (MathF.PI / 2.0f);
            var vx = MathF.Cos(angle) * cfg.BulletSpeed;
            var vy = MathF.Sin(angle) * cfg.BulletSpeed;

            BulletSpawner.SpawnBossSpecialBullet(
                world,
                origin.X,
                origin.Y,
                vx,
                vy,
                cfg.BulletRadius,
                bType,
                cfg.FillColor,
                cfg.OutlineColor);
        }

        angleOffset += cfg.AngleStep;
    }

    /// <summary>天水降雨弾幕を生成します。</summary>
    internal static void EmitRainDrop(World world, RainDropConfig cfg)
    {
        var rx = cfg.MinX + Random.Shared.NextSingle() * (cfg.MaxX - cfg.MinX);
        var bType = BossPatternConfigContainerBase.ParseBulletType(cfg.BulletType, BulletType.Cyan);

        BulletSpawner.SpawnBossSpecialBullet(
            world,
            rx,
            0.0f,
            0.0f,
            cfg.BulletSpeed,
            cfg.BulletRadius,
            bType,
            cfg.FillColor,
            cfg.OutlineColor);
    }

    /// <summary>急降下着地炸裂弾幕を生成します。</summary>
    internal static void EmitDiveImpact(World world, DiveRamConfig cfg, Vec2f origin)
    {
        var spawnY = origin.Y + cfg.ImpactOffsetY;
        EventBus.Publish(new BossDiveImpactEvent(origin.X, spawnY));
        var count = Math.Max(1, cfg.ImpactBulletCount);
        var step = MathF.PI * 2.0f / count;
        var bType = BossPatternConfigContainerBase.ParseBulletType(cfg.BulletType, BulletType.Magenta);

        for (var i = 0; i < count; i++)
        {
            var angle = i * step;
            var vx = MathF.Cos(angle) * cfg.ImpactBulletSpeed;
            var vy = MathF.Sin(angle) * cfg.ImpactBulletSpeed;

            BulletSpawner.SpawnBossSpecialBullet(
                world,
                origin.X,
                spawnY,
                vx,
                vy,
                cfg.ImpactBulletRadius,
                bType,
                cfg.ImpactFillColor,
                cfg.ImpactOutlineColor);
        }
    }

    /// <summary>滅地狂吼の天降落石を生成します。</summary>
    internal static void EmitEarthquakeRocks(World world, EarthquakeRoarConfig cfg)
    {
        var count = Math.Max(1, cfg.RockCount);
        var bType = BossPatternConfigContainerBase.ParseBulletType(cfg.BulletType, BulletType.Cyan);
        for (var i = 0; i < count; i++)
        {
            var rx = cfg.MinX + Random.Shared.NextSingle() * (cfg.MaxX - cfg.MinX);
            BulletSpawner.SpawnBossSpecialBullet(
                world,
                rx,
                0.0f,
                0.0f,
                cfg.RockSpeed,
                cfg.RockRadius,
                bType,
                cfg.RockFillColor,
                cfg.RockOutlineColor);
        }
    }

    /// <summary>撼地踏撃の双方向衝撃波を生成します。</summary>
    internal static void EmitGroundStompShockwaves(World world, GroundStompConfig cfg, Vec2f origin, float bodyHeight)
    {
        var groundY = origin.Y + bodyHeight / 2.0f - cfg.GroundOffsetY;
        EffectSpawner.SpawnShockwave(world, origin.X, groundY);

        BulletSpawner.SpawnGroundShockwave(
            world,
            origin.X - cfg.ShockwaveOffsetX,
            groundY,
            -cfg.ShockwaveSpeed,
            cfg.ShockwaveRadius,
            cfg.ShockwaveDamage,
            cfg.FillColor,
            cfg.OutlineColor);

        BulletSpawner.SpawnGroundShockwave(
            world,
            origin.X + cfg.ShockwaveOffsetX,
            groundY,
            cfg.ShockwaveSpeed,
            cfg.ShockwaveRadius,
            cfg.ShockwaveDamage,
            cfg.FillColor,
            cfg.OutlineColor);
    }

    /// <summary>泰山圧頂の着地破片を生成します。</summary>
    internal static void EmitLeapSmashShrapnel(World world, LeapSmashConfig cfg, Vec2f origin, float bodyHeight)
    {
        var groundY = origin.Y + bodyHeight / 2.0f;
        EffectSpawner.SpawnShockwave(world, origin.X, groundY);

        var count = Math.Max(2, cfg.ShrapnelCount);
        for (var k = 0; k < count; k++)
        {
            var progress = k / (float)(count - 1);
            var angle = -MathF.PI * (cfg.ShrapnelAngleStartRatio + cfg.ShrapnelAngleSpanRatio * progress);
            var vx = MathF.Cos(angle) * cfg.ShrapnelSpeed;
            var vy = MathF.Sin(angle) * cfg.ShrapnelSpeed;

            BulletSpawner.SpawnBossBullet(
                world,
                origin.X,
                groundY - cfg.ShrapnelOffsetY,
                vx,
                vy,
                cfg.ShrapnelRadius,
                cfg.ShrapnelFillColor,
                cfg.ShrapnelOutlineColor);
        }
    }
}
