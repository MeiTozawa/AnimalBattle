using System.Runtime.InteropServices;

namespace AnimalBattle.Components;

/// <summary>
/// 衝突判定用バウンディングボックスおよび状態を表すコンポーネント。
/// </summary>
/// <remarks>
/// 指定されたサイズとオフセットで <see cref="CollisionBox"/> の新しいインスタンスを初期化します。
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct CollisionBox(float width, float height, float offsetX = 0.0f, float offsetY = 0.0f)
{
    /// <summary>バウンディングボックス幅</summary>
    public float Width = width;

    /// <summary>バウンディングボックス高さ</summary>
    public float Height = height;

    /// <summary>X 軸オフセット</summary>
    public float OffsetX = offsetX;

    /// <summary>Y 軸オフセット</summary>
    public float OffsetY = offsetY;

    /// <summary>接地フラグ</summary>
    public bool IsGrounded = false;

    /// <summary>壁接触フラグ</summary>
    public bool IsOnWall = false;

    /// <summary>
    /// <see cref="CollisionBox"/> を統一幾何形状 <see cref="CollisionShape"/> へ変換します。
    /// </summary>
    public readonly CollisionShape ToCollisionShape() => new()
    {
        Type = CollisionShapeType.Box,
        Width = Width,
        Height = Height,
        OffsetX = OffsetX,
        OffsetY = OffsetY
    };

    /// <summary>
    /// <see cref="CollisionBox"/> から <see cref="CollisionShape"/> への暗黙変換を提供します。
    /// </summary>
    public static implicit operator CollisionShape(in CollisionBox box) => box.ToCollisionShape();
}
