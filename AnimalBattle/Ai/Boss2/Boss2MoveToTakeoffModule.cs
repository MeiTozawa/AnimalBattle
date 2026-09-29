using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss2;

/// <summary>
///     Boss2 が別足場への跳躍開始位置へ移動する State_Module です。
/// </summary>
/// <param name="configuration">Boss2 設定境界。</param>
/// <param name="state">Boss2 共有実行時状態。</param>
public sealed class Boss2MoveToTakeoffModule(IBoss2Configuration configuration, Boss2BehaviorState state)
    : IBossStateModule, IStateModuleDependencyProvider {
    private readonly IBoss2Configuration _configuration =
        configuration ?? throw new ArgumentNullException(nameof(configuration));

    private readonly Boss2BehaviorState _state = state ?? throw new ArgumentNullException(nameof(state));

    /// <inheritdoc />
    public BossState HandledState => Boss2State.MoveToTakeoff;

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_state.IsDead) return;

        var settings = _configuration.GetPhase(context.Phase);
        var deltaX = context.PlayerObservation.PositionX - _state.CurrentPosition.X;
        var direction = MathF.Abs(deltaX) > _configuration.CombatSettings.MoveThreshold ? MathF.Sign(deltaX) : 0.0f;
        var velocityX = direction * settings.MoveSpeed;
        if (MathF.Abs(deltaX) > _configuration.CombatSettings.MoveThreshold) _state.FacingRight = deltaX > 0.0f;
        _state.CurrentVelocity = new Vec2f(velocityX, _state.CurrentVelocity.Y);
        context.Movement.SetVelocity(velocityX, _state.CurrentVelocity.Y);
        _state.StateElapsed += context.DeltaTime;
        if (_state.IsGrounded && MathF.Abs(context.PlayerObservation.PositionX - _state.CurrentPosition.X) <=
            settings.TakeoffDistance)
            _state.RequestTransition(Boss2State.PlatformTraverse, new TransitionReason(ReasonKind.PlayerObservation));
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } =
        ["Boss2Configuration", "MovementPort", "PlayerObservation"];
}