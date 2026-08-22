namespace AnimalBattle.Ai.Runtime;

/// <summary>
/// 一つの Frame_Update で取得したプレイヤー観測の値スナップショットを表します。
/// </summary>
public readonly struct PlayerObservation
{
    /// <summary>プレイヤー Entity の識別子を取得します。</summary>
    public ulong EntityId { get; }

    /// <summary>プレイヤーが生存しているかどうかを取得します。</summary>
    public bool IsAlive { get; }

    /// <summary>プレイヤーの X 座標を取得します。</summary>
    public float PositionX { get; }

    /// <summary>プレイヤーの Y 座標を取得します。</summary>
    public float PositionY { get; }

    /// <summary>プレイヤーの X 方向速度を取得します。</summary>
    public float VelocityX { get; }

    /// <summary>プレイヤーの Y 方向速度を取得します。</summary>
    public float VelocityY { get; }

    /// <summary>観測を取得した Frame_Update 番号を取得します。</summary>
    public long FrameUpdateNumber { get; }

    /// <summary>プレイヤー観測が有効な値かどうかを取得します。</summary>
    public bool IsValid => EntityId != 0
        && FrameUpdateNumber >= 0
        && IsFinite(PositionX)
        && IsFinite(PositionY)
        && IsFinite(VelocityX)
        && IsFinite(VelocityY);

    /// <summary>プレイヤー観測を生成します。</summary>
    public PlayerObservation(
        ulong entityId,
        bool isAlive,
        float positionX,
        float positionY,
        float velocityX,
        float velocityY,
        long frameUpdateNumber)
    {
        EntityId = entityId;
        IsAlive = isAlive;
        PositionX = positionX;
        PositionY = positionY;
        VelocityX = velocityX;
        VelocityY = velocityY;
        FrameUpdateNumber = frameUpdateNumber;
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
