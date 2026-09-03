using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Bindings;
using AnimalBattle.Core.Physics;

namespace AnimalBattle.Ai.Boss1;

/// <summary>Boss1 の攻撃後 Recovery を共通 Recovery lifecycle で処理する State_Module です。</summary>
public sealed class Boss1RecoveryModule : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBoss1Configuration _configuration;
    private readonly Boss1BehaviorState _state;
    private readonly Dictionary<BossSkill, RecoveryModule> _recoveries = new();
    private RecoveryModule? _activeRecovery;

    /// <inheritdoc />
    public BossState Id => Boss1StateIds.Recovery;
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss1Configuration", "MovementPort", "ObservationService"];

    /// <summary>Boss1 の攻撃 pattern と共有実行値を注入します。</summary>
    public Boss1RecoveryModule(IBoss1Configuration configuration, Boss1BehaviorState state)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        BossSkill[] boss1Attacks = [BossSkill.Aimed3Shot, BossSkill.RingBurst, BossSkill.DoubleSpiral, BossSkill.RainDrop, BossSkill.CrossChaos, BossSkill.FastSniper, BossSkill.DiveRam];
        foreach (BossSkill skill in boss1Attacks)
        {
            AttackDefinition definition = configuration.GetAttack(skill, out Boss1AttackPattern pattern)
                ? pattern.Definition
                : new AttackDefinition(skill, 0, 0, 0, 0);
            _recoveries[skill] = new RecoveryModule(Boss1StateIds.Recovery, definition);
        }
    }

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _activeRecovery = null;
        _state.StateElapsed = 0.0f;
        if (!Boss1ModuleSupport.IsBoss1(in context, in invocation)
            || !_state.CurrentSkill.IsValid()
            || !_recoveries.TryGetValue(_state.CurrentSkill, out _activeRecovery))
        {
            Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.Warning, ErrorCode.InvalidBossSkill, "Boss1 の Recovery 対象が不正です。", _state.CurrentSkill, AttackStage.Recovery);
            _state.RequestTransition(Boss1StateIds.Hover, new TransitionReason(ReasonKind.SafetyFallback, skill: _state.CurrentSkill, errorCode: ErrorCode.InvalidBossSkill));
            return;
        }


        _activeRecovery.Enter(ref context, in invocation);
        if (!_activeRecovery.IsActive && !_activeRecovery.CanEvaluateNextTransition)
        {
            _state.RequestTransition(Boss1StateIds.Hover, new TransitionReason(ReasonKind.SafetyFallback, skill: _state.CurrentSkill, errorCode: ErrorCode.InvalidConfiguration));
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

        // 冲撞（DiveRam）の易傷・後隙期間中のみ地上で完全停止し、プレイヤーが殴りやすい無防備状態を維持
        if (_state.CurrentSkill == BossSkill.DiveRam)
        {
            if (context.Movement.MoveTo(_state.CurrentPosition.X, _state.CurrentPosition.Y).IsError)
            {
                Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "DiveRam Recovery 中の静止 command を発行できませんでした。", _state.CurrentSkill, AttackStage.Recovery);
            }
        }
        else
        {
            Boss1ModuleSupport.ApplyHoverMovement(ref context, in invocation, _configuration, _state, _state.CurrentSkill, AttackStage.Recovery);
            _state.AttackStartPosition = _state.CurrentPosition;
        }

        if (_activeRecovery.CanEvaluateNextTransition)
        {
            _state.IsAttackActive = false;
            _state.RequestTransition(Boss1StateIds.Hover, new TransitionReason(ReasonKind.AttackCompleted, skill: _state.CurrentSkill));
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

