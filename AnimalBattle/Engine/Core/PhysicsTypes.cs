using System.Runtime.InteropServices;

namespace AnimalBattle.Core;

/// <summary>
///     2次元浮動小数点ベクトル構造体（Native C++ の Vec2f とメモリレイアウト完全一致: X: 4byte, Y: 4byte）。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct Vec2f(float X, float Y) {
    /// <summary>デフォルト初期化 (0, 0)</summary>
    public Vec2f() : this(0.0f, 0.0f) { }

    /// <summary>ゼロベクトル (0, 0)</summary>
    public static readonly Vec2f Zero = new(0.0f, 0.0f);

    /// <summary>単位ベクトル (1, 1)</summary>
    public static readonly Vec2f One = new(1.0f, 1.0f);

    /// <summary>ベクトル加算</summary>
    public static Vec2f operator +(Vec2f a, Vec2f b)
    {
        return new Vec2f(a.X + b.X, a.Y + b.Y);
    }

    /// <summary>ベクトル減算</summary>
    public static Vec2f operator -(Vec2f a, Vec2f b)
    {
        return new Vec2f(a.X - b.X, a.Y - b.Y);
    }

    /// <summary>単項マイナス（符号反転）</summary>
    public static Vec2f operator -(Vec2f a)
    {
        return new Vec2f(-a.X, -a.Y);
    }

    /// <summary>スカラー乗算</summary>
    public static Vec2f operator *(Vec2f a, float scalar)
    {
        return new Vec2f(a.X * scalar, a.Y * scalar);
    }

    /// <summary>スカラー除算</summary>
    public static Vec2f operator /(Vec2f a, float scalar)
    {
        return new Vec2f(a.X / scalar, a.Y / scalar);
    }

    /// <summary>ベクトルの長さの 2 乗を算出します。</summary>
    public readonly float LengthSquared()
    {
        return X * X + Y * Y;
    }

    /// <summary>ベクトルの長さを算出します。</summary>
    public readonly float Length()
    {
        return MathF.Sqrt(LengthSquared());
    }

    /// <summary>正規化ベクトルを算出します。</summary>
    public readonly Vec2f Normalized()
    {
        var len = Length();
        return len > 0.00001f ? this / len : Zero;
    }

    /// <summary>Vec2i への変換（整数切り捨て）</summary>
    public readonly Vec2i ToVec2i()
    {
        return new Vec2i((int)X, (int)Y);
    }

    /// <summary>Vec2i への明示的型変換（整数切り捨て）</summary>
    public static explicit operator Vec2i(Vec2f v) => v.ToVec2i();

    /// <summary>Vec2i への四捨五入変換</summary>
    public readonly Vec2i RoundToVec2i()
    {
        return new Vec2i((int)MathF.Round(X), (int)MathF.Round(Y));
    }
}

/// <summary>
///     2次元円形幾何衝突形状（中心座標 + 半径）。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct Circle2D(Vec2f Center, float Radius);

/// <summary>
///     2次元軸平行矩形 (AABB) 衝突形状（中心座標 + サイズ）。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct Box2D(Vec2f Center, Vec2f Size) {
    /// <summary>矩形の左端 X 座標</summary>
    public float Left => Center.X - Size.X * 0.5f;

    /// <summary>矩形の右端 X 座標</summary>
    public float Right => Center.X + Size.X * 0.5f;

    /// <summary>矩形の上端 Y 座標</summary>
    public float Top => Center.Y - Size.Y * 0.5f;

    /// <summary>矩形の下端 Y 座標</summary>
    public float Bottom => Center.Y + Size.Y * 0.5f;
}

/// <summary>
///     2次元整数ベクトル構造体（Native C++ の Vec2i とメモリレイアウト完全一致: X: 4byte, Y: 4byte）。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct Vec2i(int X, int Y) {
    /// <summary>デフォルト初期化 (0, 0)</summary>
    public Vec2i() : this(0, 0) { }

    /// <summary>ゼロベクトル (0, 0)</summary>
    public static readonly Vec2i Zero = new(0, 0);

    /// <summary>単位ベクトル (1, 1)</summary>
    public static readonly Vec2i One = new(1, 1);

    /// <summary>ベクトル加算</summary>
    public static Vec2i operator +(Vec2i a, Vec2i b)
    {
        return new Vec2i(a.X + b.X, a.Y + b.Y);
    }

    /// <summary>ベクトル減算</summary>
    public static Vec2i operator -(Vec2i a, Vec2i b)
    {
        return new Vec2i(a.X - b.X, a.Y - b.Y);
    }

    /// <summary>単項マイナス（符号反転）</summary>
    public static Vec2i operator -(Vec2i a)
    {
        return new Vec2i(-a.X, -a.Y);
    }

    /// <summary>スカラー乗算</summary>
    public static Vec2i operator *(Vec2i a, int scalar)
    {
        return new Vec2i(a.X * scalar, a.Y * scalar);
    }

    /// <summary>スカラー除算</summary>
    public static Vec2i operator /(Vec2i a, int scalar)
    {
        return new Vec2i(a.X / scalar, a.Y / scalar);
    }

    /// <summary>Vec2f への変換</summary>
    public Vec2f ToVec2f()
    {
        return new Vec2f(X, Y);
    }

    /// <summary>Vec2f への暗黙的型変換</summary>
    public static implicit operator Vec2f(Vec2i v) => v.ToVec2f();
}