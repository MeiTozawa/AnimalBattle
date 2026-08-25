using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Runtime;

/// <summary>State_Module の観測を bounded buffer へ渡す値型イベントです。</summary>
public readonly struct ObservationEvent(
    ulong entityId,
    BossType bossType,
    BossState state,
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
    BossState previousState = default,
    BossState nextState = default,
    float configuredDuration = 0,
    float realDuration = 0,
    ulong targetEntityId = 0,
    float attackRange = 0,
    float attackStartTime = 0)
{
    /// <summary>Boss Entity の識別子です。</summary>
    public ulong EntityId { get; } = entityId;
    /// <summary>Boss 種別です。</summary>
    public BossType BossType { get; } = bossType;
    /// <summary>固定 BossState です。</summary>
    public BossState State { get; } = state;
    /// <summary>旧プロパティ名です。値の型は固定 BossState です。</summary>
    [Obsolete("State を使用してください。")]
    public BossState StateId => State;
    /// <summary>互換用の固定 BossState 名です。</summary>
    [Obsolete("State を使用してください。")]
    public BossState BossState => State;
    public BossState PreviousState { get; } = previousState;
    /// <summary>遷移先の固定 BossState です。</summary>
    public BossState NextState { get; } = nextState;
    /// <summary>互換用の遷移前状態名です。</summary>
    [Obsolete("PreviousState を使用してください。")]
    public BossState PreviousBossState => PreviousState;
    /// <summary>互換用の遷移先状態名です。</summary>
    [Obsolete("NextState を使用してください。")]
    public BossState NextBossState => NextState;
    [Obsolete("PreviousState を使用してください。")]
    public BossState PreviousStateId => PreviousState;
    /// <summary>旧遷移先プロパティ名です。</summary>
    [Obsolete("NextState を使用してください。")]
    public BossState NextStateId => NextState;
    /// <summary>親 Phase です。</summary>
    public Phase Phase { get; } = phase;
    /// <summary>処理種別です。</summary>
    public LifecycleOperation Operation { get; } = operation;
    /// <summary>イベント重大度です。</summary>
    public DiagnosticSeverity Severity { get; } = severity;
    /// <summary>エラー分類です。</summary>
    public ErrorCode ErrorCode { get; } = errorCode;
    /// <summary>診断メッセージです。</summary>
    public string Message { get; } = message;
    /// <summary>Frame_Update 番号です。</summary>
    public long FrameUpdateNumber { get; } = frameUpdateNumber;
    /// <summary>経過時間です。</summary>
    public float DeltaTime { get; } = deltaTime;
    /// <summary>攻撃識別子です。</summary>
    public AttackId AttackId { get; } = attackId;
    /// <summary>攻撃段階です。</summary>
    public AttackStage AttackStage { get; } = attackStage;
    /// <summary>遷移理由です。</summary>
    public TransitionReason TransitionReason { get; } = transitionReason;
    /// <summary>設定上の継続時間です。</summary>
    public float ConfiguredDuration { get; } = configuredDuration;
    /// <summary>実際の継続時間です。</summary>
    public float RealDuration { get; } = realDuration;
    /// <summary>対象 Entity の識別子です。</summary>
    public ulong TargetEntityId { get; } = targetEntityId;
    /// <summary>攻撃範囲です。</summary>
    public float AttackRange { get; } = attackRange;
    /// <summary>攻撃開始時点の実時間です。</summary>
    public float AttackStartTime { get; } = attackStartTime;
}
