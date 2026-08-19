namespace AnimalBattle.Core;

/// <summary>
/// 2次元座標（X, Y）を表す汎用構造体。
/// </summary>
/// <typeparam name="T">座標成分の数値型（int, float, double など）</typeparam>
/// <remarks>
/// X 座標および Y 座標を指定して <see cref="Point{T}"/> の新しいインスタンスを初期化します。
/// </remarks>
/// <param name="x">X 座標</param>
/// <param name="y">Y 座標</param>
public struct Point<T>(T x, T y) where T : struct
{
    /// <summary>
    /// X 座標を取得または設定します。
    /// </summary>
    public T X { get; set; } = x;

    /// <summary>
    /// Y 座標を取得または設定します。
    /// </summary>
    public T Y { get; set; } = y;
}
