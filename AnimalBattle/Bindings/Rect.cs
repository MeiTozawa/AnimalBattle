using System;
using System.Runtime.InteropServices;

namespace AnimalBattle.Bindings;

/// <summary>
/// 2次元矩形領域を表す汎用構造体。
/// </summary>
/// <typeparam name="T">座標値の数値型</typeparam>
[StructLayout(LayoutKind.Sequential)]
public struct Rect<T> where T : struct
{
    /// <summary>
    /// 左端 X 座標を取得または設定します。
    /// </summary>
    public T X1;

    /// <summary>
    /// 上端 Y 座標を取得または設定します。
    /// </summary>
    public T Y1;

    /// <summary>
    /// 右端 X 座標を取得または設定します。
    /// </summary>
    public T X2;

    /// <summary>
    /// 下端 Y 座標を取得または設定します。
    /// </summary>
    public T Y2;

    /// <summary>
    /// 指定された対角座標で新しい <see cref="Rect{T}"/> 構造体を初期化します。
    /// </summary>
    /// <param name="x1">左端 X 座標</param>
    /// <param name="y1">上端 Y 座標</param>
    /// <param name="x2">右端 X 座標</param>
    /// <param name="y2">下端 Y 座標</param>
    public Rect(T x1, T y1, T x2, T y2)
    {
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
    }
}
