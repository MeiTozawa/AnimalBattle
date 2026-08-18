using System.Runtime.InteropServices;

namespace AnimalBattle.Components;

/// <summary>
/// 衝突判定用バウンディングボックスおよび状態を表すコンポーネント。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct CollisionBox
{
    /// <summary>バウンディングボックス幅</summary>
    public float Width;

    /// <summary>バウンディングボックス高さ</summary>
    public float Height;

    /// <summary>X 軸オフセット</summary>
    public float OffsetX;

    /// <summary>Y 軸オフセット</summary>
    public float OffsetY;

    /// <summary>接地フラグ</summary>
    public bool IsGrounded;

    /// <summary>壁接触フラグ</summary>
    public bool IsOnWall;

    /// <summary>
    /// 指定されたサイズとオフセットで <see cref="CollisionBox"/> の新しいインスタンスを初期化します。
    /// </summary>
    public CollisionBox(float width, float height, float offsetX = 0.0f, float offsetY = 0.0f)
    {
        Width = width;
        Height = height;
        OffsetX = offsetX;
        OffsetY = offsetY;
        IsGrounded = false;
        IsOnWall = false;
    }
}
