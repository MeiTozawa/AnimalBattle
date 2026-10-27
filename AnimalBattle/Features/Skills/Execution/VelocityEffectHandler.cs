namespace AnimalBattle.Skills;

/// <summary>
/// 施術者への瞬間移動速度適用を担当する標準ハンドラー。
/// </summary>
public sealed class VelocityEffectHandler : ISkillEffectHandler
{
    /// <inheritdoc />
    public SkillEffectKind Kind => SkillEffectKind.Velocity;

    /// <inheritdoc />
    public void Execute(in SkillExecutionContext ctx, SkillEffectConfig effect, ref float angleOffset)
    {
        ctx.SetVelocity?.Invoke(new Vec2f(effect.Velocity.X * ctx.ForwardDir, effect.Velocity.Y));
    }
}
