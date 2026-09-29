using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss2;

internal static class Boss2ModuleSupport {
    internal static BossState SafeMovementState => Boss2State.Idle;
    internal static BossState SafeAttackState => Boss2State.Idle;

    internal static bool IsBoss2(in StateContext context, in LifecycleInvocation invocation)
    {
        return context.BossType == BossType.Boss2 && invocation.BossType == BossType.Boss2;
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
            // 観測失敗だけで Boss2 の正常 lifecycle を停止しません。
        }
    }

    internal static bool GetPattern(
        IBoss2Configuration configuration,
        BossSkill skill,
        in StateContext context,
        in LifecycleInvocation invocation,
        out Boss2AttackPattern pattern)
    {
        if (configuration.GetAttack(skill, out pattern) && pattern.IsValid) return true;

        Record(in context, in invocation, DiagnosticSeverity.Warning, ErrorCode.InvalidBossSkill, "Boss2 の攻撃設定が不正です。",
            skill);
        pattern = null!;
        return false;
    }
}