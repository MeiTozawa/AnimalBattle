namespace AnimalBattle.Skills;

/// <summary>
/// 各種幾何トポロジーに基づく弾幕生成を担当する標準ハンドラー。
/// </summary>
public sealed class BulletEffectHandler : ISkillEffectHandler
{
    /// <inheritdoc />
    public SkillEffectKind Kind => SkillEffectKind.Bullet;

    /// <inheritdoc />
    public void Execute(in SkillExecutionContext ctx, SkillEffectConfig effect, ref float angleOffset)
    {
        var cfg = effect.Bullet;
        if (cfg == null) return;

        var world = ctx.World;
        var origin = ctx.Origin;
        var target = ctx.TargetPos;

        switch (cfg.Pattern)
        {
            case BulletPatternKind.AimedSpread:
                EmitAimedSpread(world, cfg, origin, target);
                PublishBulletFired(ctx.SkillTag);
                break;

            case BulletPatternKind.RadialRing:
                EmitRadialRing(world, cfg, origin, target, ref angleOffset);
                PublishBulletFired(ctx.SkillTag);
                break;

            case BulletPatternKind.DoubleSpiral:
                EmitDoubleSpiral(world, cfg, origin, ref angleOffset);
                PublishBulletFired(ctx.SkillTag);
                break;

            case BulletPatternKind.CrossWave:
                EmitCrossWave(world, cfg, origin, ref angleOffset);
                PublishBulletFired(ctx.SkillTag);
                break;

            case BulletPatternKind.Skyfall:
                EmitSkyfall(world, cfg);
                PublishBulletFired(ctx.SkillTag);
                break;

            case BulletPatternKind.ImpactRing:
                EmitImpactRing(world, cfg, origin);
                PublishBulletFired(ctx.SkillTag);
                break;

            case BulletPatternKind.GroundShockwave:
                EmitGroundShockwaves(world, cfg, origin, ctx.BodyHeight);
                break;

            case BulletPatternKind.ShrapnelFan:
                EmitShrapnelFan(world, cfg, origin, ctx.BodyHeight);
                break;
        }
    }

    private static void PublishBulletFired(StringId skillId)
    {
        if (!skillId.IsEmpty)
        {
            EventBus.Publish(new BossBulletFiredEvent(skillId));
        }
    }

    private static BulletType ParseBulletType(string? str, BulletType defaultType = BulletType.Normal)
    {
        if (!string.IsNullOrWhiteSpace(str) && Enum.TryParse(str, true, out BulletType result))
        {
            return result;
        }
        return defaultType;
    }

    private static void EmitAimedSpread(World world, BulletEmitConfig cfg, Vec2f origin, Vec2f target)
    {
        var diff = target - origin;
        var baseAngle = MathF.Atan2(diff.Y, diff.X);

        var count = Math.Max(1, cfg.Count);
        var halfSpread = cfg.SpreadAngle / 2.0f;
        var startAngle = baseAngle - halfSpread;
        var angleStep = count > 1 ? cfg.SpreadAngle / (count - 1) : 0.0f;
        var bType = ParseBulletType(cfg.BulletType, BulletType.Cyan);

        for (var i = 0; i < count; i++)
        {
            var angle = startAngle + i * angleStep;
            var vel = new Vec2f(MathF.Cos(angle) * cfg.Speed, MathF.Sin(angle) * cfg.Speed);

            BulletSpawner.SpawnBossSpecialBullet(
                world,
                origin,
                vel,
                cfg.Radius,
                bType,
                cfg.FillColor,
                cfg.OutlineColor,
                cfg.IsIndestructible);
        }
    }

    private static void EmitRadialRing(World world, BulletEmitConfig cfg, Vec2f origin, Vec2f target, ref float angleOffset)
    {
        var diff = target != Vec2f.Zero ? target - origin : Vec2f.Zero;
        var baseAngle = diff.LengthSquared() > 0.001f ? MathF.Atan2(diff.Y, diff.X) : 0.0f;

        var count = Math.Max(1, cfg.Count);
        var step = cfg.AngleStep > 0.0f ? cfg.AngleStep : MathF.PI * 2.0f / count;
        var bType = ParseBulletType(cfg.BulletType, BulletType.Magenta);

        var startAngle = cfg.AngleStep > 0.0f
            ? baseAngle - (count - 1) * step * 0.5f + angleOffset
            : baseAngle + angleOffset;

        for (var i = 0; i < count; i++)
        {
            var angle = startAngle + i * step;
            var vel = new Vec2f(MathF.Cos(angle) * cfg.Speed, MathF.Sin(angle) * cfg.Speed);

            BulletSpawner.SpawnBossSpecialBullet(
                world,
                origin,
                vel,
                cfg.Radius,
                bType,
                cfg.FillColor,
                cfg.OutlineColor,
                cfg.IsIndestructible);
        }

        angleOffset += 0.15f;
    }

    private static void EmitDoubleSpiral(World world, BulletEmitConfig cfg, Vec2f origin, ref float angleOffset)
    {
        var bType = ParseBulletType(cfg.BulletType, BulletType.Yellow);

        var vel1 = new Vec2f(MathF.Cos(angleOffset) * cfg.Speed, MathF.Sin(angleOffset) * cfg.Speed);
        BulletSpawner.SpawnBossSpecialBullet(
            world,
            origin,
            vel1,
            cfg.Radius,
            bType,
            cfg.FillColor,
            cfg.OutlineColor,
            cfg.IsIndestructible);

        var angle2 = angleOffset + MathF.PI;
        var vel2 = new Vec2f(MathF.Cos(angle2) * cfg.Speed, MathF.Sin(angle2) * cfg.Speed);
        BulletSpawner.SpawnBossSpecialBullet(
            world,
            origin,
            vel2,
            cfg.Radius,
            bType,
            cfg.FillColor2,
            cfg.OutlineColor2,
            cfg.IsIndestructible);

        angleOffset += cfg.AngleStep;
    }

    private static void EmitCrossWave(World world, BulletEmitConfig cfg, Vec2f origin, ref float angleOffset)
    {
        var bType = ParseBulletType(cfg.BulletType, BulletType.Magenta);

        for (var k = 0; k < 4; k++)
        {
            var angle = angleOffset + k * (MathF.PI / 2.0f);
            var vel = new Vec2f(MathF.Cos(angle) * cfg.Speed, MathF.Sin(angle) * cfg.Speed);

            BulletSpawner.SpawnBossSpecialBullet(
                world,
                origin,
                vel,
                cfg.Radius,
                bType,
                cfg.FillColor,
                cfg.OutlineColor,
                cfg.IsIndestructible);
        }

        angleOffset += cfg.AngleStep;
    }

    private static void EmitSkyfall(World world, BulletEmitConfig cfg)
    {
        var count = Math.Max(1, cfg.Count);
        var bType = ParseBulletType(cfg.BulletType, BulletType.Cyan);

        for (var i = 0; i < count; i++)
        {
            var rx = cfg.MinX + Random.Shared.NextSingle() * (cfg.MaxX - cfg.MinX);
            BulletSpawner.SpawnBossSpecialBullet(
                world,
                new Vec2f(rx, 0.0f),
                new Vec2f(0.0f, cfg.Speed),
                cfg.Radius,
                bType,
                cfg.FillColor,
                cfg.OutlineColor,
                cfg.IsIndestructible);
        }
    }

    private static void EmitImpactRing(World world, BulletEmitConfig cfg, Vec2f origin)
    {
        var impactPos = new Vec2f(origin.X, origin.Y + cfg.OffsetY);
        EventBus.Publish(new BossDiveImpactEvent(impactPos));

        var count = Math.Max(1, cfg.Count);
        var step = MathF.PI * 2.0f / count;
        var bType = ParseBulletType(cfg.BulletType, BulletType.Magenta);

        for (var i = 0; i < count; i++)
        {
            var angle = i * step;
            var vel = new Vec2f(MathF.Cos(angle) * cfg.Speed, MathF.Sin(angle) * cfg.Speed);

            BulletSpawner.SpawnBossSpecialBullet(
                world,
                impactPos,
                vel,
                cfg.Radius,
                bType,
                cfg.FillColor,
                cfg.OutlineColor,
                cfg.IsIndestructible);
        }
    }

    private static void EmitGroundShockwaves(World world, BulletEmitConfig cfg, Vec2f origin, float bodyHeight)
    {
        var groundY = origin.Y + bodyHeight / 2.0f - cfg.GroundOffsetY;
        EffectSpawner.SpawnShockwave(world, new Vec2f(origin.X, groundY));

        BulletSpawner.SpawnGroundShockwave(
            world,
            new Vec2f(origin.X - cfg.OffsetX, groundY),
            -cfg.Speed,
            cfg.Radius,
            cfg.Damage,
            cfg.FillColor,
            cfg.OutlineColor,
            cfg.IsIndestructible);

        BulletSpawner.SpawnGroundShockwave(
            world,
            new Vec2f(origin.X + cfg.OffsetX, groundY),
            cfg.Speed,
            cfg.Radius,
            cfg.Damage,
            cfg.FillColor,
            cfg.OutlineColor,
            cfg.IsIndestructible);
    }

    private static void EmitShrapnelFan(World world, BulletEmitConfig cfg, Vec2f origin, float bodyHeight)
    {
        var groundY = origin.Y + bodyHeight / 2.0f;
        EffectSpawner.SpawnShockwave(world, new Vec2f(origin.X, groundY));

        var count = Math.Max(2, cfg.Count);
        for (var k = 0; k < count; k++)
        {
            var progress = k / (float)(count - 1);
            var angle = -MathF.PI * (cfg.AngleStartRatio + cfg.AngleSpanRatio * progress);
            var vel = new Vec2f(MathF.Cos(angle) * cfg.Speed, MathF.Sin(angle) * cfg.Speed);

            BulletSpawner.SpawnBossBullet(
                world,
                new Vec2f(origin.X, groundY - cfg.OffsetY),
                vel,
                cfg.Radius,
                cfg.FillColor,
                cfg.OutlineColor,
                cfg.IsIndestructible);
        }
    }
}
