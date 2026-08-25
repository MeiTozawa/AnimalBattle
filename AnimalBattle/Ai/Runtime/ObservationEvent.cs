using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Runtime;

/// <summary>
/// State_Module の観測を bounded buffer へ渡す値型イベントです。
/// </summary>
/// <remarks>観測イベントを生成します。</remarks>
public readonly struct ObservationEvent(
    ulong entityId,
    BossType bossType,
    StateId stateId,
    Phase phase,
    LifecycleOperation operation,
    long frameUpdateNumber,
    float deltaTime,
    AttackId attackId = default,
    AttackStage attackStage = default,
    TransitionReason transitionReason = default,
    DiagnosticSeverity severity = DiagnosticSeverity.Information,
    ErrorCode errorCode = ErrorCode.None,
    string message = "",
    StateId previousStateId = default,
    StateId nextStateId = default,
    float configuredDuration = 0,
    float realDuration = 0,
    ulong targetEntityId = 0,
    float attackRange = 0,
    float attackStartTime = 0)
{
    /// <summary>Boss Entity の識別子を取得します。</summary>
    public ulong EntityId { get; } = entityId;
    /// <summary>Boss 種別を取得します。</summary>
    public BossType BossType { get; } = bossType;
    /// <summary>状態識別子を取得します。</summary>
    public StateId StateId { get; } = stateId;
    /// <summary>遷移前の状態識別子を取得します。</summary>
    public StateId PreviousStateId { get; } = previousStateId;
    /// <summary>遷移先の状態識別子を取得します。</summary>
    public StateId NextStateId { get; } = nextStateId;
    /// <summary>親 Phase を取得します。</summary>
    public Phase Phase { get; } = phase;
    /// <summary>処理種別を取得します。</summary>
    public LifecycleOperation Operation { get; } = operation;
    /// <summary>イベントの重大度を取得します。</summary>
    public DiagnosticSeverity Severity { get; } = severity;
    /// <summary>エラー分類を取得します。</summary>
    public ErrorCode ErrorCode { get; } = errorCode;
    /// <summary>簡潔な日本語メッセージを取得します。</summary>
    public string Message { get; } = message;
    /// <summary>Frame_Update 番号を取得します。</summary>
    public long FrameUpdateNumber { get; } = frameUpdateNumber;
    /// <summary>経過時間を取得します。</summary>
    public float DeltaTime { get; } = deltaTime;
    /// <summary>攻撃識別子を取得します。</summary>
    public AttackId AttackId { get; } = attackId;
    /// <summary>攻撃段階を取得します。</summary>
    public AttackStage AttackStage { get; } = attackStage;
    /// <summary>遷移理由を取得します。</summary>
    public TransitionReason TransitionReason { get; } = transitionReason;
    /// <summary>設定上の継続時間を取得します。</summary>
    public float ConfiguredDuration { get; } = configuredDuration;
    /// <summary>実際の継続時間を取得します。</summary>
    public float RealDuration { get; } = realDuration;
    /// <summary>対象 Entity の識別子を取得します。</summary>
    public ulong TargetEntityId { get; } = targetEntityId;
    /// <summary>攻撃範囲を取得します。</summary>
    public float AttackRange { get; } = attackRange;
    /// <summary>攻撃開始時点の実時間を取得します。</summary>
    public float AttackStartTime { get; } = attackStartTime;
}
