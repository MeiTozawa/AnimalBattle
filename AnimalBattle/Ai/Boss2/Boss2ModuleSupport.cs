using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss2;

internal static class Boss2ModuleSupport
{
    internal static bool IsBoss2(in StateContext context, in LifecycleInvocation invocation)
        => context.BossType == BossType.Boss2 && invocation.BossType == BossType.Boss2;

    internal static void Record(
        in StateContext context,
        in LifecycleInvocation invocation,
        DiagnosticSeverity severity,
        ErrorCode errorCode,
        string message,
        AttackId attackId = default,
        AttackStage stage = AttackStage.None)
    {
        ObservationEvent observation = new(
            context.EntityId,
            context.BossType,
            invocation.State,
            context.Phase,
            invocation.Operation,
            context.FrameUpdateNumber,
            context.DeltaTime,
            attackId,
            stage,
            new TransitionReason(ReasonKind.Configuration, attackId: attackId, errorCode: errorCode),
            severity,
            errorCode,
            message);
        try
        {
            context.Observation.Record(in observation);
        }
        catch
        {
            // 観測失敗だけで Boss2 の正常 lifecycle を停止しません。
        }
    }

    internal static bool GetPattern(
        IBoss2Configuration configuration,
        AttackId attackId,
        in StateContext context,
        in LifecycleInvocation invocation,
        out Boss2AttackPattern pattern)
    {
        if (configuration.GetAttack(attackId, out pattern) && pattern.IsValid)
        {
            return true;
        }

        Record(in context, in invocation, DiagnosticSeverity.Warning, ErrorCode.InvalidAttackId, "Boss2 の攻撃設定が不正です。", attackId);
        pattern = null!;
        return false;
    }

    internal static BossState SafeMovementState => Boss2StateIds.Idle;
    internal static BossState SafeAttackState => Boss2StateIds.Idle;
}

internal sealed class Boss2TelegraphRunner
{
    private readonly BossState _stateId;
    private readonly IBoss2Configuration _configuration;
    private readonly Boss2BehaviorState _state;
    private TelegraphModule? _active;

    internal Boss2TelegraphRunner(BossState stateId, IBoss2Configuration configuration, Boss2BehaviorState state)
    {
        _stateId = stateId;
        _configuration = configuration;
        _state = state;
    }

    internal bool IsComplete => _active is not null && _active.IsComplete;

    internal void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _active = null;
        if (!Boss2ModuleSupport.GetPattern(_configuration, _state.CurrentAttackId, in context, in invocation, out Boss2AttackPattern pattern))
        {
            _state.RequestTransition(Boss2StateIds.Idle, new TransitionReason(ReasonKind.SafetyFallback, attackId: _state.CurrentAttackId, errorCode: ErrorCode.InvalidAttackId));
            return;
        }

        _active = new TelegraphModule(_stateId, pattern.Definition);
        _state.AttackTargetPosition = new Core.Physics.Vec2f(context.PlayerObservation.PositionX, context.PlayerObservation.PositionY);
        _state.AttackTargetEntityId = context.PlayerObservation.EntityId;
        _active.Enter(ref context, in invocation);
        if (!_active.IsActive && !_active.IsComplete)
        {
            _state.RequestTransition(Boss2StateIds.Idle, new TransitionReason(ReasonKind.SafetyFallback, attackId: _state.CurrentAttackId, errorCode: ErrorCode.InvalidConfiguration));
        }
    }

    internal void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_active is null || !_active.IsActive)
        {
            return;
        }

        _active.Update(ref context, in invocation);
        if (IsComplete)
        {
            BossState executeState = Boss2StateIds.GetExecuteState(_state.CurrentAttackId);
            if (!executeState.IsValid())
            {
                _state.RequestTransition(Boss2StateIds.Idle, new TransitionReason(ReasonKind.SafetyFallback, attackId: _state.CurrentAttackId, errorCode: ErrorCode.InvalidAttackId));
                return;
            }

            _state.RequestTransition(executeState, new TransitionReason(ReasonKind.TimerElapsed, attackId: _state.CurrentAttackId));
        }
    }

    internal void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _active?.Exit(ref context, in invocation);
        _active = null;
    }
}

internal sealed class Boss2RecoveryRunner
{
    private readonly IBoss2Configuration _configuration;
    private readonly Boss2BehaviorState _state;
    private RecoveryModule? _active;

    internal Boss2RecoveryRunner(IBoss2Configuration configuration, Boss2BehaviorState state)
    {
        _configuration = configuration;
        _state = state;
    }

    internal void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _active = null;
        if (!Boss2ModuleSupport.GetPattern(_configuration, _state.CurrentAttackId, in context, in invocation, out Boss2AttackPattern pattern))
        {
            _state.RequestTransition(Boss2StateIds.Idle, new TransitionReason(ReasonKind.SafetyFallback, attackId: _state.CurrentAttackId, errorCode: ErrorCode.InvalidAttackId));
            return;
        }

        _active = new RecoveryModule(Boss2StateIds.Recovery, pattern.Definition);
        _active.Enter(ref context, in invocation);
        if (!_active.IsActive && !_active.CanEvaluateNextTransition)
        {
            _state.RequestTransition(Boss2StateIds.Idle, new TransitionReason(ReasonKind.SafetyFallback, attackId: _state.CurrentAttackId, errorCode: ErrorCode.InvalidConfiguration));
        }
    }

    internal void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_active is null || !_active.IsActive)
        {
            return;
        }

        _active.Update(ref context, in invocation);
        _state.StateElapsed = _active.Elapsed;
        if (_active.CanEvaluateNextTransition)
        {
            _state.IsAttackActive = false;
            _state.RequestTransition(Boss2StateIds.Idle, new TransitionReason(ReasonKind.AttackCompleted, attackId: _state.CurrentAttackId));
        }
    }

    internal void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _active?.Exit(ref context, in invocation);
        _active = null;
        _state.StateElapsed = 0.0f;
    }
}
