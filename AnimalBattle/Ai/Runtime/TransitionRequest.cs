using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Runtime;

/// <summary>
/// 一回の状態遷移要求と完全な遷移理由 payload を表します。
/// </summary>
public readonly record struct TransitionRequest
{
    /// <summary>現在の State_Id を取得します。</summary>
    public StateId FromStateId { get; }

    /// <summary>遷移先の State_Id を取得します。</summary>
    public StateId ToStateId { get; }

    /// <summary>遷移理由の全 payload を取得します。</summary>
    public TransitionReason Reason { get; }

    /// <summary>遷移時点の Phase を取得します。</summary>
    public Phase Phase { get; }

    /// <summary>対象 Entity の識別子を取得します。</summary>
    public ulong EntityId { get; }

    /// <summary>遷移を要求した Frame_Update 番号を取得します。</summary>
    public long FrameUpdateNumber { get; }

    /// <summary>拒否時に試す安全な復帰先を取得します。</summary>
    public StateId SafeRecoveryStateId { get; }

    /// <summary>要求値が最低限の形式を満たすかどうかを取得します。</summary>
    public bool IsValid => FromStateId.IsValid
        && ToStateId.IsValid
        && EntityId != 0
        && FrameUpdateNumber >= 0
        && Reason.HasValidThreshold
        && Phase != Phase.Unknown;

    /// <summary>遷移要求を生成します。</summary>
    public TransitionRequest(
        StateId fromStateId,
        StateId toStateId,
        TransitionReason reason,
        Phase phase,
        ulong entityId,
        long frameUpdateNumber,
        StateId safeRecoveryStateId = default)
    {
        FromStateId = fromStateId;
        ToStateId = toStateId;
        Reason = reason;
        Phase = phase;
        EntityId = entityId;
        FrameUpdateNumber = frameUpdateNumber;
        SafeRecoveryStateId = safeRecoveryStateId;
    }
}
