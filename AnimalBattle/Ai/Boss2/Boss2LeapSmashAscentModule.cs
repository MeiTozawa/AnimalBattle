using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss2;

/// <summary>LeapSmash の上昇 Execute State_Module です。</summary>
public sealed class Boss2LeapSmashAscentModule : Boss2ExecuteModuleBase
{
    /// <summary>Boss2 設定と共有実行値を注入します。</summary>
    public Boss2LeapSmashAscentModule(IBoss2Configuration configuration, Boss2BehaviorState state)
        : base(Boss2StateIds.LeapSmashAscent, configuration, state)
    {
    }

    /// <inheritdoc />
    public override void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        base.Update(ref context, in invocation);
        if (!State.IsDead && State.CurrentVelocity.Y >= 0.0f && Gate is not null && Gate.IsActive)
        {
            State.RequestTransition(Boss2StateIds.LeapSmashDescent, new TransitionReason(ReasonKind.TimerElapsed, attackId: State.CurrentAttackId));
        }
    }
}
