using System;
using System.Collections.Generic;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Dsl.Modules;

/// <summary>
/// Boss1 の攻撃後隙（Recovery）を宣言的パイプライン定義に基づいて一元処理する DSL モジュールです。
/// </summary>
public sealed class DslBoss1RecoveryModule : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBoss1Configuration _configuration;
    private readonly Boss1BehaviorState _state;
    private RecoveryModule? _activeRecovery;

    /// <inheritdoc />
    public BossState Id => BossState.Boss1_Recovery;
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss1Configuration", "MovementPort", "ObservationService"];

    /// <summary>Boss1 設定および共有状態を注入して Recovery モジュールを生成します。</summary>
    public DslBoss1RecoveryModule(IBoss1Configuration configuration, Boss1BehaviorState state)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _activeRecovery = null;
        _state.StateElapsed = 0.0f;
        if (!DslBoss1Support.IsBoss1(in context, in invocation) || !_state.CurrentSkill.IsValid())
        {
            DslBoss1Support.Record(in context, in invocation, DiagnosticSeverity.Warning, ErrorCode.InvalidBossSkill, "Boss1 の Recovery 対象が不正です。", _state.CurrentSkill, AttackStage.Recovery);
            _state.RequestTransition(BossState.Boss1_Hover, new TransitionReason(ReasonKind.SafetyFallback, skill: _state.CurrentSkill, errorCode: ErrorCode.InvalidBossSkill));
            return;
        }

        if (!_configuration.GetAttack(_state.CurrentSkill, out Boss1AttackPattern pattern))
        {
            pattern = new Boss1AttackPattern(_state.CurrentSkill, 0, 0, 0);
        }

        _activeRecovery = new RecoveryModule(BossState.Boss1_Recovery, pattern.Definition);
        _activeRecovery.Enter(ref context, in invocation);

        if (!_activeRecovery.IsActive && !_activeRecovery.CanEvaluateNextTransition)
        {
            _state.RequestTransition(BossState.Boss1_Hover, new TransitionReason(ReasonKind.SafetyFallback, skill: _state.CurrentSkill, errorCode: ErrorCode.InvalidConfiguration));
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

        // 冲撞（DiveRam）の後隙期間中のみ地上で完全停止し、プレイヤーの反撃好機を維持
        if (_state.CurrentSkill == BossSkill.DiveRam)
        {
            if (context.Movement.MoveTo(_state.CurrentPosition.X, _state.CurrentPosition.Y).IsError)
            {
                DslBoss1Support.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "DiveRam Recovery 中の静止 command を発行できませんでした。", _state.CurrentSkill, AttackStage.Recovery);
            }
        }
        else
        {
            DslBoss1Support.ApplyHoverMovement(ref context, in invocation, _configuration, _state, _state.CurrentSkill, AttackStage.Recovery);
            _state.AttackStartPosition = _state.CurrentPosition;
        }

        if (_activeRecovery.CanEvaluateNextTransition)
        {
            _state.IsAttackActive = false;
            _state.RequestTransition(BossState.Boss1_Hover, new TransitionReason(ReasonKind.AttackCompleted, skill: _state.CurrentSkill));
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
