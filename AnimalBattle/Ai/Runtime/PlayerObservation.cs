namespace AnimalBattle.Ai.Runtime;

/// <summary>
/// 一つの Frame_Update で取得したプレイヤー観測の値スナップショットを表します。
/// </summary>
/// <remarks>プレイヤー観測を生成します。</remarks>
public readonly struct PlayerObservation(
    ulong entityId,
    bool isAlive,
    float positionX,
    float positionY,
    float velocityX,
    float velocityY,
    long frameUpdateNumber)
{
    /// <summary>プレイヤー Entity の識別子を取得します。</summary>
    public ulong EntityId { get; } = entityId;

    /// <summary>プレイヤーが生存しているかどうかを取得します。</summary>
    public bool IsAlive { get; } = isAlive;

    /// <summary>プレイヤーの X 座標を取得します。</summary>
    public float PositionX { get; } = positionX;

    /// <summary>プレイヤーの Y 座標を取得します。</summary>
    public float PositionY { get; } = positionY;

    /// <summary>プレイヤーの X 方向速度を取得します。</summary>
    public float VelocityX { get; } = velocityX;

    /// <summary>プレイヤーの Y 方向速度を取得します。</summary>
    public float VelocityY { get; } = velocityY;

    /// <summary>観測を取得した Frame_Update 番号を取得します。</summary>
    public long FrameUpdateNumber { get; } = frameUpdateNumber;

    /// <summary>プレイヤー観測が有効な値かどうかを取得します。</summary>
    public bool IsValid => EntityId != 0
        && FrameUpdateNumber >= 0
        && IsFinite(PositionX)
        && IsFinite(PositionY)
        && IsFinite(VelocityX)
        && IsFinite(VelocityY);

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
