namespace AnimalBattle.Ai.Contracts;

/// <summary>State_Module のライフサイクル呼び出しに渡す不変メタデータです。</summary>
public readonly record struct LifecycleInvocation {
    /// <summary>ライフサイクル呼び出しのメタデータを生成します。</summary>
    public LifecycleInvocation(BossType bossType, ulong entityId, BossState state, LifecycleOperation operation,
        long frameUpdateNumber, float deltaTime, Phase phase)
    {
        BossType = bossType;
        EntityId = entityId;
        State = state;
        Operation = operation;
        FrameUpdateNumber = frameUpdateNumber;
        DeltaTime = deltaTime;
        Phase = phase;
    }

    /// <summary>呼び出し対象の Boss 種別です。</summary>
    public BossType BossType { get; }

    /// <summary>呼び出し対象の Entity 識別子です。</summary>
    public ulong EntityId { get; }

    /// <summary>呼び出し対象の固定 BossState です。</summary>
    public BossState State { get; }

    /// <summary>呼び出し操作種別です。</summary>

    public LifecycleOperation Operation { get; }

    /// <summary>Frame_Update 番号です。</summary>
    public long FrameUpdateNumber { get; }

    /// <summary>経過時間（秒）です。</summary>
    public float DeltaTime { get; }

    /// <summary>親 Phase です。</summary>
    public Phase Phase { get; }

    /// <summary>呼び出しメタデータが有効かどうかを取得します。</summary>
    public bool IsValid => EntityId != 0
     && State.IsValid()
     && FrameUpdateNumber >= 0
     && !float.IsNaN(DeltaTime)
     && !float.IsInfinity(DeltaTime)
     && DeltaTime >= 0;
}