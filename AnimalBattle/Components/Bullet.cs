using System.Drawing;

namespace AnimalBattle.Components;

/// <summary>
///     弾の半径、描画発光カラー、生存期間、および跳ね返り挙動を定義するコンポーネント。
/// </summary>
/// <remarks>
///     <see cref="Bullet" /> の新しいインスタンスを初期化します。
/// </remarks>
/// <param name="radius">判定半径</param>
/// <param name="fillColor">塗りつぶし色</param>
/// <param name="outlineColor">発光枠色</param>
/// <param name="isBouncing">反射フラグ</param>
public struct Bullet(float radius, Color fillColor, Color outlineColor, bool isBouncing = false) {
    /// <summary>弾の当たり判定・描画半径（ピクセル）</summary>
    public float Radius = radius;

    /// <summary>弾内部の塗りつぶしカラー (<see cref="Color" />)</summary>
    public Color FillColor = fillColor;

    /// <summary>弾外周の発光アウトラインカラー (<see cref="Color" />)</summary>
    public Color OutlineColor = outlineColor;

    /// <summary>弾の生存時間タイマー（秒）</summary>
    public float Lifetime = 0.0f;

    /// <summary>画面端やプラットフォームで跳ね返るかどうかのフラグ</summary>
    public bool IsBouncing = isBouncing;
}