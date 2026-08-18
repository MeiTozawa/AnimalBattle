using System.Runtime.InteropServices;
using AnimalBattle.Core.Physics;

namespace AnimalBattle.Components;

/// <summary>
/// 2D トランスフォーム（位置・拡大率・回転）を表すコンポーネント。
/// Native C++ の ab::Transform とメモリレイアウトが完全一致（20 bytes）します。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Transform
{
    /// <summary>位置座標 (Vec2f)</summary>
    public Vector2D Position;

    /// <summary>拡大率 (Vec2f)</summary>
    public Vector2D Scale;

    /// <summary>回転角度（ラジアン）</summary>
    public float Rotation;

    /// <summary>X 座標への簡易アクセスプロパティ</summary>
    public float X
    {
        readonly get => Position.X;
        set => Position = new Vector2D(value, Position.Y);
    }

    /// <summary>Y 座標への簡易アクセスプロパティ</summary>
    public float Y
    {
        readonly get => Position.Y;
        set => Position = new Vector2D(Position.X, value);
    }

    /// <summary>X 軸拡大率への簡易アクセスプロパティ</summary>
    public float ScaleX
    {
        readonly get => Scale.X;
        set => Scale = new Vector2D(value, Scale.Y);
    }

    /// <summary>Y 軸拡大率への簡易アクセスプロパティ</summary>
    public float ScaleY
    {
        readonly get => Scale.Y;
        set => Scale = new Vector2D(Scale.X, value);
    }

    /// <summary>
    /// 指定された位置、拡大率、回転で <see cref="Transform"/> の新しいインスタンスを初期化します。
    /// </summary>
    public Transform(float x, float y, float scaleX = 1.0f, float scaleY = 1.0f, float rotation = 0.0f)
    {
        Position = new Vector2D(x, y);
        Scale = new Vector2D(scaleX, scaleY);
        Rotation = rotation;
    }

    /// <summary>
    /// 指定された位置ベクトル、拡大率ベクトル、回転で <see cref="Transform"/> の新しいインスタンスを初期化します。
    /// </summary>
    public Transform(Vector2D position, Vector2D scale = default, float rotation = 0.0f)
    {
        Position = position;
        Scale = scale == default ? new Vector2D(1.0f, 1.0f) : scale;
        Rotation = rotation;
    }
}
