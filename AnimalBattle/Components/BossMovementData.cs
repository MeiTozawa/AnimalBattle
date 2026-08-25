using System.Runtime.InteropServices;

using AnimalBattle.Ai.Contracts;
using AnimalBattle.Core.Physics;

namespace AnimalBattle.Components;

/// <summary>Boss の移動領域を表す純粋な ECS データです。</summary>
[StructLayout(LayoutKind.Sequential)]
public struct BossMovementData
{
    /// <summary>移動先の位置です。</summary>
    public Vec2f TargetPosition;
    /// <summary>移動開始位置です。</summary>
    public Vec2f StartPosition;
    /// <summary>現在要求されている速度です。</summary>
    public Vec2f Velocity;
    /// <summary>現在の Movement State_Id です。</summary>
    public BossState BossState;
    /// <summary>向きが右かどうかです。</summary>
    public bool FacingRight;
    /// <summary>移動の残りクールダウンです。</summary>
    public float Cooldown;
    /// <summary>接地しているかどうかです。</summary>
    public bool IsGrounded;
    /// <summary>空中にいるかどうかです。</summary>
    public bool IsAirborne;

    /// <summary>移動データを生成します。</summary>
    public BossMovementData(BossState stateId)
    {
        TargetPosition = Vec2f.Zero;
        StartPosition = Vec2f.Zero;
        Velocity = Vec2f.Zero;
        BossState = stateId;
        FacingRight = false;
        Cooldown = 0.0f;
        IsGrounded = false;
        IsAirborne = false;
    }
}