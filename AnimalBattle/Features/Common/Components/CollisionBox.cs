using System.Runtime.InteropServices;

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
}
