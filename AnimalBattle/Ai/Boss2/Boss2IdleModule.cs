using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss2;

/// <summary>Boss2 が足場上で停止し、次の行動を選択する待機 State_Module です。</summary>
/// <remarks>共有実行値を注入して待機 module を生成します。</remarks>
public sealed class Boss2IdleModule(Boss2BehaviorState state) : IBossStateModule, IStateModuleDependencyProvider {
    private readonly Boss2BehaviorState _state = state ?? throw new ArgumentNullException(nameof(state));

    /// <inheritdoc />
    public BossState HandledState => Boss2State.Idle;

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
        _state.IsAttackActive = false;
        if (!Boss2ModuleSupport.IsBoss2(in context, in invocation) || _state.IsDead) return;

        _state.IsInvulnerable = false;
        var deltaX = context.PlayerObservation.PositionX - _state.CurrentPosition.X;
        if (MathF.Abs(deltaX) > 6.0f) _state.FacingRight = deltaX > 0.0f;
        context.Movement.Stop();
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_state.IsDead) return;

        var deltaX = context.PlayerObservation.PositionX - _state.CurrentPosition.X;
        if (MathF.Abs(deltaX) > 6.0f) _state.FacingRight = deltaX > 0.0f;
        _state.StateElapsed += context.DeltaTime;
        context.Movement.Stop();
        if (_state.IsPlayerAlive && _state.IsGrounded && _state.StateElapsed >= 0.0f)
        {
            var playerInput = _state.AttackTargetEntityId > long.MaxValue
                ? long.MaxValue
                : (long)_state.AttackTargetEntityId;
            _state.RequestTransition(Boss2State.PatrolChase,
                new TransitionReason(ReasonKind.PlayerObservation, originalInput: playerInput));
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["MovementPort", "PlayerObservation"];
}