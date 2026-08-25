using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss1;

internal static class Boss1ModuleSupport
{
    internal static bool IsBoss1(in StateContext context, in LifecycleInvocation invocation)
        => context.BossType == BossType.Boss1 && invocation.BossType == BossType.Boss1;

    internal static void Record(
        in StateContext context,
        in LifecycleInvocation invocation,
        DiagnosticSeverity severity,
        ErrorCode errorCode,
        string message,
        AttackId attackId = default,
        AttackStage stage = AttackStage.None)
    {
        ObservationEvent observation = new(
            context.EntityId,
            context.BossType,
            invocation.State,
            context.Phase,
            invocation.Operation,
            context.FrameUpdateNumber,
            context.DeltaTime,
            attackId,
            stage,
            new TransitionReason(ReasonKind.Configuration, attackId: attackId, errorCode: errorCode),
            severity,
            errorCode,
            message);
        try
        {
            context.Observation.TryRecord(in observation);
        }
        catch
        {
            // 診断失敗だけで Boss1 の正常 lifecycle を停止しません。
        }
    }

    internal static float Clamp01(float value) => MathF.Max(0.0f, MathF.Min(1.0f, value));

    internal static float Ease(float progress, Core.EaseType easeType)
    {
        float t = Clamp01(progress);
        return easeType switch
        {
            Core.EaseType.InQuad => t * t,
            Core.EaseType.OutQuad => t * (2.0f - t),
            Core.EaseType.InOutQuad => t < 0.5f ? 2.0f * t * t : -1.0f + (4.0f - 2.0f * t) * t,
            _ => t
        };
    }
}
