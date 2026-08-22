namespace AnimalBattle.Ai.Contracts;

/// <summary>
/// State module の各ライフサイクル呼び出しに渡す不変メタデータを表します。
/// </summary>
public readonly record struct LifecycleInvocation
{
    /// <summary>
    /// 呼び出し対象の Boss 種別を取得します。
    /// </summary>
    public BossType BossType { get; }

    /// <summary>
    /// 呼び出し対象の Entity 識別子を取得します。
    /// </summary>
    public ulong EntityId { get; }

    /// <summary>
    /// 呼び出し対象の状態識別子を取得します。
    /// </summary>
    public StateId StateId { get; }

    /// <summary>
    /// 呼び出しのライフサイクル種別を取得します。
    /// </summary>
    public LifecycleOperation Operation { get; }

    /// <summary>
    /// フレーム更新番号を取得します。
    /// </summary>
    public long FrameUpdateNumber { get; }

    /// <summary>
    /// この呼び出しに渡された経過時間を秒単位で取得します。
    /// </summary>
    public float DeltaTime { get; }

    /// <summary>
    /// 呼び出し時点の親 Phase を取得します。
    /// </summary>
    public Phase Phase { get; }

    /// <summary>
    /// Entity、StateId、フレーム番号、経過時間が契約を満たすかどうかを取得します。
    /// </summary>
    public bool IsValid => EntityId != 0
        && StateId.IsValid
        && FrameUpdateNumber >= 0
        && !float.IsNaN(DeltaTime)
        && !float.IsInfinity(DeltaTime)
        && DeltaTime >= 0;

    /// <summary>
    /// ライフサイクル呼び出しのメタデータを生成します。
    /// </summary>
    /// <param name="bossType">呼び出し対象の Boss 種別。</param>
    /// <param name="entityId">呼び出し対象の Entity 識別子。</param>
    /// <param name="stateId">呼び出し対象の状態識別子。</param>
    /// <param name="operation">呼び出しのライフサイクル種別。</param>
    /// <param name="frameUpdateNumber">フレーム更新番号。</param>
    /// <param name="deltaTime">経過時間（秒）。0 以上である必要があります。</param>
    /// <param name="phase">呼び出し時点の親 Phase。</param>
    public LifecycleInvocation(
        BossType bossType,
        ulong entityId,
        StateId stateId,
        LifecycleOperation operation,
        long frameUpdateNumber,
        float deltaTime,
        Phase phase)
    {
        BossType = bossType;
        EntityId = entityId;
        StateId = stateId;
        Operation = operation;
        FrameUpdateNumber = frameUpdateNumber;
        DeltaTime = deltaTime;
        Phase = phase;
    }
}
