using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss2;

/// <summary>LeapSmash の落下 Execute State_Module です。</summary>
public sealed class Boss2LeapSmashDescentModule : Boss2ExecuteModuleBase
{
    /// <summary>Boss2 設定と共有実行値を注入します。</summary>
    public Boss2LeapSmashDescentModule(IBoss2Configuration configuration, Boss2BehaviorState state)
        : base(Boss2StateIds.LeapSmashDescent, configuration, state)
    {
    }

    /// <inheritdoc />
    public override void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        base.Update(ref context, in invocation);
        if (!State.IsDead)
        {
            Boss2PhaseSettings settings = Configuration.GetPhase(context.Phase);
            context.Movement.ApplyGravity(settings.Gravity, settings.MaxFallSpeed);
            if (State.IsGrounded && State.StateElapsed > 0.05f)
            {
                State.IsAttackActive = false;
                State.RequestTransition(Boss2StateIds.Recovery, new TransitionReason(ReasonKind.AttackCompleted, attackId: State.CurrentAttackId));
            }
        }
    }
}
