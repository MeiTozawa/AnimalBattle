using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Core.Physics;

namespace AnimalBattle.Ai.Boss2;

/// <summary>Boss2 が現在の足場でプレイヤーへ追跡する State_Module です。</summary>
public sealed class Boss2PatrolChaseModule : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBoss2Configuration _configuration;
    private readonly Boss2BehaviorState _state;

    /// <inheritdoc />
    public BossState Id => Boss2StateIds.PatrolChase;
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss2Configuration", "MovementPort", "PlayerObservation"];

    /// <summary>Boss2 設定と共有実行値を注入します。</summary>
    public Boss2PatrolChaseModule(IBoss2Configuration configuration, Boss2BehaviorState state)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
        _state.IsInvulnerable = false;
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_state.IsDead || !_state.IsGrounded)
        {
            if (!_state.IsGrounded)
            {
                _state.RequestTransition(Boss2StateIds.PlatformFall, new TransitionReason(ReasonKind.PlayerObservation));
            }
            return;
        }

        Boss2PhaseSettings settings = _configuration.GetPhase(context.Phase);
        float deltaX = context.PlayerObservation.PositionX - _state.CurrentPosition.X;
        _state.FacingRight = deltaX >= 0.0f;
        float velocityX = MathF.Sign(deltaX) * settings.MoveSpeed;
        _state.CurrentVelocity = new Vec2f(velocityX, _state.CurrentVelocity.Y);
        context.Movement.SetVelocity(velocityX, _state.CurrentVelocity.Y);
        _state.StateElapsed += context.DeltaTime;
        if (MathF.Abs(deltaX) <= 175.0f && MathF.Abs(context.PlayerObservation.PositionY - _state.CurrentPosition.Y) <= 75.0f)
        {
            SelectNextAttack(ref context, in invocation);
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
    }

    private void SelectNextAttack(ref StateContext context, in LifecycleInvocation invocation)
    {
        Boss2PhaseSettings settings = _configuration.GetPhase(context.Phase);
        if (settings.AttackOrder.Count == 0)
        {
            Boss2ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.Warning, ErrorCode.InvalidConfiguration, "Boss2 の攻撃順が空です。");
            return;
        }

        int index = _state.CurrentAttackIndex % settings.AttackOrder.Count;
        _state.CurrentAttackIndex++;
        AttackId attackId = settings.AttackOrder[index];
        if (!Boss2ModuleSupport.GetPattern(_configuration, attackId, in context, in invocation, out Boss2AttackPattern pattern))
        {
            _state.RequestTransition(Boss2StateIds.Idle, new TransitionReason(ReasonKind.SafetyFallback, attackId: attackId, errorCode: ErrorCode.InvalidAttackId));
            return;
        }

        _state.CurrentAttackId = pattern.AttackId;
        _state.AttackStartPosition = _state.CurrentPosition;
        _state.AttackTargetPosition = new Vec2f(context.PlayerObservation.PositionX, context.PlayerObservation.PositionY);
        _state.AttackTargetEntityId = context.PlayerObservation.EntityId;
        _state.IsAttackActive = true;
        _state.RequestTransition(Boss2StateIds.GetTelegraphState(attackId), new TransitionReason(ReasonKind.PlayerObservation, attackId: attackId));
    }
}
