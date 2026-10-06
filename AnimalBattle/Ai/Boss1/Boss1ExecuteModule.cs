using System.Diagnostics;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss1;

/// <summary>
///     Boss1 のすべての攻撃技能（各種弾幕および俯衝突進 DiveRam）の Execute ライフサイクルを一元処理する実行モジュールです。
/// </summary>
/// <param name="configuration">Boss1 設定境界。</param>
/// <param name="state">Boss1 実行時状態。</param>
public sealed class Boss1ExecuteModule(IBoss1Configuration configuration, Boss1BehaviorState state)
    : IBossStateModule, IStateModuleDependencyProvider {
    private readonly IBoss1Configuration _configuration =
        configuration ?? throw new ArgumentNullException(nameof(configuration));

    private readonly Boss1BehaviorState _state = state ?? throw new ArgumentNullException(nameof(state));
    private float _elapsed;
    private ExecuteGate? _gate;
    private float _nextEmission;
    private Boss1AttackPattern? _pattern;

    /// <inheritdoc />
    public BossState HandledState => Boss1State.Execute;

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _elapsed = 0.0f;
        _nextEmission = 0.0f;
        _state.StateElapsed = 0.0f;
        _state.AttackSubTimer = 0.0f;

        var skill = _state.CurrentSkill;
        if (!_configuration.GetAttack(skill, out _pattern) || _pattern is null)
            _pattern = new Boss1AttackPattern(skill, 0, 0, 0);

        if (_gate is null)
            _gate = new ExecuteGate(_pattern.Definition);
        else
            _gate.Reset(_pattern.Definition);

        if (skill == BossSkill.DiveRam)
        {
            _state.AttackStartPosition = _state.CurrentPosition;
            _state.AttackTargetPosition =
                new Vec2f(context.PlayerObservation.PositionX, context.PlayerObservation.PositionY);
            _state.AttackTargetEntityId = context.PlayerObservation.EntityId;
        }
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (!Boss1ModuleSupport.IsBoss1(in context, in invocation) || _state.IsDead) return;

        var gate = _gate;
        var pattern = _pattern;
        if (gate is null || pattern is null) return;

        var result = gate.Execute(ref context, in invocation, false, _state.CurrentPosition.X, _state.CurrentPosition.Y);
        if (!result.Started)
        {
            _state.RequestTransition(Boss1State.Recovery,
                new TransitionReason(ReasonKind.SafetyFallback, skill: pattern.Skill,
                    errorCode: ErrorCode.InvalidConfiguration));
            return;
        }

        if (pattern.Skill == BossSkill.DiveRam)
            UpdateDiveRam(ref context, in invocation, pattern, in result);
        else
            UpdateBarrage(ref context, in invocation, pattern, in result);
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _gate?.Reset();
        _elapsed = 0.0f;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } =
        ["Boss1Configuration", "AttackPort", "MovementPort", "RandomSource"];

    private void UpdateDiveRam(ref StateContext context, in LifecycleInvocation invocation, Boss1AttackPattern pattern,
        in ExecuteResult result)
    {
        var duration = pattern.Definition.ExecuteDuration;
        var startPos = _state.AttackStartPosition;
        var targetPos = _state.AttackTargetPosition;

        var progress = Math.Clamp(_elapsed / Math.Max(0.01f, duration), 0.0f, 1.0f);
        var easeProgress = EasingMath.Evaluate(pattern.EaseType, progress);

        var nextX = startPos.X + (targetPos.X - startPos.X) * easeProgress;
        var nextY = startPos.Y + (targetPos.Y - startPos.Y) * easeProgress;
        Vec2f position = new(nextX, nextY);

        _state.CurrentPosition = position;
        context.Movement.MoveTo(position.X, position.Y);

        _elapsed = MathF.Min(duration, _elapsed + context.DeltaTime);
        _state.StateElapsed = _elapsed;
        if (result.IsComplete || _elapsed >= duration)
            _state.RequestTransition(Boss1State.Recovery,
                new TransitionReason(ReasonKind.AttackCompleted, skill: pattern.Skill));
    }

    private void UpdateBarrage(ref StateContext context, in LifecycleInvocation invocation, Boss1AttackPattern pattern,
        in ExecuteResult result)
    {
        Boss1ModuleSupport.ApplyHoverMovement(ref context, in invocation, _configuration, _state, pattern.Skill,
            AttackStage.Execute);
        _state.AttackStartPosition = _state.CurrentPosition;

        var duration = pattern.Definition.ExecuteDuration;
        var interval = pattern.Interval;
        if (pattern.Definition.ComponentCount == 0 && interval > 0.0f && _elapsed >= _nextEmission && _elapsed <= duration)
        {
            EmitBarrageWave(ref context, in invocation, pattern);
            _nextEmission += interval;
        }

        _elapsed = MathF.Min(duration, _elapsed + context.DeltaTime);
        _state.StateElapsed = _elapsed;
        _state.AttackSubTimer = _elapsed;

        if (result.IsComplete || _elapsed >= duration)
            _state.RequestTransition(Boss1State.Recovery,
                new TransitionReason(ReasonKind.AttackCompleted, skill: pattern.Skill));
    }

    private void EmitBarrageWave(ref StateContext context, in LifecycleInvocation invocation,
        Boss1AttackPattern pattern)
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
                Debug.Fail($"[Boss1ExecuteModule 警告] 未知または未対応の Barrage スキル: {pattern.Skill}");
            break;
        }
    }

    private void EmitAimed3Shot(ref StateContext context, in LifecycleInvocation invocation, Boss1AttackPattern pattern)
    {
        var dx = context.PlayerObservation.PositionX - _state.AttackStartPosition.X;
        var dy = context.PlayerObservation.PositionY - _state.AttackStartPosition.Y;
        var length = MathF.Sqrt(dx * dx + dy * dy);
        if (length <= 0.0001f)
        {
            dx = 1.0f;
            dy = 0.0f;
        }

        var baseAngle = MathF.Atan2(dy, dx);
        for (var index = -1; index <= 1; index++)
        {
            var angle = baseAngle + index * pattern.AngleStep;
            context.Attack.EmitBullet(pattern.Skill, MathF.Cos(angle) * pattern.BulletSpeed,
                MathF.Sin(angle) * pattern.BulletSpeed);
        }
    }

    private void EmitRingBurst(ref StateContext context, in LifecycleInvocation invocation, Boss1AttackPattern pattern)
    {
        var count = Math.Max(4, pattern.BulletCount);
        for (var index = 0; index < count; index++)
        {
            var angle = index / (float)count * MathF.Tau + _state.AngleOffset;
            context.Attack.EmitBullet(pattern.Skill, MathF.Cos(angle) * pattern.BulletSpeed,
                MathF.Sin(angle) * pattern.BulletSpeed);
        }

        _state.AngleOffset += pattern.AngleStep;
    }

    private void EmitDoubleSpiral(ref StateContext context, in LifecycleInvocation invocation,
        Boss1AttackPattern pattern)
    {
        var first = _state.AngleOffset;
        var second = first + MathF.PI;
        context.Attack.EmitBullet(pattern.Skill, MathF.Cos(first) * pattern.BulletSpeed,
            MathF.Sin(first) * pattern.BulletSpeed);
        context.Attack.EmitBullet(pattern.Skill, MathF.Cos(second) * pattern.BulletSpeed,
            MathF.Sin(second) * pattern.BulletSpeed);

        _state.AngleOffset += pattern.AngleStep;
    }

    private static void EmitRainDrop(ref StateContext context, in LifecycleInvocation invocation, Boss1AttackPattern pattern)
    {
        var range = pattern.MaxX - pattern.MinX;
        var firstX = pattern.MinX + context.Random.NextSingle() * range;
        var secondX = pattern.MinX + context.Random.NextSingle() * range;
        context.Attack.EmitBullet(pattern.Skill, firstX, pattern.BulletSpeed);
        context.Attack.EmitBullet(pattern.Skill, secondX, pattern.BulletSpeed);
    }

    private void EmitCrossChaos(ref StateContext context, in LifecycleInvocation invocation, Boss1AttackPattern pattern)
    {
        for (var index = 0; index < 4; index++)
        {
            var angle = _state.AngleOffset + index * (MathF.PI / 2.0f);
            context.Attack.EmitBullet(pattern.Skill, MathF.Cos(angle) * pattern.BulletSpeed,
                MathF.Sin(angle) * pattern.BulletSpeed);
        }

        _state.AngleOffset += pattern.AngleStep;
    }

    private void EmitFastSniper(ref StateContext context, in LifecycleInvocation invocation, Boss1AttackPattern pattern)
    {
        var dx = context.PlayerObservation.PositionX - _state.AttackStartPosition.X;
        var dy = context.PlayerObservation.PositionY - _state.AttackStartPosition.Y;
        var angle = MathF.Atan2(dy, dx);
        context.Attack.EmitBullet(pattern.Skill, MathF.Cos(angle) * pattern.BulletSpeed,
            MathF.Sin(angle) * pattern.BulletSpeed);
    }
}