using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss2;

/// <summary>FrenzyCharge の壁激突後に Boss2 を短時間停止する State_Module です。</summary>
public sealed class Boss2WallCrashStunModule : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly Boss2BehaviorState _state;

    /// <inheritdoc />
    public StateId Id => Boss2StateIds.WallCrashStun;
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["MovementPort", "ObservationService"];

    /// <summary>共有実行値を注入して WallCrashStun module を生成します。</summary>
    public Boss2WallCrashStunModule(Boss2BehaviorState state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
        _state.IsAttackActive = false;
        context.Movement.Stop();
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_state.IsDead)
        {
            return;
        }

        context.Movement.Stop();
        _state.StateElapsed += context.DeltaTime;
        if (_state.StateElapsed >= 1.2f)
        {
            _state.RequestTransition(Boss2StateIds.Recovery, new TransitionReason(ReasonKind.TimerElapsed, attackId: _state.CurrentAttackId));
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
    }
}
