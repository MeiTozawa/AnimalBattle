using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss1;

/// <summary>Boss1 の攻撃後 Recovery を共通 Recovery lifecycle で処理する State_Module です。</summary>
public sealed class Boss1RecoveryModule : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBoss1Configuration _configuration;
    private readonly Boss1BehaviorState _state;
    private readonly RecoveryModule[] _recoveries;
    private RecoveryModule? _activeRecovery;

    /// <inheritdoc />
    public StateId Id => Boss1StateIds.Recovery;
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss1Configuration", "MovementPort", "ObservationService"];

    /// <summary>Boss1 の攻撃 pattern と共有実行値を注入します。</summary>
    public Boss1RecoveryModule(IBoss1Configuration configuration, Boss1BehaviorState state)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _recoveries = new RecoveryModule[8];
        for (int value = 1; value < _recoveries.Length; value++)
        {
            AttackId attackId = new(value);
            AttackDefinition definition = configuration.TryGetAttack(attackId, out Boss1AttackPattern pattern)
                ? pattern.Definition
                : new AttackDefinition(attackId, 0, 0, 0, 0);
            _recoveries[value] = new RecoveryModule(Boss1StateIds.Recovery, definition);
        }
    }

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _activeRecovery = null;
        _state.StateElapsed = 0.0f;
        if (!Boss1ModuleSupport.IsBoss1(in context, in invocation)
            || !_state.CurrentAttackId.IsValid
            || _state.CurrentAttackId.Value >= _recoveries.Length)
        {
            Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.Warning, ErrorCode.InvalidAttackId, "Boss1 の Recovery 対象が不正です。", _state.CurrentAttackId, AttackStage.Recovery);
            _state.RequestTransition(Boss1StateIds.Hover, new TransitionReason(ReasonKind.SafetyFallback, attackId: _state.CurrentAttackId, errorCode: ErrorCode.InvalidAttackId));
            return;
        }

        _activeRecovery = _recoveries[_state.CurrentAttackId.Value];
        _activeRecovery.Enter(ref context, in invocation);
        if (!_activeRecovery.IsActive && !_activeRecovery.CanEvaluateNextTransition)
        {
            _state.RequestTransition(Boss1StateIds.Hover, new TransitionReason(ReasonKind.SafetyFallback, attackId: _state.CurrentAttackId, errorCode: ErrorCode.InvalidConfiguration));
        }
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_activeRecovery is null || !_activeRecovery.IsActive)
        {
            return;
        }

        _activeRecovery.Update(ref context, in invocation);
        _state.StateElapsed = _activeRecovery.Elapsed;
        if (_state.CurrentAttackId == Boss1AttackIds.DiveRam
            && _configuration.GetPhase(context.Phase) is Boss1PhaseSettings settings
            && _activeRecovery.Elapsed >= _activeRecovery.Definition.RecoveryDuration * settings.RecoverDelayRatio)
        {
            if (!context.Movement.MoveTo(_state.BaseX, settings.HoverBaseY))
            {
                Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "DiveRam の帰還 command を発行できませんでした。", _state.CurrentAttackId, AttackStage.Recovery);
            }
        }

        if (_activeRecovery.CanEvaluateNextTransition)
        {
            _state.IsAttackActive = false;
            _state.RequestTransition(Boss1StateIds.Hover, new TransitionReason(ReasonKind.AttackCompleted, attackId: _state.CurrentAttackId));
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _activeRecovery?.Exit(ref context, in invocation);
        _activeRecovery = null;
        _state.StateElapsed = 0.0f;
    }
}
