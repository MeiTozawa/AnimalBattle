using System;
using System.Runtime.InteropServices;

namespace AnimalBattle.Core.Physics;

/// <summary>
/// 2次元汎用ベクトル構造体（Native C++ とメモリレイアウト完全一致: X: 4byte, Y: 4byte）。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct Vector2D(float X, float Y)
{
    /// <summary>ゼロベクトル (0, 0)</summary>
    public static readonly Vector2D Zero = new(0.0f, 0.0f);

    /// <summary>ベクトル加算</summary>
    public static Vector2D operator +(Vector2D a, Vector2D b) => new(a.X + b.X, a.Y + b.Y);

    /// <summary>ベクトル減算</summary>
    public static Vector2D operator -(Vector2D a, Vector2D b) => new(a.X - b.X, a.Y - b.Y);

    /// <summary>スカラー乗算</summary>
    public static Vector2D operator *(Vector2D a, float scalar) => new(a.X * scalar, a.Y * scalar);

    /// <summary>スカラー除算</summary>
    public static Vector2D operator /(Vector2D a, float scalar) => new(a.X / scalar, a.Y / scalar);

    /// <summary>ベクトルの長さの 2 乗を算出します。</summary>
    public readonly float LengthSquared() => X * X + Y * Y;

    /// <summary>ベクトルの長さを算出します。</summary>
    public readonly float Length() => MathF.Sqrt(LengthSquared());

    /// <summary>正規化ベクトルを算出します。</summary>
    public readonly Vector2D Normalized()
    {
        float len = Length();
        return len > 0.00001f ? this / len : Zero;
    }
}

/// <summary>
/// 2次元円形幾何衝突形状（中心座標 + 半径）。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct Circle2D(Vector2D Center, float Radius)
{
    /// <summary>
    /// 中心座標 (X, Y) と半径から円形形状を生成します。
    /// </summary>
    public Circle2D(float centerX, float centerY, float radius)
        : this(new Vector2D(centerX, centerY), radius)
    {
    }
}

/// <summary>
/// 2次元軸平行矩形 (AABB) 衝突形状（中心座標 + サイズ）。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct Box2D(Vector2D Center, Vector2D Size)
{
    /// <summary>
    /// 中心座標 (X, Y) と幅・高さから矩形形状を生成します。
    /// </summary>
    public Box2D(float centerX, float centerY, float width, float height)
        : this(new Vector2D(centerX, centerY), new Vector2D(width, height))
    {
    }

    /// <summary>全幅（ピクセル）</summary>
    public float Width => Size.X;

    /// <summary>全高（ピクセル）</summary>
    public float Height => Size.Y;
}

/// <summary>
/// 2次元整数ベクトル構造体（Native C++ の Vec2i とメモリレイアウト完全一致: X: 4byte, Y: 4byte）。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct Vec2i(int X, int Y)
{
    /// <summary>ゼロベクトル (0, 0)</summary>
    public static readonly Vec2i Zero = new(0, 0);

    /// <summary>ベクトル加算</summary>
    public static Vec2i operator +(Vec2i a, Vec2i b) => new(a.X + b.X, a.Y + b.Y);

    /// <summary>ベクトル減算</summary>
    public static Vec2i operator -(Vec2i a, Vec2i b) => new(a.X - b.X, a.Y - b.Y);

    /// <summary>スカラー乗算</summary>
    public static Vec2i operator *(Vec2i a, int scalar) => new(a.X * scalar, a.Y * scalar);

    /// <summary>スカラー除算</summary>
    public static Vec2i operator /(Vec2i a, int scalar) => new(a.X / scalar, a.Y / scalar);

    /// <summary>Vector2D への変換</summary>
    public Vector2D ToVector2D() => new(X, Y);
}
