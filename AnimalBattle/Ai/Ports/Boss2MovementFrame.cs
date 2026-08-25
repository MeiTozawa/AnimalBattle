using AnimalBattle.Core.Physics;

namespace AnimalBattle.Ai.Ports;

/// <summary>Boss2 の移動 command 適用前後で共有する純粋な運動データです。</summary>
public struct Boss2MovementFrame
{
    /// <summary>Boss の中心位置です。</summary>
    public Vec2f Position;
    /// <summary>Boss の速度です。</summary>
    public Vec2f Velocity;
    /// <summary>Boss の衝突幅です。</summary>
    public float Width;
    /// <summary>Boss の衝突高さです。</summary>
    public float Height;
    /// <summary>Boss が接地しているかどうかです。</summary>
    public bool IsGrounded;

    /// <summary>Boss2 の運動データを生成します。</summary>
    public Boss2MovementFrame(Vec2f position, Vec2f velocity, float width, float height, bool isGrounded)
    {
        Position = position;
        Velocity = velocity;
        Width = width;
        Height = height;
        IsGrounded = isGrounded;
    }
}

/// <summary>Boss2 の navigation 計画に必要な純粋な観測値です。</summary>
public readonly struct Boss2NavigationRequest
{
    /// <summary>Boss の現在 X 座標です。</summary>
    public float CurrentX { get; }
    /// <summary>Boss の現在の足元 Y 座標です。</summary>
    public float CurrentFootY { get; }
    /// <summary>プレイヤーの目標 X 座標です。</summary>
    public float TargetX { get; }
    /// <summary>プレイヤーの足元 Y 座標です。</summary>
    public float TargetFootY { get; }
    /// <summary>Boss が接地しているかどうかです。</summary>
    public bool IsGrounded { get; }
    /// <summary>使用する地上移動速度です。</summary>
    public float MoveSpeed { get; }

    /// <summary>Boss2 navigation の入力値を生成します。</summary>
    public Boss2NavigationRequest(
        float currentX,
        float currentFootY,
        float targetX,
        float targetFootY,
        bool isGrounded,
        float moveSpeed)
    {
        CurrentX = currentX;
        CurrentFootY = currentFootY;
        TargetX = targetX;
        TargetFootY = targetFootY;
        IsGrounded = isGrounded;
        MoveSpeed = moveSpeed;
    }
}
