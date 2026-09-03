using System;
using System.Collections.Generic;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Core.Physics;

namespace AnimalBattle.Ai.Dsl.Modules;

/// <summary>
/// Boss1 の各技能の予兆（Telegraph）を宣言的パイプライン定義に基づいて一元実行する DSL モジュールです。
/// </summary>
public sealed class DslBoss1TelegraphModule : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBoss1Configuration _configuration;
    private readonly Boss1BehaviorState _state;
    private TelegraphModule? _activeTelegraph;

    /// <inheritdoc />
    public BossState Id => BossState.Boss1_Telegraph;
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss1Configuration", "EffectPort", "MovementPort", "ObservationService"];

    /// <summary>Boss1 設定および共有状態を注入して Telegraph モジュールを生成します。</summary>
    public DslBoss1TelegraphModule(IBoss1Configuration configuration, Boss1BehaviorState state)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _activeTelegraph = null;
        if (!DslBoss1Support.IsBoss1(in context, in invocation) || !_state.CurrentSkill.IsValid())
        {
            DslBoss1Support.Record(in context, in invocation, DiagnosticSeverity.Warning, ErrorCode.InvalidBossSkill, "Boss1 の Telegraph 対象が不正です。", _state.CurrentSkill);
            _state.RequestTransition(BossState.Boss1_Hover, new TransitionReason(ReasonKind.SafetyFallback, skill: _state.CurrentSkill, errorCode: ErrorCode.InvalidBossSkill));
            return;
        }

        if (!_configuration.GetAttack(_state.CurrentSkill, out Boss1AttackPattern pattern))
        {
            pattern = new Boss1AttackPattern(_state.CurrentSkill, 0, 0, 0);
        }

        _activeTelegraph = new TelegraphModule(BossState.Boss1_Telegraph, pattern.Definition);
        _state.AttackStartPosition = _state.CurrentPosition;
        _state.AttackTargetPosition = new Vec2f(context.PlayerObservation.PositionX, context.PlayerObservation.PositionY);
        _state.AttackTargetEntityId = context.PlayerObservation.EntityId;
        _activeTelegraph.Enter(ref context, in invocation);

        if (!_activeTelegraph.IsActive && !_activeTelegraph.IsComplete)
        {
            _state.RequestTransition(BossState.Boss1_Hover, new TransitionReason(ReasonKind.SafetyFallback, skill: _state.CurrentSkill, errorCode: ErrorCode.InvalidConfiguration));
        }
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_activeTelegraph is null || !_activeTelegraph.IsActive)
        {
            return;
        }

        if (_state.CurrentSkill == BossSkill.DiveRam)
        {
            _state.AttackTargetPosition = new Vec2f(context.PlayerObservation.PositionX, context.PlayerObservation.PositionY);
            if (context.Phase is Phase.Phase1 or Phase.Phase2 or Phase.Phase3)
            {
                Boss1PhaseSettings settings = _configuration.GetPhase(context.Phase);
                float shake = settings.TelegraphShakeIntensity;
                float offsetX = (Random.Shared.NextSingle() - 0.5f) * shake;
                float offsetY = (Random.Shared.NextSingle() - 0.5f) * shake;
                float sx = _state.AttackStartPosition.X + offsetX;
                float sy = _state.AttackStartPosition.Y + offsetY;
                _state.CurrentPosition = new Vec2f(sx, sy);
                if (context.Movement.MoveTo(sx, sy).IsError)
                {
                    DslBoss1Support.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "DiveRam の予兆振動移動 command を発行できませんでした。", _state.CurrentSkill, AttackStage.Telegraph);
                }
            }
        }
        else
        {
            DslBoss1Support.ApplyHoverMovement(ref context, in invocation, _configuration, _state, _state.CurrentSkill, AttackStage.Telegraph);
            _state.AttackStartPosition = _state.CurrentPosition;
        }

        _activeTelegraph.Update(ref context, in invocation);
        if (_activeTelegraph.IsComplete)
        {
            BossState executeState = BossState.Boss1_Execute;
            if (!executeState.IsValid())
            {
                _state.RequestTransition(BossState.Boss1_Hover, new TransitionReason(ReasonKind.SafetyFallback, skill: _state.CurrentSkill, errorCode: ErrorCode.InvalidBossSkill));
                return;
            }

            _state.RequestTransition(executeState, new TransitionReason(ReasonKind.TimerElapsed, skill: _state.CurrentSkill));
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _activeTelegraph?.Exit(ref context, in invocation);
        _activeTelegraph = null;
    }
}
