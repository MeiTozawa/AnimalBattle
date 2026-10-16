using System.Drawing;

namespace AnimalBattle.Components;

/// <summary>
///     2D スプライト描画用データを保持する純粋コンポーネント。
/// </summary>
/// <remarks>
///     指定された設定値で <see cref="Sprite" /> の新しいインスタンスを初期化します。
/// </remarks>
public struct Sprite(
    int graphHandle = -1,
    bool flipX = false,
    bool isVisible = true,
    Rect srcRect = default,
    Color? color = null) {
    /// <summary>Native グラフィックハンドル</summary>
    public int GraphHandle = graphHandle;

    /// <summary>切り出し転送領域</summary>
    public Rect SrcRect = srcRect;

    /// <summary>左右反転フラグ</summary>
    public bool FlipX = flipX;

    /// <summary>表示フラグ</summary>
    public bool IsVisible = isVisible;

    /// <summary>スプライト描画色（色調補正 / Tint）</summary>
    public Color Color = color ?? Color.White;

    /// <summary>局所振動（ジッター）変位 X（ピクセル）</summary>
    public float JitterOffsetX = 0.0f;

    /// <summary>局所振動（ジッター）変位 Y（ピクセル）</summary>
    public float JitterOffsetY = 0.0f;

    /// <summary>スプリング拡縮変形倍率 X</summary>
    public float ScaleX = 1.0f;

    /// <summary>スプリング拡縮変形倍率 Y</summary>
    public float ScaleY = 1.0f;

    /// <summary>スプリング拡縮による接地位置補正 Y（ピクセル）</summary>
    public float SpringOffsetY = 0.0f;
}