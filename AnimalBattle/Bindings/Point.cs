using System;
using System.Runtime.InteropServices;

namespace AnimalBattle.Bindings;

/// <summary>
/// 2次元座標を表す汎用構造体。
/// </summary>
/// <typeparam name="T">座標値の数値型</typeparam>
[StructLayout(LayoutKind.Sequential)]
public struct Point<T> where T : struct
{
    /// <summary>
    /// X 座標を取得または設定します。
    /// </summary>
    public T X;

    /// <summary>
    /// Y 座標を取得または設定します。
    /// </summary>
    public T Y;

    /// <summary>
    /// 指定された座標値で新しい <see cref="Point{T}"/> 構造体を初期化します。
    /// </summary>
    /// <param name="x">X 座標</param>
    /// <param name="y">Y 座標</param>
    public Point(T x, T y)
    {
        X = x;
        Y = y;
    }
}
