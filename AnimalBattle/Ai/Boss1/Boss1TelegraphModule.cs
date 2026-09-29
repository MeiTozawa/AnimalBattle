using System;
using System.Collections.Generic;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss1;

/// <summary>
/// Boss1 の攻撃予兆（Telegraph）状態モジュールです。
/// 攻撃前揺れと目標固定を処理します。
/// </summary>
public sealed class Boss1TelegraphModule(IBoss1Configuration configuration, Boss1BehaviorState state)
    : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBoss1Configuration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly Boss1BehaviorState _state = state ?? throw new ArgumentNullException(nameof(state));
    private TelegraphModule? _activeTelegraph;

    /// <inheritdoc />
    public BossState HandledState => Boss1State.Telegraph;

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss1Configuration", "EffectPort", "MovementPort", "ObservationService"];

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.AttackStartPosition = _state.CurrentPosition;
        _state.AttackTargetPosition = new(context.PlayerObservation.PositionX, context.PlayerObservation.PositionY);
        _state.AttackTargetEntityId = context.PlayerObservation.EntityId;
        _state.StateElapsed = 0.0f;

        BossSkill skill = _state.CurrentSkill;
        if (!skill.IsValid())
        {
            _activeTelegraph?.Exit(ref context, in invocation);
            _state.RequestTransition(Boss1State.Hover, new TransitionReason(ReasonKind.SafetyFallback, skill: skill, errorCode: ErrorCode.InvalidBossSkill));
            return;
        }

        if (!_configuration.GetAttack(skill, out Boss1AttackPattern pattern))
        {
            _activeTelegraph?.Exit(ref context, in invocation);
            _state.RequestTransition(Boss1State.Hover, new TransitionReason(ReasonKind.SafetyFallback, skill: skill, errorCode: ErrorCode.InvalidConfiguration));
            return;
        }

        if (_activeTelegraph is null)
        {
            _activeTelegraph = new(Boss1State.Telegraph, pattern.Definition);
        }
        else
        {
            _activeTelegraph.Reset(Boss1State.Telegraph, pattern.Definition);
        }

        _activeTelegraph.Enter(ref context, in invocation);

        if (!_activeTelegraph.IsActive && !_activeTelegraph.IsComplete)
        {
            _state.RequestTransition(Boss1State.Hover, new TransitionReason(ReasonKind.SafetyFallback, skill: skill, errorCode: ErrorCode.InvalidConfiguration));
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
        _state.StateElapsed = _activeTelegraph.Elapsed;

        if (_activeTelegraph.IsComplete)
        {
            _state.RequestTransition(Boss1State.Execute, new TransitionReason(ReasonKind.TimerElapsed, skill: _state.CurrentSkill));
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _activeTelegraph?.Exit(ref context, in invocation);
    }
}
