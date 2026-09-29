using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Runtime;

/// <summary>一回の状態遷移要求と完全な遷移理由 payload です。</summary>
public readonly record struct TransitionRequest {
    /// <summary>遷移要求を生成します。</summary>
    public TransitionRequest(BossState fromStateId, BossState toStateId, TransitionReason reason, Phase phase,
        ulong entityId, long frameUpdateNumber, BossState safeRecoveryStateId = default)
    {
        FromStateId = fromStateId;
        ToStateId = toStateId;
        Reason = reason;
        Phase = phase;
        EntityId = entityId;
        FrameUpdateNumber = frameUpdateNumber;
        SafeRecoveryStateId = safeRecoveryStateId;
    }

    /// <summary>現在の固定 BossState です。</summary>
    public BossState FromStateId { get; }

    /// <summary>遷移先の固定 BossState です。</summary>
    public BossState ToStateId { get; }

    /// <summary>遷移理由の全 payload です。</summary>
    public TransitionReason Reason { get; }

    /// <summary>遷移時点の Phase です。</summary>
    public Phase Phase { get; }

    /// <summary>対象 Entity の識別子です。</summary>
    public ulong EntityId { get; }

    /// <summary>要求した Frame_Update 番号です。</summary>
    public long FrameUpdateNumber { get; }

    /// <summary>拒否時に試す安全な復帰先です。</summary>
    public BossState SafeRecoveryStateId { get; }

    /// <summary>要求値が最低限の形式を満たすかどうかを取得します。</summary>
    public bool IsValid => FromStateId.IsValid()
     && ToStateId.IsValid()
     && EntityId != 0
     && FrameUpdateNumber >= 0
     && Reason.HasValidThreshold
     && Phase != Phase.Unknown;
}