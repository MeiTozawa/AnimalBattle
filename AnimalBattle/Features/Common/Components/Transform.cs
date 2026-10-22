using System.Runtime.InteropServices;

namespace AnimalBattle.Common.Components;

/// <summary>
///     2D トランスフォーム：位置・拡大率・回転を表すコンポーネント。
///     Native C++ の ab::Transform とメモリレイアウトが完全一致：20 bytesします。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Transform {
    /// <summary>位置座標 Vec2f型</summary>
    public Vec2f Position;

    /// <summary>拡大率 Vec2f型</summary>
    public Vec2f Scale;

    /// <summary>回転角度ラジアン単位</summary>
    public float Rotation;

    /// <summary>
    ///     指定された位置ベクトル、拡大率ベクトル、回転で <see cref="Transform" /> の新しいインスタンスを初期化します。
    /// </summary>
    public Transform(Vec2f position, Vec2f scale = default, float rotation = 0.0f)
    {
        Position = position;
        Scale = scale == default ? new Vec2f(1.0f, 1.0f) : scale;
        Rotation = rotation;
    }
}