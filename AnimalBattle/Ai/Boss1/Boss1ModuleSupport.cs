using System.Diagnostics;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss1;

internal static class Boss1ModuleSupport {
    internal static bool IsBoss1(in StateContext context, in LifecycleInvocation invocation)
    {
        return context.BossType == BossType.Boss1 && invocation.BossType == BossType.Boss1;
    }

    internal static void ApplyHoverMovement(
        ref StateContext context,
        in LifecycleInvocation invocation,
        IBoss1Configuration configuration,
        Boss1BehaviorState state,
        BossSkill skill = default,
        AttackStage stage = AttackStage.None)
    {
        if (context.Phase is not (Phase.Phase1 or Phase.Phase2 or Phase.Phase3)) return;

        var settings = configuration.GetPhase(context.Phase);
        var deltaTime = context.DeltaTime;
        state.HoverTimer += deltaTime;

        var curPos = state.CurrentPosition;
        Vec2f basePos = new(state.BaseX, settings.HoverBaseY);
        Vec2f playerPos = new(context.PlayerObservation.PositionX, context.PlayerObservation.PositionY);

        var newPos = PhysicsService.CalculateBossHoverPosition(
            curPos,
            basePos,
            playerPos,
            state.HoverTimer,
            settings.HoverSpeedX,
            settings.HoverSpeedY,
            settings.HoverAmpX,
            settings.HoverAmpY,
            settings.PlayerPursuitWeight,
            settings.HoverLerpSpeed,
            deltaTime);

        state.CurrentPosition = newPos;
        context.Movement.MoveTo(newPos.X, newPos.Y);
    }

    internal static float Ease(float progress, EaseType easeType)
    {
        return EasingMath.Evaluate(easeType, progress);
    }
}