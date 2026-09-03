using System;
using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Core.Physics;

namespace AnimalBattle.Ai.Dsl.Modules;

/// <summary>
/// Boss1 の DSL 状態モジュール間で共有される補助関数および軌道計算を提供する内部ヘルパークラスです。
/// </summary>
internal static class DslBoss1Support
{
    internal static bool IsBoss1(in StateContext context, in LifecycleInvocation invocation)
        => context.BossType == BossType.Boss1 && invocation.BossType == BossType.Boss1;

    internal static void ApplyHoverMovement(
        ref StateContext context,
        in LifecycleInvocation invocation,
        IBoss1Configuration configuration,
        Boss1BehaviorState state,
        BossSkill skill = default,
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

        float targetX = basePos.X + (MathF.Sin(state.HoverTimer * settings.HoverSpeedX) * settings.HoverAmpX)
                        + ((playerPos.X - basePos.X) * settings.PlayerPursuitWeight);
        float targetY = basePos.Y + (MathF.Cos(state.HoverTimer * settings.HoverSpeedY) * settings.HoverAmpY);
        float lerpFactor = MathF.Min(1.0f, settings.HoverLerpSpeed * deltaTime);

        float nextX = curPos.X + ((targetX - curPos.X) * lerpFactor);
        float nextY = curPos.Y + ((targetY - curPos.Y) * lerpFactor);
        Vec2f newPos = new(nextX, nextY);

        state.CurrentPosition = newPos;
        if (context.Movement.MoveTo(newPos.X, newPos.Y).IsError)
        {
            Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "Boss1 の浮遊移動 command を発行できませんでした。", skill, stage);
        }
    }

    internal static void Record(
        in StateContext context,
        in LifecycleInvocation invocation,
        DiagnosticSeverity severity,
        ErrorCode errorCode,
        string message,
        BossSkill skill = default,
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
            skill,
            stage,
            new TransitionReason(ReasonKind.Configuration, skill: skill, errorCode: errorCode),
            severity,
            errorCode,
            message);
        try
        {
            context.Observation.Record(in observation);
        }
        catch
        {
            // 観測失敗だけで Boss1 の正常 lifecycle を停止しません。
        }
    }
}
