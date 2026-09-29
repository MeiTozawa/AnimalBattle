using System.Drawing;

namespace AnimalBattle.Components;

/// <summary>
///     衝撃波やジャンプリングなどの拡散円環・円形視覚演出パラメータを保持するコンポーネント。
/// </summary>
/// <remarks>
///     指定された半径およびカラーで <see cref="RingEffect" /> の新しいインスタンスを初期化します。
/// </remarks>
/// <param name="startRadius">初期展開半径（ピクセル）</param>
/// <param name="endRadius">最終最大展開半径（ピクセル）</param>
/// <param name="color">描画カラー</param>
/// <param name="isFilled">塗りつぶし円として描画するかどうか</param>
public struct RingEffect(float startRadius, float endRadius, Color color, bool isFilled = false) {
    /// <summary>初期展開半径（ピクセル）</summary>
    public float StartRadius = startRadius;

    /// <summary>最終最大展開半径（ピクセル）</summary>
    public float EndRadius = endRadius;

    /// <summary>描画カラー</summary>
    public Color Color = color;

    /// <summary>塗りつぶし円として描画するかどうか</summary>
    public bool IsFilled = isFilled;
}