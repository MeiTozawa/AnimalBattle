using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Core.Physics;

namespace AnimalBattle.Ai.Boss2;

/// <summary>Boss2 の足場間跳躍の上昇を処理する State_Module です。</summary>
public sealed class Boss2PlatformJumpModule : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBoss2Configuration _configuration;
    private readonly Boss2BehaviorState _state;

    /// <inheritdoc />
    public BossState Id => Boss2StateIds.PlatformJump;
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss2Configuration", "MovementPort"];

    /// <summary>Boss2 設定と共有実行値を注入します。</summary>
    public Boss2PlatformJumpModule(IBoss2Configuration configuration, Boss2BehaviorState state)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        Boss2PhaseSettings settings = _configuration.GetPhase(context.Phase);
        _state.IsGrounded = false;
        _state.StateElapsed = 0.0f;
        _state.CurrentVelocity = new Vec2f(_state.CurrentVelocity.X, -settings.JumpForce);
        context.Movement.SetVelocity(_state.CurrentVelocity.X, -settings.JumpForce);
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_state.IsDead)
        {
            return;
        }

        Boss2PhaseSettings settings = _configuration.GetPhase(context.Phase);
        context.Movement.ApplyGravity(settings.Gravity, settings.MaxFallSpeed);
        _state.StateElapsed += context.DeltaTime;
        if (_state.CurrentVelocity.Y >= 0.0f)
        {
            _state.RequestTransition(Boss2StateIds.PlatformFall, new TransitionReason(ReasonKind.TimerElapsed));
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
    }
}
