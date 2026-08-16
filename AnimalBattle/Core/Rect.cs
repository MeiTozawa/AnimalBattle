using System;

namespace AnimalBattle.Core;
/// <summary>
/// 2次元の矩形領域（左、上、右、下 / X1, Y1, X2, Y2）を表す汎用構造体。
/// </summary>
/// <typeparam name="T">座標成分の数値型（int, float, double など）</typeparam>
public struct Rect<T> where T : struct
{
    /// <summary>
    /// 矩形の左端 X 座標を取得または設定します。
    /// </summary>
    public T Left { get; set; }

    /// <summary>
    /// 矩形の上端 Y 座標を取得または設定します。
    /// </summary>
    public T Top { get; set; }

    /// <summary>
    /// 矩形の右端 X 座標を取得または設定します。
    /// </summary>
    public T Right { get; set; }

    /// <summary>
    /// 矩形の下端 Y 座標を取得または設定します。
    /// </summary>
    public T Bottom { get; set; }

    /// <summary>
    /// 左端 X 座標のエイリアスプロパティ。
    /// </summary>
    public T X1
    {
        readonly get => Left;
        set => Left = value;
    }

    /// <summary>
    /// 上端 Y 座標のエイリアスプロパティ。
    /// </summary>
    public T Y1
    {
        readonly get => Top;
        set => Top = value;
    }

    /// <summary>
    /// 右端 X 座標のエイリアスプロパティ。
    /// </summary>
    public T X2
    {
        readonly get => Right;
        set => Right = value;
    }

    /// <summary>
    /// 下端 Y 座標のエイリアスプロパティ。
    /// </summary>
    public T Y2
    {
        readonly get => Bottom;
        set => Bottom = value;
    }

    /// <summary>
    /// 矩形の幅 (Right - Left) を取得します。
    /// </summary>
    public readonly T Width
    {
        get
        {
            if (typeof(T) == typeof(int))
            {
                return (T)(object)(Convert.ToInt32(Right) - Convert.ToInt32(Left));
            }
            if (typeof(T) == typeof(float))
            {
                return (T)(object)(Convert.ToSingle(Right) - Convert.ToSingle(Left));
            }
            return Right;
        }
    }

    /// <summary>
    /// 矩形の高さ (Bottom - Top) を取得します。
    /// </summary>
    public readonly T Height
    {
        get
        {
            if (typeof(T) == typeof(int))
            {
                return (T)(object)(Convert.ToInt32(Bottom) - Convert.ToInt32(Top));
            }
            if (typeof(T) == typeof(float))
            {
                return (T)(object)(Convert.ToSingle(Bottom) - Convert.ToSingle(Top));
            }
            return Bottom;
        }
    }

    /// <summary>
    /// 指定された座標 (x, y) が矩形領域内に含まれるか判定します。
    /// </summary>
    public readonly bool Contains(T x, T y)
    {
        float fx = Convert.ToSingle(x);
        float fy = Convert.ToSingle(y);
        return fx >= Convert.ToSingle(Left) && fx <= Convert.ToSingle(Right) &&
               fy >= Convert.ToSingle(Top) && fy <= Convert.ToSingle(Bottom);
    }

    /// <summary>
    /// 指定されたポイント座標が矩形領域内に含まれるか判定します。
    /// </summary>
    public readonly bool Contains(Point<T> point)
    {
        return Contains(point.X, point.Y);
    }

    /// <summary>
    /// 指定された別の矩形と交差（オーバーラップ）しているか判定します。
    /// </summary>
    /// <param name="other">判定対象の矩形</param>
    /// <returns>交差している場合は true</returns>
    public readonly bool Intersects(Rect<float> other)
    {
        return Convert.ToSingle(Left) < other.Right &&
               Convert.ToSingle(Right) > other.Left &&
               Convert.ToSingle(Top) < other.Bottom &&
               Convert.ToSingle(Bottom) > other.Top;
    }

    /// <summary>
    /// 左、上、右、下の各境界座標を指定して <see cref="Rect{T}"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="left">左端 X 座標</param>
    /// <param name="top">上端 Y 座標</param>
    /// <param name="right">右端 X 座標</param>
    /// <param name="bottom">下端 Y 座標</param>
    public Rect(T left, T top, T right, T bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }
}
