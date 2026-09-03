using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using ErrorOr;

namespace AnimalBattle.Ai.Attack;

/// <summary>攻撃対象と範囲を表示・観測する共通 Telegraph State_Module です。</summary>
public sealed class TelegraphModule : IBossStateModule
{
    private readonly AttackDefinition _definition;
    private bool _active;
    private float _elapsed;
    private float _startTime;
    private PlayerObservation _target;

    /// <summary>Telegraph の State_Id を取得します。</summary>
    public BossState Id { get; }
    /// <summary>現在の技能識別子を取得します。</summary>
    public BossSkill Skill => _definition.Skill;

    /// <summary>攻撃定義を取得します。</summary>
    public AttackDefinition Definition => _definition;
    /// <summary>Telegraph の攻撃範囲を取得します。</summary>
    public float AttackRange => _definition.Range;
    /// <summary>Telegraph 開始時点の実時間を取得します。</summary>
    public float AttackStartTime => _startTime;
    /// <summary>Telegraph 開始時に固定した対象 Entity を取得します。</summary>
    public ulong TargetEntityId => _target.EntityId;
    /// <summary>Telegraph が現在アクティブかどうかを取得します。</summary>
    public bool IsActive => _active;
    /// <summary>Telegraph が完了したかどうかを取得します。</summary>
    public bool IsComplete => !_active && _elapsed > 0;
    /// <summary>Telegraph の経過時間を取得します。</summary>
    public float Elapsed => _elapsed;
    /// <summary>Telegraph 開始時に固定した対象観測を取得します。</summary>
    public PlayerObservation Target => _target;

    /// <summary>攻撃定義と State_Id を指定して Telegraph を生成します。</summary>
    public TelegraphModule(BossState stateId, AttackDefinition definition)
    {
        if (!stateId.IsValid()) throw new ArgumentOutOfRangeException(nameof(stateId));
        ArgumentNullException.ThrowIfNull(definition);
        Id = stateId;
        _definition = definition;
    }

    /// <summary>Telegraph を開始し、開始時点の対象を固定します。</summary>
    public ErrorOr<Success> Start(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_active)
        {
            return Result.Success;
        }

        if (!_definition.IsValid || !invocation.IsValid)
        {
            Record(in context, in invocation, DiagnosticSeverity.Warning, ErrorCode.InvalidConfiguration, "攻撃時間が不正なため Telegraph を開始せず、安全な非攻撃行動へ移行します。", 0);
            return Error.Validation("TelegraphModule.Start", "攻撃定義またはフレーム呼び出しが不正です。");
        }

        _target = context.PlayerObservation;
        _elapsed = 0;
        _startTime = 0;
        _active = true;
        EmitTelegraph(ref context, in invocation);
        return Result.Success;
    }

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        Start(ref context, in invocation);
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (!_active)
        {
            return;
        }

        EmitTelegraph(ref context, in invocation);
        _elapsed = MathF.Min(_definition.TelegraphDuration, _elapsed + context.DeltaTime);
        if (_elapsed >= _definition.TelegraphDuration)
        {
            _active = false;
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _active = false;
    }

    private void EmitTelegraph(ref StateContext context, in LifecycleInvocation invocation)
    {
        bool displayed;
        try
        {
            displayed = !context.Effect.RequestTelegraph(_target.PositionX, _target.PositionY).IsError;
        }
        catch
        {
            displayed = false;
        }

        if (!displayed)
        {
            Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "攻撃予告の表示に失敗しました。", _elapsed);
            return;
        }

        Record(in context, in invocation, DiagnosticSeverity.Information, ErrorCode.None, "攻撃予告を表示しました。", _elapsed);
    }

    private void Record(
        in StateContext context,
        in LifecycleInvocation invocation,
        DiagnosticSeverity severity,
        ErrorCode errorCode,
        string message,
        float realDuration)
    {
        ObservationEvent observation = new(
            context.EntityId,
            context.BossType,
            invocation.State,
            context.Phase,
            invocation.Operation,
            context.FrameUpdateNumber,
            context.DeltaTime,
            _definition.Skill,
            AttackStage.Telegraph,
            new TransitionReason(ReasonKind.AttackCompleted, skill: _definition.Skill),
            severity,

            errorCode,
            message,
            configuredDuration: _definition.TelegraphDuration,
            realDuration: realDuration,
            targetEntityId: _target.EntityId,
            attackRange: _definition.Range,
            attackStartTime: _startTime);
        try
        {
            context.Observation.Record(in observation);
        }
        catch
        {
            // 観測失敗だけを理由に Telegraph の正常処理を停止しません。
        }
    }
}
