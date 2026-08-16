namespace AnimalBattle.Core;

/// <summary>
/// 2次元浮動小数点ベクトル (Point&lt;float&gt; と相互変換可能な構造体)。
/// </summary>
/// <remarks>
/// 指定された X および Y 成分で <see cref="Vec2"/> の新しいインスタンスを初期化します。
/// </remarks>
public struct Vec2(float x, float y)
{
    /// <summary>X 座標成分</summary>
    public float X { get; set; } = x;

    /// <summary>Y 座標成分</summary>
    public float Y { get; set; } = y;
}