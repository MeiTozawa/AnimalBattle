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
    BossSkill skill = default,
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
    /// <summary>遷移前の固定 BossState です。</summary>
    public BossState PreviousState { get; } = previousState;
    /// <summary>遷移先の固定 BossState です。</summary>
    public BossState NextState { get; } = nextState;
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
    /// <summary>技能識別子です。</summary>
    public BossSkill Skill { get; } = skill;

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
