using System;
using System.Collections.Generic;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss2;

/// <summary>
/// Boss2 の攻撃後隙（Recovery）状態モジュールです。
/// 攻撃後の隙・硬直と通常行動（Idle）への復帰を処理します。
/// </summary>
public sealed class Boss2RecoveryModule(IBoss2Configuration configuration, Boss2BehaviorState state)
    : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBoss2Configuration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly Boss2BehaviorState _state = state ?? throw new ArgumentNullException(nameof(state));
    private RecoveryModule? _activeRecovery;

    /// <inheritdoc />
    public BossState HandledState => Boss2State.Recovery;

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss2Configuration", "MovementPort", "ObservationService"];

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;

        BossSkill skill = _state.CurrentSkill;
        if (!skill.IsValid())
        {
            _activeRecovery?.Exit(ref context, in invocation);
            _state.RequestTransition(Boss2State.Idle, new TransitionReason(ReasonKind.SafetyFallback, skill: skill, errorCode: ErrorCode.InvalidBossSkill));
            return;
        }

        if (!_configuration.GetAttack(skill, out Boss2AttackPattern pattern))
        {
            _activeRecovery?.Exit(ref context, in invocation);
            _state.RequestTransition(Boss2State.Idle, new TransitionReason(ReasonKind.SafetyFallback, skill: skill, errorCode: ErrorCode.InvalidConfiguration));
            return;
        }

        if (_activeRecovery is null)
        {
            _activeRecovery = new(Boss2State.Recovery, pattern.Definition);
        }
        else
        {
            _activeRecovery.Reset(Boss2State.Recovery, pattern.Definition);
        }

        _activeRecovery.Enter(ref context, in invocation);

        if (!_activeRecovery.IsActive && !_activeRecovery.CanEvaluateNextTransition)
        {
            _state.RequestTransition(Boss2State.Idle, new TransitionReason(ReasonKind.SafetyFallback, skill: skill, errorCode: ErrorCode.InvalidConfiguration));
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

        // 泰山圧頂（LeapSmash）の易傷着地後隙中は移動を完全停止しプレイヤーの攻撃好機を維持
        if (_state.CurrentSkill == BossSkill.LeapSmash)
        {
            context.Movement.Stop();
        }

        if (_activeRecovery.CanEvaluateNextTransition)
        {
            _state.IsAttackActive = false;
            _state.RequestTransition(Boss2State.Idle, new TransitionReason(ReasonKind.AttackCompleted, skill: _state.CurrentSkill));
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _activeRecovery?.Exit(ref context, in invocation);
        _state.StateElapsed = 0.0f;
    }
}
