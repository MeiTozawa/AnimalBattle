using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Runtime;

/// <summary>
/// State_Module の観測を bounded buffer へ渡す値型イベントです。
/// </summary>
public readonly struct ObservationEvent
{
    /// <summary>Boss Entity の識別子を取得します。</summary>
    public ulong EntityId { get; }
    /// <summary>Boss 種別を取得します。</summary>
    public BossType BossType { get; }
    /// <summary>状態識別子を取得します。</summary>
    public StateId StateId { get; }
    /// <summary>遷移前の状態識別子を取得します。</summary>
    public StateId PreviousStateId { get; }
    /// <summary>遷移先の状態識別子を取得します。</summary>
    public StateId NextStateId { get; }
    /// <summary>親 Phase を取得します。</summary>
    public Phase Phase { get; }
    /// <summary>処理種別を取得します。</summary>
    public LifecycleOperation Operation { get; }
    /// <summary>イベントの重大度を取得します。</summary>
    public DiagnosticSeverity Severity { get; }
    /// <summary>エラー分類を取得します。</summary>
    public ErrorCode ErrorCode { get; }
    /// <summary>簡潔な日本語メッセージを取得します。</summary>
    public string Message { get; }
    /// <summary>Frame_Update 番号を取得します。</summary>
    public long FrameUpdateNumber { get; }
    /// <summary>経過時間を取得します。</summary>
    public float DeltaTime { get; }
    /// <summary>攻撃識別子を取得します。</summary>
    public AttackId AttackId { get; }
    /// <summary>攻撃段階を取得します。</summary>
    public AttackStage AttackStage { get; }
    /// <summary>遷移理由を取得します。</summary>
    public TransitionReason TransitionReason { get; }
    /// <summary>設定上の継続時間を取得します。</summary>
    public float ConfiguredDuration { get; }
    /// <summary>実際の継続時間を取得します。</summary>
    public float RealDuration { get; }
    /// <summary>対象 Entity の識別子を取得します。</summary>
    public ulong TargetEntityId { get; }

    /// <summary>観測イベントを生成します。</summary>
    public ObservationEvent(
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
        ulong targetEntityId = 0)
    {
        EntityId = entityId;
        BossType = bossType;
        StateId = stateId;
        PreviousStateId = previousStateId;
        NextStateId = nextStateId;
        Phase = phase;
        Operation = operation;
        Severity = severity;
        ErrorCode = errorCode;
        Message = message;
        FrameUpdateNumber = frameUpdateNumber;
        DeltaTime = deltaTime;
        AttackId = attackId;
        AttackStage = attackStage;
        TransitionReason = transitionReason;
        ConfiguredDuration = configuredDuration;
        RealDuration = realDuration;
        TargetEntityId = targetEntityId;
    }
}
