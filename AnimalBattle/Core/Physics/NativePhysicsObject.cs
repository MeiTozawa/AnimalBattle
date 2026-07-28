using System.Runtime.InteropServices;

namespace AnimalBattle.Core;

/// <summary>
/// Native C++ 物理オブジェクトのデータを保持する構造体。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativePhysicsObject
{
    /// <summary>オブジェクトの固有ID</summary>
    public int Id;

    /// <summary>位置 X 座標</summary>
    public float X;

    /// <summary>位置 Y 座標</summary>
    public float Y;

    /// <summary>操作移動速度 Vx</summary>
    public float Vx;

    /// <summary>操作移動速度 Vy</summary>
    public float Vy;

    /// <summary>ふっとび速度 KnockbackX</summary>
    public float KnockbackX;

    /// <summary>ふっとび速度 KnockbackY</summary>
    public float KnockbackY;

    /// <summary>当たり判定の幅</summary>
    public float Width;

    /// <summary>当たり判定の高さ</summary>
    public float Height;

    /// <summary>体重</summary>
    public float Weight;

    /// <summary>ダメージ蓄積率 (%)</summary>
    public float DamagePercent;

    /// <summary>ヒットスタン残り時間</summary>
    public float HitstunTimer;

    /// <summary>すり抜け無効化タイマー</summary>
    public float DropThroughTimer;

    /// <summary>現在の衝突フラグ</summary>
    public CollisionFlags CollisionFlags;
}
