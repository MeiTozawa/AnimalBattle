using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss1;

/// <summary>
///     Boss1 の攻撃後隙（Recovery）状態モジュールです。
///     攻撃後の隙・硬直とホバリング状態への復帰を処理します。
/// </summary>
public sealed class Boss1RecoveryModule(IBoss1Configuration configuration, Boss1BehaviorState state)
    : IBossStateModule, IStateModuleDependencyProvider {
    private readonly IBoss1Configuration _configuration =
        configuration ?? throw new ArgumentNullException(nameof(configuration));

    private readonly Boss1BehaviorState _state = state ?? throw new ArgumentNullException(nameof(state));
    private RecoveryModule? _activeRecovery;

    /// <inheritdoc />
    public BossState HandledState => Boss1State.Recovery;

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;

        var skill = _state.CurrentSkill;
        if (!skill.IsValid())
        {
            _activeRecovery?.Exit(ref context, in invocation);
            _state.RequestTransition(Boss1State.Hover,
                new TransitionReason(ReasonKind.SafetyFallback, skill: skill, errorCode: ErrorCode.InvalidBossSkill));
            return;
        }

        if (!_configuration.GetAttack(skill, out var pattern))
        {
            _activeRecovery?.Exit(ref context, in invocation);
            _state.RequestTransition(Boss1State.Hover,
                new TransitionReason(ReasonKind.SafetyFallback, skill: skill,
                    errorCode: ErrorCode.InvalidConfiguration));
            return;
        }

        if (_activeRecovery is null)
            _activeRecovery = new RecoveryModule(Boss1State.Recovery, pattern.Definition);
        else
            _activeRecovery.Reset(Boss1State.Recovery, pattern.Definition);

        _activeRecovery.Enter(ref context, in invocation);

        if (!_activeRecovery.IsActive && !_activeRecovery.CanEvaluateNextTransition)
            _state.RequestTransition(Boss1State.Hover,
                new TransitionReason(ReasonKind.SafetyFallback, skill: skill,
                    errorCode: ErrorCode.InvalidConfiguration));
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_activeRecovery is null || !_activeRecovery.IsActive) return;

        _activeRecovery.Update(ref context, in invocation);
        _state.StateElapsed = _activeRecovery.Elapsed;

        // 冲撞（DiveRam）の易傷・後隙期間中のみ地上で静止し、他技能の後隙では通常浮遊移動を維持
        if (_state.CurrentSkill == BossSkill.DiveRam)
        {
            context.Movement.MoveTo(_state.CurrentPosition.X, _state.CurrentPosition.Y);
        }
        else
        {
            Boss1ModuleSupport.ApplyHoverMovement(ref context, in invocation, _configuration, _state,
                _state.CurrentSkill, AttackStage.Recovery);
            _state.AttackStartPosition = _state.CurrentPosition;
        }

        if (_activeRecovery.CanEvaluateNextTransition)
        {
            _state.IsAttackActive = false;
            _state.RequestTransition(Boss1State.Hover,
                new TransitionReason(ReasonKind.AttackCompleted, skill: _state.CurrentSkill));
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _activeRecovery?.Exit(ref context, in invocation);
        _state.StateElapsed = 0.0f;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } =
        ["Boss1Configuration", "MovementPort", "ObservationService"];
}