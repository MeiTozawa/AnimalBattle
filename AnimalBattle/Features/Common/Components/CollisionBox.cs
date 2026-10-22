using System.Runtime.InteropServices;
using AnimalBattle.Core;
using AnimalBattle.Core.Geometry;

namespace AnimalBattle.Common.Components;

/// <summary>
///     衝突判定用バウンディングボックスおよび状態を表すコンポーネント。
/// </summary>
/// <remarks>
///     指定されたサイズとオフセットで <see cref="CollisionBox" /> の新しいインスタンスを初期化します。
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct CollisionBox(Vec2f size, Vec2f offset = default) {
    /// <summary>バウンディングボックスサイズ Vec2f型</summary>
    public Vec2f Size = size;

    /// <summary>局所オフセット Vec2f型</summary>
    public Vec2f Offset = offset;

    /// <summary>接地フラグ</summary>
    public bool IsGrounded = false;

    /// <summary>
    ///     <see cref="CollisionBox" /> を統一幾何形状 <see cref="CollisionShape" /> へ変換します。
    /// </summary>
    public readonly CollisionShape ToCollisionShape()
    {
        return new CollisionShape.Box(Size, Offset);
    }

    /// <summary>
    ///     <see cref="CollisionBox" /> から <see cref="CollisionShape" /> への暗黙変換を提供します。
    /// </summary>
    public static implicit operator CollisionShape(in CollisionBox box)
    {
        return box.ToCollisionShape();
    }
}