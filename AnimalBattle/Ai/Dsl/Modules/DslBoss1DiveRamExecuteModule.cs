using System;
using System.Collections.Generic;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Core;
using AnimalBattle.Core.Physics;

namespace AnimalBattle.Ai.Dsl.Modules;

/// <summary>
/// Boss1 の俯衝突進（DiveRam）を宣言的パイプライン定義に基づいて実行する DSL モジュールです。
/// </summary>
public sealed class DslBoss1DiveRamExecuteModule : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBoss1Configuration _configuration;
    private readonly Boss1BehaviorState _state;
    private ExecuteGate? _gate;
    private Boss1AttackPattern? _pattern;
    private float _elapsed;

    /// <inheritdoc />
    public BossState Id => Boss1StateIds.DiveRamExecute;
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss1Configuration", "AttackPort", "MovementPort"];

    /// <summary>Boss1 設定および共有状態を注入して DiveRam モジュールを生成します。</summary>
    public DslBoss1DiveRamExecuteModule(IBoss1Configuration configuration, Boss1BehaviorState state)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _elapsed = 0.0f;
        _state.StateElapsed = 0.0f;
        _state.AttackStartPosition = _state.CurrentPosition;
        _state.AttackTargetPosition = new Vec2f(context.PlayerObservation.PositionX, context.PlayerObservation.PositionY);
        _state.AttackTargetEntityId = context.PlayerObservation.EntityId;

        if (!_configuration.GetAttack(BossSkill.DiveRam, out _pattern) || _pattern is null)
        {
            _pattern = new Boss1AttackPattern(BossSkill.DiveRam, 0, 0, 0);
        }

        _gate = new ExecuteGate(_pattern.Definition);
        _gate.Reset();
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (!DslBoss1Support.IsBoss1(in context, in invocation) || _state.IsDead)
        {
            return;
        }

        ExecuteGate? gate = _gate;
        Boss1AttackPattern? pattern = _pattern;
        if (gate is null || pattern is null)
        {
            return;
        }

        ExecuteResult result = gate.Execute(ref context, in invocation);
        if (!result.Started)
        {
            _state.RequestTransition(Boss1StateIds.Recovery, new TransitionReason(ReasonKind.SafetyFallback, skill: pattern.Skill, errorCode: ErrorCode.InvalidConfiguration));
            return;
        }

        float duration = pattern.Definition.ExecuteDuration;
        Vec2f startPos = _state.AttackStartPosition;
        Vec2f targetPos = _state.AttackTargetPosition;

        float progress = Math.Clamp(_elapsed / Math.Max(0.01f, duration), 0.0f, 1.0f);
        float easeProgress = pattern.EaseType switch
        {
            EaseType.InQuad => progress * progress,
            EaseType.OutQuad => progress * (2.0f - progress),
            EaseType.InOutQuad => progress < 0.5f ? 2.0f * progress * progress : -1.0f + ((4.0f - (2.0f * progress)) * progress),
            _ => progress * progress
        };

        float nextX = startPos.X + ((targetPos.X - startPos.X) * easeProgress);
        float nextY = startPos.Y + ((targetPos.Y - startPos.Y) * easeProgress);
        Vec2f position = new(nextX, nextY);

        _state.CurrentPosition = position;
        if (context.Movement.MoveTo(position.X, position.Y).IsError)
        {
            DslBoss1Support.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "DiveRam の移動 command を発行できませんでした。", pattern.Skill, AttackStage.Execute);
        }

        _elapsed = MathF.Min(duration, _elapsed + context.DeltaTime);
        _state.StateElapsed = _elapsed;
        if (result.IsComplete || _elapsed >= duration)
        {
            _state.RequestTransition(Boss1StateIds.Recovery, new TransitionReason(ReasonKind.AttackCompleted, skill: pattern.Skill));
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _gate?.Reset();
        _elapsed = 0.0f;
    }
}
