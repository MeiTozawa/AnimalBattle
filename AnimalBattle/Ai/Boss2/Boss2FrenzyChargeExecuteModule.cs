using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss2;

/// <summary>FrenzyCharge の Execute と壁激突への遷移を処理する State_Module です。</summary>
public sealed class Boss2FrenzyChargeExecuteModule : Boss2ExecuteModuleBase
{
    /// <summary>Boss2 設定と共有実行値を注入します。</summary>
    public Boss2FrenzyChargeExecuteModule(IBoss2Configuration configuration, Boss2BehaviorState state)
        : base(Boss2StateIds.FrenzyChargeExecute, configuration, state)
    {
    }

    /// <inheritdoc />
    public override void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        base.Update(ref context, in invocation);
        if (Gate is not null && Gate.IsComplete && !State.IsDead)
        {
            State.RequestTransition(Boss2StateIds.WallCrashStun, new TransitionReason(ReasonKind.ExternalRequest, attackId: State.CurrentAttackId));
        }
    }
}
