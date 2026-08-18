using AnimalBattle.Core;

namespace AnimalBattle.Components;

/// <summary>
/// 2D スプライト描画用データを保持する純粋コンポーネント。
/// </summary>
/// <remarks>
/// 指定された設定値で <see cref="Sprite"/> の新しいインスタンスを初期化します。
/// </remarks>
public struct Sprite(int graphHandle = -1, bool flipX = false, bool isVisible = true, Rect<int> srcRect = default)
{
    /// <summary>Native グラフィックハンドル</summary>
    public int GraphHandle = graphHandle;

    /// <summary>切り出し転送領域</summary>
    public Rect<int> SrcRect = srcRect;

    /// <summary>左右反転フラグ</summary>
    public bool FlipX = flipX;

    /// <summary>表示フラグ</summary>
    public bool IsVisible = isVisible;
}
