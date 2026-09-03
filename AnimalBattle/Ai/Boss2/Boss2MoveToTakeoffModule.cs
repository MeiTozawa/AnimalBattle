using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Core.Physics;

namespace AnimalBattle.Ai.Boss2;

/// <summary>Boss2 が別足場への跳躍開始位置へ移動する State_Module です。</summary>
public sealed class Boss2MoveToTakeoffModule : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBoss2Configuration _configuration;
    private readonly Boss2BehaviorState _state;

    /// <inheritdoc />
    public BossState Id => BossState.Boss2_MoveToTakeoff;
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss2Configuration", "MovementPort", "PlayerObservation"];

    /// <summary>Boss2 設定と共有実行値を注入します。</summary>
    public Boss2MoveToTakeoffModule(IBoss2Configuration configuration, Boss2BehaviorState state)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_state.IsDead)
        {
            return;
        }

        Boss2PhaseSettings settings = _configuration.GetPhase(context.Phase);
        float deltaX = context.PlayerObservation.PositionX - _state.CurrentPosition.X;
        float direction = MathF.Abs(deltaX) > 6.0f ? MathF.Sign(deltaX) : 0.0f;
        float velocityX = direction * settings.MoveSpeed;
        if (MathF.Abs(deltaX) > 6.0f)
        {
            _state.FacingRight = deltaX > 0.0f;
        }
        _state.CurrentVelocity = new Vec2f(velocityX, _state.CurrentVelocity.Y);
        context.Movement.SetVelocity(velocityX, _state.CurrentVelocity.Y);
        _state.StateElapsed += context.DeltaTime;
        if (_state.IsGrounded && MathF.Abs(context.PlayerObservation.PositionX - _state.CurrentPosition.X) <= settings.TakeoffDistance)
        {
            _state.RequestTransition(BossState.Boss2_PlatformTraverse, new TransitionReason(ReasonKind.PlayerObservation));
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
    }
}
