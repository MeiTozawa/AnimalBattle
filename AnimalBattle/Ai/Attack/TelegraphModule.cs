using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using ErrorOr;

namespace AnimalBattle.Ai.Attack;

/// <summary>攻撃対象と範囲を表示・観測する共通 Telegraph State_Module です。</summary>
public sealed class TelegraphModule : IBossStateModule {
    private PlayerObservation _target;

    /// <summary>攻撃定義と State_Id を指定して Telegraph を生成します。</summary>
    public TelegraphModule(BossState stateId, AttackDefinition definition)
    {
        if (!stateId.IsValid) throw new ArgumentOutOfRangeException(nameof(stateId));
        ArgumentNullException.ThrowIfNull(definition);
        HandledState = stateId;
        Definition = definition;
    }

    /// <summary>現在の技能識別子を取得します。</summary>
    public BossSkill Skill => Definition.Skill;

    /// <summary>攻撃定義を取得します。</summary>
    public AttackDefinition Definition { get; private set; }

    private float AttackStartTime { get; set; }

    /// <summary>Telegraph が現在アクティブかどうかを取得します。</summary>
    public bool IsActive { get; private set; }

    /// <summary>Telegraph 終了条件を満たしたかどうかを取得します。</summary>
    public bool IsComplete => Elapsed >= Definition.TelegraphDuration;

    /// <summary>Telegraph の経過時間を取得します。</summary>
    public float Elapsed { get; private set; }

    /// <summary>Telegraph の担当状態を取得します。</summary>
    public BossState HandledState { get; private set; }

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        Start(ref context, in invocation);
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (!IsActive) return;

        EmitTelegraph(ref context, in invocation);
        Elapsed = MathF.Min(Definition.TelegraphDuration, Elapsed + context.DeltaTime);
        if (Elapsed >= Definition.TelegraphDuration) IsActive = false;
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        IsActive = false;
    }

    /// <summary>
    ///     既存インスタンスを再利用するために、状態識別子と攻撃定義を就地リセットします。
    /// </summary>
    public void Reset(BossState stateId, AttackDefinition definition)
    {
        if (!stateId.IsValid) throw new ArgumentOutOfRangeException(nameof(stateId));
        ArgumentNullException.ThrowIfNull(definition);
        HandledState = stateId;
        Definition = definition;
        IsActive = false;
        Elapsed = 0;
        AttackStartTime = 0;
        _target = default;
    }

    /// <summary>Telegraph を開始し、開始時点の対象を固定します。</summary>
    public ErrorOr<Success> Start(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (IsActive) return Result.Success;

        if (!Definition.IsValid || !invocation.IsValid)
        {
            Record(in context, in invocation, DiagnosticSeverity.Warning, ErrorCode.InvalidConfiguration,
                "攻撃時間が不正なため Telegraph を開始せず、安全な非攻撃行動へ移行します。", 0);
            return Error.Validation("TelegraphModule.Start", "攻撃定義またはフレーム呼び出しが不正です。");
        }

        _target = context.PlayerObservation;
        Elapsed = 0;
        AttackStartTime = 0;
        IsActive = true;
        EmitTelegraph(ref context, in invocation);
        return Result.Success;
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
            Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed,
                "攻撃予告の表示に失敗しました。", Elapsed);
            return;
        }

        Record(in context, in invocation, DiagnosticSeverity.Information, ErrorCode.None, "攻撃予告を表示しました。", Elapsed);
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
            Definition.Skill,
            AttackStage.Telegraph,
            new TransitionReason(ReasonKind.AttackCompleted, skill: Definition.Skill),
            severity,
            errorCode,
            message,
            configuredDuration: Definition.TelegraphDuration,
            realDuration: realDuration,
            targetEntityId: _target.EntityId,
            attackRange: Definition.Range,
            attackStartTime: AttackStartTime);
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