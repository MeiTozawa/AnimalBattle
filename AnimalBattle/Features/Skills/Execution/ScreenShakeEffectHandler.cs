namespace AnimalBattle.Skills;

/// <summary>
/// 技能実行時の画面振動を担当する標準ハンドラー。
/// </summary>
public sealed class ScreenShakeEffectHandler : ISkillEffectHandler
{
    /// <inheritdoc />
    public SkillEffectKind Kind => SkillEffectKind.ScreenShake;

    /// <inheritdoc />
    public void Execute(in SkillExecutionContext ctx, SkillEffectConfig effect, ref float angleOffset)
    {
        var camCfg = ctx.World.Get<CameraConfig>();
        var trauma = effect.ShakeIntensity > 1.0f
            ? effect.ShakeIntensity * camCfg.AbilityShakeTraumaScale
            : effect.ShakeIntensity;
        CameraService.AddTrauma(trauma);
    }
}
