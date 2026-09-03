using System;
using System.Collections.Generic;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using ErrorOr;

namespace AnimalBattle.Ai.Dsl.Modules;

/// <summary>
/// 宣言的パイプラインで使用される汎用の攻撃後隙（Recovery）状態モジュールです。
/// Boss1 および Boss2 で共通利用され、攻撃後の隙・硬直とデフォルト状態への復帰を処理します。
/// </summary>
/// <remarks>
/// 汎用後隙モジュールを初期化します。
/// </remarks>
/// <param name="adapter">Boss 攻撃アダプター。</param>
public sealed class DslRecoveryModule(IBossAttackAdapter adapter) : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBossAttackAdapter _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
    private RecoveryModule? _activeRecovery;

    /// <inheritdoc />
    public BossState HandledState => _adapter.RecoveryState;

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["MovementPort", "ObservationService"];

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _activeRecovery = null;
        _adapter.OnRecoveryEnter(ref context, in invocation);

        BossSkill skill = _adapter.CurrentSkill;
        if (!skill.IsValid())
        {
            _adapter.RequestTransition(_adapter.DefaultState, new TransitionReason(ReasonKind.SafetyFallback, skill: skill, errorCode: ErrorCode.InvalidBossSkill));
            return;
        }

        ErrorOr<AttackDefinition> definitionResult = _adapter.GetAttackDefinition(skill);
        if (definitionResult.IsError)
        {
            _adapter.RequestTransition(_adapter.DefaultState, new TransitionReason(ReasonKind.SafetyFallback, skill: skill, errorCode: ErrorCode.InvalidConfiguration));
            return;
        }

        _activeRecovery = new RecoveryModule(_adapter.RecoveryState, definitionResult.Value);
        _activeRecovery.Enter(ref context, in invocation);

        if (!_activeRecovery.IsActive && !_activeRecovery.CanEvaluateNextTransition)
        {
            _adapter.RequestTransition(_adapter.DefaultState, new TransitionReason(ReasonKind.SafetyFallback, skill: skill, errorCode: ErrorCode.InvalidConfiguration));
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
        _adapter.OnRecoveryUpdate(ref context, in invocation, _activeRecovery.Elapsed);

        if (_activeRecovery.CanEvaluateNextTransition)
        {
            _adapter.OnRecoveryCompleted();
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _activeRecovery?.Exit(ref context, in invocation);
        _activeRecovery = null;
        _adapter.SetStateElapsed(0.0f);
    }
}
