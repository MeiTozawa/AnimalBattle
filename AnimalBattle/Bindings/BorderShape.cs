using System;

namespace AnimalBattle.Bindings;

/// <summary>
/// テキスト枠線の形状タイプを表す列挙型。
/// </summary>
public enum BorderShape
{
    /// <summary>
    /// 枠線なし。
    /// </summary>
    Null,

    /// <summary>
    /// 単一矩形枠線。
    /// </summary>
    Rectangle,

    /// <summary>
    /// 二重矩形枠線。
    /// </summary>
    DoubleRectangle,

    /// <summary>
    /// カプセル（ピル）型枠線。
    /// </summary>
    Pill,

    /// <summary>
    /// 下線。
    /// </summary>
    Underline,
}
