using System;
using System.Collections.Generic;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Dsl.Modules;

/// <summary>
/// Boss1 のすべての攻撃技能（各種弾幕および俯衝突進 DiveRam）の Execute ライフサイクルを宣言的定義に基づいて一元処理する汎用実行モジュールです。
/// </summary>
/// <param name="configuration">Boss1 設定境界。</param>
/// <param name="state">Boss1 実行時状態。</param>
public sealed class DslBoss1ExecuteModule(IBoss1Configuration configuration, Boss1BehaviorState state)
    : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBoss1Configuration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly Boss1BehaviorState _state = state ?? throw new ArgumentNullException(nameof(state));
    private ExecuteGate? _gate;
    private Boss1AttackPattern? _pattern;
    private float _elapsed;
    private float _nextEmission;
    private int _emissionIndex;

    /// <inheritdoc />
    public BossState HandledState => Boss1State.Execute;

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss1Configuration", "AttackPort", "MovementPort", "RandomSource"];

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _elapsed = 0.0f;
        _nextEmission = 0.0f;
        _emissionIndex = 0;
        _state.StateElapsed = 0.0f;
        _state.AttackSubTimer = 0.0f;

        BossSkill skill = _state.CurrentSkill;
        if (!_configuration.GetAttack(skill, out _pattern) || _pattern is null)
        {
            _pattern = new(skill, 0, 0, 0);
        }

        if (_gate is null)
        {
            _gate = new(_pattern.Definition);
        }
        else
        {
            _gate.Reset(_pattern.Definition);
        }

        if (skill == BossSkill.DiveRam)
        {
            _state.AttackStartPosition = _state.CurrentPosition;
            _state.AttackTargetPosition = new(context.PlayerObservation.PositionX, context.PlayerObservation.PositionY);
            _state.AttackTargetEntityId = context.PlayerObservation.EntityId;
        }

        if (!DslBoss1Support.IsBoss1(in context, in invocation) || _pattern.Skill != skill)
        {
            DslBoss1Support.Record(in context, in invocation, DiagnosticSeverity.Warning, ErrorCode.InvalidBossSkill, "Boss1 の Execute 対象が現在攻撃と一致しません。", skill, AttackStage.Execute);
        }
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
            _state.RequestTransition(Boss1State.Recovery, new TransitionReason(ReasonKind.SafetyFallback, skill: pattern.Skill, errorCode: ErrorCode.InvalidConfiguration));
            return;
        }

        if (pattern.Skill == BossSkill.DiveRam)
        {
            UpdateDiveRam(ref context, in invocation, pattern, in result);
        }
        else
        {
            UpdateBarrage(ref context, in invocation, pattern, in result);
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _gate?.Reset();
        _elapsed = 0.0f;
    }

    private void UpdateDiveRam(ref StateContext context, in LifecycleInvocation invocation, Boss1AttackPattern pattern, in ExecuteResult result)
    {
        float duration = pattern.Definition.ExecuteDuration;
        Vec2f startPos = _state.AttackStartPosition;
        Vec2f targetPos = _state.AttackTargetPosition;

        float progress = Math.Clamp(_elapsed / Math.Max(0.01f, duration), 0.0f, 1.0f);
        float easeProgress = EasingMath.Evaluate(pattern.EaseType, progress);

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
            _state.RequestTransition(Boss1State.Recovery, new TransitionReason(ReasonKind.AttackCompleted, skill: pattern.Skill));
        }
    }

    private void UpdateBarrage(ref StateContext context, in LifecycleInvocation invocation, Boss1AttackPattern pattern, in ExecuteResult result)
    {
        DslBoss1Support.ApplyHoverMovement(ref context, in invocation, _configuration, _state, pattern.Skill, AttackStage.Execute);
        _state.AttackStartPosition = _state.CurrentPosition;

        float duration = pattern.Definition.ExecuteDuration;
        float interval = pattern.Interval;
        if (interval > 0.0f && _elapsed >= _nextEmission && _elapsed <= duration)
        {
            EmitBarrageWave(ref context, in invocation, pattern);
            _nextEmission += interval;
            _emissionIndex++;
        }

        _elapsed = MathF.Min(duration, _elapsed + context.DeltaTime);
        _state.StateElapsed = _elapsed;
        _state.AttackSubTimer = _elapsed;

        if (result.IsComplete || _elapsed >= duration)
        {
            _state.RequestTransition(Boss1State.Recovery, new TransitionReason(ReasonKind.AttackCompleted, skill: pattern.Skill));
        }
    }

    private void EmitBarrageWave(ref StateContext context, in LifecycleInvocation invocation, Boss1AttackPattern pattern)
    {
        switch (pattern.Skill)
        {
            case BossSkill.Aimed3Shot:
                EmitAimed3Shot(ref context, in invocation, pattern);
                break;
            case BossSkill.RingBurst:
                EmitRingBurst(ref context, in invocation, pattern);
                break;
            case BossSkill.DoubleSpiral:
                EmitDoubleSpiral(ref context, in invocation, pattern);
                break;
            case BossSkill.RainDrop:
                EmitRainDrop(ref context, in invocation, pattern);
                break;
            case BossSkill.CrossChaos:
                EmitCrossChaos(ref context, in invocation, pattern);
                break;
            case BossSkill.FastSniper:
                EmitFastSniper(ref context, in invocation, pattern);
                break;
            default:
                break;
        }
    }

    private void EmitAimed3Shot(ref StateContext context, in LifecycleInvocation invocation, Boss1AttackPattern pattern)
    {
        float dx = context.PlayerObservation.PositionX - _state.AttackStartPosition.X;
        float dy = context.PlayerObservation.PositionY - _state.AttackStartPosition.Y;
        float length = MathF.Sqrt((dx * dx) + (dy * dy));
        if (length <= 0.0001f)
        {
            dx = 1.0f;
            dy = 0.0f;
        }

        float baseAngle = MathF.Atan2(dy, dx);
        for (int index = -1; index <= 1; index++)
        {
            float angle = baseAngle + (index * pattern.AngleStep);
            if (context.Attack.EmitBullet(pattern.Skill, MathF.Cos(angle) * pattern.BulletSpeed, MathF.Sin(angle) * pattern.BulletSpeed).IsError)
            {
                DslBoss1Support.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "Aimed3Shot の弾発行に失敗しました。", pattern.Skill, AttackStage.Execute);
            }
        }
    }

    private void EmitRingBurst(ref StateContext context, in LifecycleInvocation invocation, Boss1AttackPattern pattern)
    {
        int count = Math.Max(4, pattern.BulletCount);
        for (int index = 0; index < count; index++)
        {
            float angle = ((index / (float)count) * MathF.Tau) + _state.AngleOffset;
            if (context.Attack.EmitBullet(pattern.Skill, MathF.Cos(angle) * pattern.BulletSpeed, MathF.Sin(angle) * pattern.BulletSpeed).IsError)
            {
                DslBoss1Support.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "RingBurst の弾発行に失敗しました。", pattern.Skill, AttackStage.Execute);
            }
        }

        _state.AngleOffset += pattern.AngleStep;
    }

    private void EmitDoubleSpiral(ref StateContext context, in LifecycleInvocation invocation, Boss1AttackPattern pattern)
    {
        float first = _state.AngleOffset;
        float second = first + MathF.PI;
        if (context.Attack.EmitBullet(pattern.Skill, MathF.Cos(first) * pattern.BulletSpeed, MathF.Sin(first) * pattern.BulletSpeed).IsError
            || context.Attack.EmitBullet(pattern.Skill, MathF.Cos(second) * pattern.BulletSpeed, MathF.Sin(second) * pattern.BulletSpeed).IsError)
        {
            DslBoss1Support.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "DoubleSpiral の弾発行に失敗しました。", pattern.Skill, AttackStage.Execute);
        }

        _state.AngleOffset += pattern.AngleStep;
    }

    private void EmitRainDrop(ref StateContext context, in LifecycleInvocation invocation, Boss1AttackPattern pattern)
    {
        float range = pattern.MaxX - pattern.MinX;
        float firstX = pattern.MinX + (context.Random.NextSingle() * range);
        float secondX = pattern.MinX + (context.Random.NextSingle() * range);
        bool firstError = context.Attack.EmitBullet(pattern.Skill, firstX, pattern.BulletSpeed).IsError;
        bool secondError = context.Attack.EmitBullet(pattern.Skill, secondX, pattern.BulletSpeed).IsError;
        if (firstError || secondError)
        {
            DslBoss1Support.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "RainDrop の弾発行に失敗しました。", pattern.Skill, AttackStage.Execute);
        }
    }

    private void EmitCrossChaos(ref StateContext context, in LifecycleInvocation invocation, Boss1AttackPattern pattern)
    {
        bool hasError = false;
        for (int index = 0; index < 4; index++)
        {
            float angle = _state.AngleOffset + (index * (MathF.PI / 2.0f));
            if (context.Attack.EmitBullet(pattern.Skill, MathF.Cos(angle) * pattern.BulletSpeed, MathF.Sin(angle) * pattern.BulletSpeed).IsError)
            {
                hasError = true;
            }
        }

        if (hasError)
        {
            DslBoss1Support.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "CrossChaos の弾発行に失敗しました。", pattern.Skill, AttackStage.Execute);
        }

        _state.AngleOffset += pattern.AngleStep;
    }

    private void EmitFastSniper(ref StateContext context, in LifecycleInvocation invocation, Boss1AttackPattern pattern)
    {
        float dx = context.PlayerObservation.PositionX - _state.AttackStartPosition.X;
        float dy = context.PlayerObservation.PositionY - _state.AttackStartPosition.Y;
        float angle = MathF.Atan2(dy, dx);
        if (context.Attack.EmitBullet(pattern.Skill, MathF.Cos(angle) * pattern.BulletSpeed, MathF.Sin(angle) * pattern.BulletSpeed).IsError)
        {
            DslBoss1Support.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "FastSniper の弾発行に失敗しました。", pattern.Skill, AttackStage.Execute);
        }
    }
}
