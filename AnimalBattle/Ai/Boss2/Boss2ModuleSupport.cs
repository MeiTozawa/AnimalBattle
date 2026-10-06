using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss2;

internal static class Boss2ModuleSupport {
    internal static bool IsBoss2(in StateContext context, in LifecycleInvocation invocation)
    {
        return context.BossType == BossType.Boss2 && invocation.BossType == BossType.Boss2;
    }

    internal static bool GetPattern(
        IBoss2Configuration configuration,
        BossSkill skill,
        in StateContext context,
        in LifecycleInvocation invocation,
        out Boss2AttackPattern pattern)
    {
        if (configuration.GetAttack(skill, out pattern) && pattern.IsValid) return true;

        pattern = null!;
        return false;
    }
}