using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss2;

/// <summary>Boss2 が足場上で停止し、次の行動を選択する待機 State_Module です。</summary>
public sealed class Boss2IdleModule : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly Boss2BehaviorState _state;

    /// <inheritdoc />
    public BossState Id => Boss2StateIds.Idle;
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["MovementPort", "PlayerObservation"];

    /// <summary>共有実行値を注入して待機 module を生成します。</summary>
    public Boss2IdleModule(Boss2BehaviorState state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
        _state.IsAttackActive = false;
        if (!Boss2ModuleSupport.IsBoss2(in context, in invocation) || _state.IsDead)
        {
            return;
        }

        _state.IsInvulnerable = false;
        _state.FacingRight = context.PlayerObservation.PositionX >= _state.CurrentPosition.X;
        context.Movement.Stop();
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_state.IsDead)
        {
            return;
        }

        _state.FacingRight = context.PlayerObservation.PositionX >= _state.CurrentPosition.X;
        _state.StateElapsed += context.DeltaTime;
        context.Movement.Stop();
        if (_state.IsPlayerAlive && _state.IsGrounded && _state.StateElapsed >= 0.0f)
        {
            long playerInput = _state.AttackTargetEntityId > long.MaxValue
                ? long.MaxValue
                : (long)_state.AttackTargetEntityId;
            _state.RequestTransition(Boss2StateIds.PatrolChase, new TransitionReason(ReasonKind.PlayerObservation, originalInput: playerInput));
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
    }
}
