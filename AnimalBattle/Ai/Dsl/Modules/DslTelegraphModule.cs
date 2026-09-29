using System;
using System.Collections.Generic;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using ErrorOr;

namespace AnimalBattle.Ai.Dsl.Modules;

/// <summary>
/// 宣言的パイプラインで使用される汎用の攻撃予兆（Telegraph）状態モジュールです。
/// Boss1 および Boss2 で共通利用され、攻撃前揺れと目標固定を処理します。
/// </summary>
/// <remarks>
/// 汎用予兆モジュールを初期化します。
/// </remarks>
/// <param name="adapter">Boss 攻撃アダプター。</param>
public sealed class DslTelegraphModule(IBossAttackAdapter adapter) : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBossAttackAdapter _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
    private TelegraphModule? _activeTelegraph;

    /// <inheritdoc />
    public BossState HandledState => _adapter.TelegraphState;

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["EffectPort", "MovementPort", "ObservationService"];

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        BossSkill skill = _adapter.CurrentSkill;
        if (!skill.IsValid())
        {
            _activeTelegraph?.Exit(ref context, in invocation);
            _adapter.RequestTransition(_adapter.DefaultState, new TransitionReason(ReasonKind.SafetyFallback, skill: skill, errorCode: ErrorCode.InvalidBossSkill));
            return;
        }

        ErrorOr<AttackDefinition> definitionResult = _adapter.GetAttackDefinition(skill);
        if (definitionResult.IsError)
        {
            _activeTelegraph?.Exit(ref context, in invocation);
            _adapter.RequestTransition(_adapter.DefaultState, new(ReasonKind.SafetyFallback, skill: skill, errorCode: ErrorCode.InvalidConfiguration));
            return;
        }

        if (_activeTelegraph is null)
        {
            _activeTelegraph = new(_adapter.TelegraphState, definitionResult.Value);
        }
        else
        {
            _activeTelegraph.Reset(_adapter.TelegraphState, definitionResult.Value);
        }

        _adapter.OnTelegraphEnter(ref context, in invocation);
        _activeTelegraph.Enter(ref context, in invocation);

        if (!_activeTelegraph.IsActive && !_activeTelegraph.IsComplete)
        {
            _adapter.RequestTransition(_adapter.DefaultState, new(ReasonKind.SafetyFallback, skill: skill, errorCode: ErrorCode.InvalidConfiguration));
        }
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_activeTelegraph is null || !_activeTelegraph.IsActive)
        {
            return;
        }

        _activeTelegraph.Update(ref context, in invocation);
        _adapter.SetStateElapsed(_activeTelegraph.Elapsed);

        if (_activeTelegraph.IsComplete)
        {
            BossState executeState = _adapter.ExecuteState;
            if (!executeState.IsValid)
            {
                _adapter.RequestTransition(_adapter.DefaultState, new TransitionReason(ReasonKind.SafetyFallback, skill: _adapter.CurrentSkill, errorCode: ErrorCode.InvalidBossSkill));
                return;
            }

            _adapter.RequestTransition(executeState, new TransitionReason(ReasonKind.TimerElapsed, skill: _adapter.CurrentSkill));
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _activeTelegraph?.Exit(ref context, in invocation);
    }
}
