using System.Runtime.InteropServices;
using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Runtime;

/// <summary>Boss の移動状態を表すデータ構造体です。</summary>
/// <remarks>移動データを生成します。</remarks>
[StructLayout(LayoutKind.Sequential)]
public struct BossMovementData(BossState stateId)
{
    /// <summary>移動先の位置です。</summary>
    public Vec2f TargetPosition = Vec2f.Zero;

    /// <summary>移動開始位置です。</summary>
    public Vec2f StartPosition = Vec2f.Zero;

    /// <summary>現在要求されている速度です。</summary>
    public Vec2f Velocity = Vec2f.Zero;

    /// <summary>現在の Movement State_Id です。</summary>
    public BossState BossState = stateId;

    /// <summary>向きが右かどうかです。</summary>
    public bool FacingRight = false;

    /// <summary>移動の残りクールダウンです。</summary>
    public float Cooldown = 0.0f;

    /// <summary>接地しているかどうかです。</summary>
    public bool IsGrounded = false;

    /// <summary>空中にいるかどうかです。</summary>
    public bool IsAirborne = false;
}
