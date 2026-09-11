namespace AnimalBattle.Ai.Ports;

/// <summary>Boss2 の移動 command 適用前後で共有する純粋な運動データです。</summary>
/// <remarks>Boss2 の運動データを生成します。</remarks>
public struct Boss2MovementFrame(Vec2f position, Vec2f velocity, float width, float height, bool isGrounded)
{
    /// <summary>Boss の中心位置です。</summary>
    public Vec2f Position = position;
    /// <summary>Boss の速度です。</summary>
    public Vec2f Velocity = velocity;
    /// <summary>Boss の衝突幅です。</summary>
    public float Width = width;
    /// <summary>Boss の衝突高さです。</summary>
    public float Height = height;
    /// <summary>Boss が接地しているかどうかです。</summary>
    public bool IsGrounded = isGrounded;
}

/// <summary>Boss2 の navigation 計画に必要な純粋な観測値です。</summary>
/// <remarks>Boss2 navigation の入力値を生成します。</remarks>
public readonly struct Boss2NavigationRequest(
    float currentX,
    float currentFootY,
    float targetX,
    float targetFootY,
    bool isGrounded,
    float moveSpeed)
{
    /// <summary>Boss の現在 X 座標です。</summary>
    public float CurrentX { get; } = currentX;
    /// <summary>Boss の現在の足元 Y 座標です。</summary>
    public float CurrentFootY { get; } = currentFootY;
    /// <summary>プレイヤーの目標 X 座標です。</summary>
    public float TargetX { get; } = targetX;
    /// <summary>プレイヤーの足元 Y 座標です。</summary>
    public float TargetFootY { get; } = targetFootY;
    /// <summary>Boss が接地しているかどうかです。</summary>
    public bool IsGrounded { get; } = isGrounded;
    /// <summary>使用する地上移動速度です。</summary>
    public float MoveSpeed { get; } = moveSpeed;
}
