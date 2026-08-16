using AnimalBattle.Core;

namespace AnimalBattle.Components;

/// <summary>
/// 2D スプライト描画用データを保持する純粋コンポーネント。
/// </summary>
public struct Sprite
{
    /// <summary>Native グラフィックハンドル</summary>
    public int GraphHandle;

    /// <summary>切り出し転送領域</summary>
    public Rect<int> SrcRect;

    /// <summary>左右反転フラグ</summary>
    public bool FlipX;

    /// <summary>表示フラグ</summary>
    public bool IsVisible;

    /// <summary>
    /// 指定された設定値で <see cref="Sprite"/> の新しいインスタンスを初期化します。
    /// </summary>
    public Sprite(int graphHandle = -1, bool flipX = false, bool isVisible = true, Rect<int> srcRect = default)
    {
        GraphHandle = graphHandle;
        SrcRect = srcRect;
        FlipX = flipX;
        IsVisible = isVisible;
    }
}
