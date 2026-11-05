namespace AnimalBattle.Features.Items.Components;

/// <summary>
///     ドロップアイテムの放物線放出・接地判定・浮遊動態を管理する物理コンポーネント。
/// </summary>
public struct ItemDropPhysics
{
    /// <summary>現在の移動速度ベクトル（px/s）。</summary>
    public Vec2f Velocity;

    /// <summary>足場または地面に接地済みかどうか。</summary>
    public bool IsGrounded;

    /// <summary>接地後の基準となる Y 座標。</summary>
    public float RestY;

    /// <summary>接地後の正弦波浮遊アニメーション用タイマー（秒）。</summary>
    public float FloatTimer;
}
