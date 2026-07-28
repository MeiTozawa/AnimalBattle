namespace AnimalBattle.Core;

/// <summary>
/// 2次元座標（X, Y）を表す汎用構造体。
/// </summary>
/// <typeparam name="T">座標成分の数値型（int, float, double など）</typeparam>
public struct Point<T> where T : struct
{
    /// <summary>
    /// X 座標を取得または設定します。
    /// </summary>
    public T X { get; set; }

    /// <summary>
    /// Y 座標を取得または設定します。
    /// </summary>
    public T Y { get; set; }

    /// <summary>
    /// X 座標および Y 座標を指定して <see cref="Point{T}"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="x">X 座標</param>
    /// <param name="y">Y 座標</param>
    public Point(T x, T y)
    {
        X = x;
        Y = y;
    }
}
