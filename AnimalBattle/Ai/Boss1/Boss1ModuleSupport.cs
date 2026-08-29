using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Bindings;
using AnimalBattle.Core.Physics;

namespace AnimalBattle.Ai.Boss1;

internal static class Boss1ModuleSupport
{
    internal static bool IsBoss1(in StateContext context, in LifecycleInvocation invocation)
        => context.BossType == BossType.Boss1 && invocation.BossType == BossType.Boss1;

    internal static void ApplyHoverMovement(
        ref StateContext context,
        in LifecycleInvocation invocation,
        IBoss1Configuration configuration,
        Boss1BehaviorState state,
        AttackId attackId = default,
        AttackStage stage = AttackStage.None)
    {
        if (context.Phase is not (Phase.Phase1 or Phase.Phase2 or Phase.Phase3))
        {
            return;
        }

        Boss1PhaseSettings settings = configuration.GetPhase(context.Phase);
        float deltaTime = context.DeltaTime;
        state.HoverTimer += deltaTime;

        Vec2f curPos = state.CurrentPosition;
        Vec2f basePos = new(state.BaseX, settings.HoverBaseY);
        Vec2f playerPos = new(context.PlayerObservation.PositionX, context.PlayerObservation.PositionY);

        NativeExtensionBindings.Extension_CalculateBossHoverPosition(
            in curPos,
            in basePos,
            in playerPos,
            state.HoverTimer,
            settings.HoverSpeedX,
            settings.HoverSpeedY,
            settings.HoverAmpX,
            settings.HoverAmpY,
            settings.PlayerPursuitWeight,
            settings.HoverLerpSpeed,
            deltaTime,
            out Vec2f newPos);

        state.CurrentPosition = newPos;
        if (context.Movement.MoveTo(newPos.X, newPos.Y).IsError)
        {
            Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "Boss1 の浮遊移動 command を発行できませんでした。", attackId, stage);
        }
    }

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
            context.Observation.Record(in observation);
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
