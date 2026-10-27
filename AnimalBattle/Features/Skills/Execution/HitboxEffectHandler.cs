namespace AnimalBattle.Skills;

/// <summary>
/// 近接攻撃判定の生成を担当する標準ハンドラー。
/// </summary>
public sealed class HitboxEffectHandler : ISkillEffectHandler
{
    /// <inheritdoc />
    public SkillEffectKind Kind => SkillEffectKind.Hitbox;

    /// <inheritdoc />
    public void Execute(in SkillExecutionContext ctx, SkillEffectConfig effect, ref float angleOffset)
    {
        var cfg = effect.Hitbox;
        if (cfg == null) return;

        var world = ctx.World;
        var origin = ctx.Origin;

        if (cfg.DirectionMode == HitboxDirectionMode.DownOnGround)
        {
            var groundY = origin.Y + ctx.BodyHeight / 2.0f;
            MeleeSpawner.SpawnMeleeSlashHitbox(
                world,
                new Vec2f(origin.X, groundY - cfg.Size.Y * 0.5f),
                Vec2f.Zero,
                cfg.Size,
                cfg.Damage,
                cfg.Duration,
                cfg.Color,
                SlashDirection.Down);
        }
        else
        {
            var offX = cfg.Offset.X;
            if (cfg.UseBossSlashOffset)
            {
                var b2Cfg = world.Get<Boss2Config>();
                offX = ctx.ForwardDir * (b2Cfg.SlashSpawnOffset.X + MathF.Abs(cfg.Offset.X));
            }
            var offY = cfg.Offset.Y;
            var dir = ctx.ForwardDir > 0.0f ? SlashDirection.Right : SlashDirection.Left;

            MeleeSpawner.SpawnMeleeSlashHitbox(
                world,
                origin + new Vec2f(offX, offY),
                new Vec2f(ctx.CasterVelocity.X, 0.0f),
                cfg.Size,
                cfg.Damage,
                cfg.Duration,
                cfg.Color,
                dir);
        }
    }
}
