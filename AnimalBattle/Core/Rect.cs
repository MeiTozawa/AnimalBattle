using System;
using System.Runtime.InteropServices;

namespace AnimalBattle.Core;

/// <summary>
/// 2次元の矩形領域（左、上、右、下 / X1, Y1, X2, Y2）を表す整数構造体。
/// </summary>
/// <remarks>
/// 左、上、右、下の各境界座標を指定して <see cref="Rect"/> の新しいインスタンスを初期化します。
/// </remarks>
/// <param name="left">左端 X 座標</param>
/// <param name="top">上端 Y 座標</param>
/// <param name="right">右端 X 座標</param>
/// <param name="bottom">下端 Y 座標</param>
[StructLayout(LayoutKind.Sequential)]
public struct Rect(int left, int top, int right, int bottom)
{
    /// <summary>
    /// 全成分が 0 の矩形定数。
    /// </summary>
    public static readonly Rect Zero = default;

    /// <summary>
    /// 矩形の左端 X 座標を取得または設定します。
    /// </summary>
    public int Left { get; set; } = left;

    /// <summary>
    /// 矩形の上端 Y 座標を取得または設定します。
    /// </summary>
    public int Top { get; set; } = top;

    /// <summary>
    /// 矩形の右端 X 座標を取得または設定します。
    /// </summary>
    public int Right { get; set; } = right;

    /// <summary>
    /// 矩形の下端 Y 座標を取得または設定します。
    /// </summary>
    public int Bottom { get; set; } = bottom;

    /// <summary>
    /// 左端 X 座標のエイリアスプロパティ。
    /// </summary>
    public int X1
    {
        readonly get => Left;
        set => Left = value;
    }

    /// <summary>
    /// 上端 Y 座標のエイリアスプロパティ。
    /// </summary>
    public int Y1
    {
        readonly get => Top;
        set => Top = value;
    }

    /// <summary>
    /// 右端 X 座標のエイリアスプロパティ。
    /// </summary>
    public int X2
    {
        readonly get => Right;
        set => Right = value;
    }

    /// <summary>
    /// 下端 Y 座標のエイリアスプロパティ。
    /// </summary>
    public int Y2
    {
        readonly get => Bottom;
        set => Bottom = value;
    }

    /// <summary>
    /// 矩形の幅 (Right - Left) を取得します。
    /// </summary>
    public readonly int Width => Right - Left;

    /// <summary>
    /// 矩形の高さ (Bottom - Top) を取得します。
    /// </summary>
    public readonly int Height => Bottom - Top;

    /// <summary>
    /// 指定された座標 (x, y) が矩形領域内に含まれるか判定します。
    /// </summary>
    public readonly bool Contains(int x, int y)
    {
        return x >= Left && x <= Right &&
               y >= Top && y <= Bottom;
    }

    /// <summary>
    /// 左上座標および幅・高さを指定して <see cref="Rect"/> を生成します。
    /// </summary>
    public static Rect FromWidthHeight(int left, int top, int width, int height)
    {
        return new Rect(left, top, left + width, top + height);
    }
}
