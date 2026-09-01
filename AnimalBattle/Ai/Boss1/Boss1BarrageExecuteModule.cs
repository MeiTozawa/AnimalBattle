using System;
using System.Collections.Generic;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss1;

/// <summary>
/// Boss1 の各種弾幕攻撃の Execute lifecycle を統合処理する汎用弾幕モジュールです。
/// </summary>
public class Boss1BarrageExecuteModule : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBoss1Configuration _configuration;
    private readonly Boss1BehaviorState _state;
    private readonly BossState _stateId;
    private ExecuteGate? _gate;
    private float _elapsed;
    private float _nextEmission;
    private int _emissionIndex;

    /// <summary>設定を取得します。</summary>
    protected IBoss1Configuration Configuration => _configuration;
    /// <summary>共有 Boss1 実行値を取得します。</summary>
    protected Boss1BehaviorState State => _state;
    /// <summary>現在の攻撃 pattern を取得します。</summary>
    protected Boss1AttackPattern Pattern { get; private set; }
    /// <inheritdoc />
    public BossState Id => _stateId;
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss1Configuration", "AttackPort", "MovementPort", "RandomSource"];

    /// <summary>
    /// 指定された状態識別子と攻撃識別子に対応する Boss1 弾幕 Execute module を生成します。
    /// </summary>
    /// <param name="stateId">対応する状態識別子。</param>
    /// <param name="configuration">Boss1 設定境界。</param>
    /// <param name="state">Boss1 実行時状態。</param>
    /// <param name="attackId">対象の攻撃識別子。</param>
    public Boss1BarrageExecuteModule(BossState stateId, IBoss1Configuration configuration, Boss1BehaviorState state, AttackId attackId)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _stateId = stateId;
        _configuration = configuration;
        _state = state ?? throw new ArgumentNullException(nameof(state));
        if (!configuration.GetAttack(attackId, out Boss1AttackPattern pattern))
        {
            pattern = new Boss1AttackPattern(attackId, Boss1AttackKind.Aimed3Shot, 0, 0, 0);
        }

        Pattern = pattern;
        _gate = new ExecuteGate(pattern.Definition);
    }

    /// <inheritdoc />
    public virtual void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _elapsed = 0.0f;
        _nextEmission = 0.0f;
        _emissionIndex = 0;
        _state.StateElapsed = 0.0f;
        _state.AttackSubTimer = 0.0f;
        _gate!.Reset();
        if (!Boss1ModuleSupport.IsBoss1(in context, in invocation) || _state.CurrentAttackId != Pattern.AttackId)
        {
            Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.Warning, ErrorCode.InvalidAttackId, "Boss1 の Execute 対象が現在攻撃と一致しません。", Pattern.AttackId, AttackStage.Execute);
        }
    }

    /// <inheritdoc />
    public virtual void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (!Boss1ModuleSupport.IsBoss1(in context, in invocation) || _state.IsDead || _gate is null)
        {
            return;
        }

        ExecuteResult result = _gate.Execute(ref context, in invocation);
        if (!result.Started)
        {
            _state.RequestTransition(Boss1StateIds.Recovery, new TransitionReason(ReasonKind.SafetyFallback, attackId: Pattern.AttackId, errorCode: ErrorCode.InvalidConfiguration));
            return;
        }

        Boss1ModuleSupport.ApplyHoverMovement(ref context, in invocation, _configuration, _state, Pattern.AttackId, AttackStage.Execute);
        _state.AttackStartPosition = _state.CurrentPosition;

        float duration = Pattern.Definition.ExecuteDuration;
        float interval = Pattern.Interval;
        if (interval > 0 && _elapsed >= _nextEmission && _elapsed <= duration)
        {
            EmitDueComponents(ref context, in invocation);
            _nextEmission += interval;
        }

        _elapsed = MathF.Min(duration, _elapsed + context.DeltaTime);
        _state.StateElapsed = _elapsed;
        _state.AttackSubTimer = MathF.Max(0.0f, _nextEmission - _elapsed);
        if (result.IsComplete || _elapsed >= duration)
        {
            _state.RequestTransition(Boss1StateIds.Recovery, new TransitionReason(ReasonKind.AttackCompleted, attackId: Pattern.AttackId));
        }
    }

    /// <inheritdoc />
    public virtual void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _gate?.Reset();
        _state.AttackSubTimer = 0.0f;
    }

    /// <summary>
    /// 一回の発射時点に必要な Boss1 固有弾幕 component を port へ渡します。
    /// </summary>
    /// <param name="context">状態コンテキスト。</param>
    /// <param name="invocation">ライフサイクル呼び出しメタデータ。</param>
    /// <param name="emissionIndex">現在の発射回数インデックス。</param>
    protected virtual void EmitComponents(ref StateContext context, in LifecycleInvocation invocation, int emissionIndex)
    {
        switch (Pattern.Kind)
        {
            case Boss1AttackKind.Aimed3Shot:
                EmitAimed3Shot(ref context, in invocation);
                break;
            case Boss1AttackKind.RingBurst:
                EmitRingBurst(ref context, in invocation);
                break;
            case Boss1AttackKind.DoubleSpiral:
                EmitDoubleSpiral(ref context, in invocation);
                break;
            case Boss1AttackKind.RainDrop:
                EmitRainDrop(ref context, in invocation);
                break;
            case Boss1AttackKind.CrossChaos:
                EmitCrossChaos(ref context, in invocation);
                break;
            case Boss1AttackKind.FastSniper:
                EmitFastSniper(ref context, in invocation);
                break;
            default:
                break;
        }
    }

    private void EmitDueComponents(ref StateContext context, in LifecycleInvocation invocation)
    {
        EmitComponents(ref context, in invocation, _emissionIndex++);
    }

    private void EmitAimed3Shot(ref StateContext context, in LifecycleInvocation invocation)
    {
        float dx = context.PlayerObservation.PositionX - State.AttackStartPosition.X;
        float dy = context.PlayerObservation.PositionY - State.AttackStartPosition.Y;
        float length = MathF.Sqrt((dx * dx) + (dy * dy));
        if (length <= 0.0001f)
        {
            dx = 1.0f;
            dy = 0.0f;
        }

        float baseAngle = MathF.Atan2(dy, dx);
        for (int index = -1; index <= 1; index++)
        {
            float angle = baseAngle + (index * Pattern.AngleStep);
            if (context.Attack.EmitBullet(Pattern.AttackId, MathF.Cos(angle) * Pattern.BulletSpeed, MathF.Sin(angle) * Pattern.BulletSpeed).IsError)
            {
                Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "Aimed3Shot の弾発行に失敗しました。", Pattern.AttackId, AttackStage.Execute);
            }
        }
    }

    private void EmitRingBurst(ref StateContext context, in LifecycleInvocation invocation)
    {
        int count = Math.Max(4, Pattern.BulletCount);
        for (int index = 0; index < count; index++)
        {
            float angle = ((index / (float)count) * MathF.Tau) + State.AngleOffset;
            if (context.Attack.EmitBullet(Pattern.AttackId, MathF.Cos(angle) * Pattern.BulletSpeed, MathF.Sin(angle) * Pattern.BulletSpeed).IsError)
            {
                Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "RingBurst の弾発行に失敗しました。", Pattern.AttackId, AttackStage.Execute);
            }
        }

        State.AngleOffset += Pattern.AngleStep;
    }

    private void EmitDoubleSpiral(ref StateContext context, in LifecycleInvocation invocation)
    {
        float first = State.AngleOffset;
        float second = first + MathF.PI;
        if (context.Attack.EmitBullet(Pattern.AttackId, MathF.Cos(first) * Pattern.BulletSpeed, MathF.Sin(first) * Pattern.BulletSpeed).IsError
            || context.Attack.EmitBullet(Pattern.AttackId, MathF.Cos(second) * Pattern.BulletSpeed, MathF.Sin(second) * Pattern.BulletSpeed).IsError)
        {
            Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "DoubleSpiral の弾発行に失敗しました。", Pattern.AttackId, AttackStage.Execute);
        }

        State.AngleOffset += Pattern.AngleStep;
    }

    private void EmitRainDrop(ref StateContext context, in LifecycleInvocation invocation)
    {
        float range = Pattern.MaxX - Pattern.MinX;
        float firstX = Pattern.MinX + (context.Random.NextSingle() * range);
        float secondX = Pattern.MinX + (context.Random.NextSingle() * range);
        bool firstError = context.Attack.EmitBullet(Pattern.AttackId, firstX, Pattern.BulletSpeed).IsError;
        bool secondError = context.Attack.EmitBullet(Pattern.AttackId, secondX, Pattern.BulletSpeed).IsError;
        if (firstError || secondError)
        {
            Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "RainDrop の弾発行に失敗しました。", Pattern.AttackId, AttackStage.Execute);
        }
    }

    private void EmitCrossChaos(ref StateContext context, in LifecycleInvocation invocation)
    {
        bool hasError = false;
        for (int index = 0; index < 4; index++)
        {
            float angle = State.AngleOffset + (index * (MathF.PI / 2.0f));
            if (context.Attack.EmitBullet(Pattern.AttackId, MathF.Cos(angle) * Pattern.BulletSpeed, MathF.Sin(angle) * Pattern.BulletSpeed).IsError)
            {
                hasError = true;
            }
        }

        if (hasError)
        {
            Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "CrossChaos の弾発行に失敗しました。", Pattern.AttackId, AttackStage.Execute);
        }

        State.AngleOffset += Pattern.AngleStep;
    }

    private void EmitFastSniper(ref StateContext context, in LifecycleInvocation invocation)
    {
        float dx = context.PlayerObservation.PositionX - State.AttackStartPosition.X;
        float dy = context.PlayerObservation.PositionY - State.AttackStartPosition.Y;
        float angle = MathF.Atan2(dy, dx);
        if (context.Attack.EmitBullet(Pattern.AttackId, MathF.Cos(angle) * Pattern.BulletSpeed, MathF.Sin(angle) * Pattern.BulletSpeed).IsError)
        {
            Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "FastSniper の弾発行に失敗しました。", Pattern.AttackId, AttackStage.Execute);
        }
    }
}
