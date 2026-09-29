using AnimalBattle.Configs;

namespace AnimalBattle.Maps;

/// <summary>
///     スプライトシートテクスチャからタイル UV 切り出しおよび単一タイルの描画を提供するタイルセットクラス。
///     すべてのパラメータを外部設定または明示的な引数から受け取り、内部でデフォルト値を持ちません。
/// </summary>
/// <param name="graphHandle">テクスチャ画像ハンドル</param>
/// <param name="tileWidth">タイル単体の幅（ピクセル）</param>
/// <param name="tileHeight">タイル単体の高さ（ピクセル）</param>
/// <param name="columns">テクスチャシート横方向タイル列数</param>
/// <param name="rows">テクスチャシート縦方向タイル行数</param>
/// <param name="leftBlock">左端ブロックのタイル ID</param>
/// <param name="middleBlock">中間ブロックのタイル ID</param>
/// <param name="rightBlock">右端ブロックのタイル ID</param>
/// <param name="collisionRect">プラットフォーム衝突領域矩形</param>
public sealed class TileSet(
    int graphHandle,
    int tileWidth,
    int tileHeight,
    int columns,
    int rows,
    int leftBlock,
    int middleBlock,
    int rightBlock,
    Rect collisionRect) {
    /// <summary>
    ///     <see cref="TileSetConfig" /> 設定データおよびテクスチャハンドルから <see cref="TileSet" /> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="graphHandle">テクスチャ画像ハンドル</param>
    /// <param name="config">タイルセット設定データ</param>
    public TileSet(int graphHandle, TileSetConfig config)
        : this(
            graphHandle,
            config.TileWidth,
            config.TileHeight,
            config.Columns,
            config.Rows,
            config.LeftBlock,
            config.MiddleBlock,
            config.RightBlock,
            new Rect(config.CollisionX1, config.CollisionY1, config.CollisionX2, config.CollisionY2))
    {
    }

    /// <summary>Native テクスチャグラフィックハンドル</summary>
    public int GraphHandle { get; } = graphHandle;

    /// <summary>タイル単体の幅（ピクセル）</summary>
    public int TileWidth { get; } = tileWidth;

    /// <summary>タイル単体の高さ（ピクセル）</summary>
    public int TileHeight { get; } = tileHeight;

    /// <summary>テクスチャシート内の横方向タイル列数</summary>
    public int Columns { get; } = columns;

    /// <summary>テクスチャシート内の縦方向タイル行数</summary>
    public int Rows { get; } = rows;

    /// <summary>左端ブロックのタイル ID (1-based)</summary>
    public int LeftBlock { get; } = leftBlock;

    /// <summary>中間ブロックのタイル ID (1-based)</summary>
    public int MiddleBlock { get; } = middleBlock;

    /// <summary>右端ブロックのタイル ID (1-based)</summary>
    public int RightBlock { get; } = rightBlock;

    /// <summary>プラットフォームタイルの衝突判定領域矩形</summary>
    public Rect CollisionRect { get; } = collisionRect;

    /// <summary>
    ///     1-based のタイル ID からスプライトシート上の UV 切り出し矩形 (<see cref="Rect" />) を取得します。
    ///     ID が 0 以下の場合は空の矩形を返します。
    /// </summary>
    /// <param name="tileId">1-based タイル ID (0 は空白/空気)</param>
    /// <returns>スプライトシート切り出し領域</returns>
    public Rect GetSourceRect(int tileId)
    {
        if (tileId <= 0 || Columns <= 0 || Rows <= 0) return Rect.Zero;

        var index = tileId - 1;
        var col = index % Columns;
        var row = index / Columns;

        var x1 = col * TileWidth;
        var y1 = row * TileHeight;
        var x2 = x1 + TileWidth;
        var y2 = y1 + TileHeight;

        return new Rect(x1, y1, x2, y2);
    }

    /// <summary>
    ///     指定された中心座標に単一タイルを描画します。
    /// </summary>
    /// <param name="tileId">1-based タイル ID</param>
    /// <param name="centerX">描画中心 X 座標</param>
    /// <param name="centerY">描画中心 Y 座標</param>
    /// <param name="scale">拡大縮小率</param>
    /// <param name="angle">回転角度（ラジアン）</param>
    /// <param name="flipX">左右反転フラグ</param>
    public void DrawTile(int tileId, int centerX, int centerY, double scale, double angle, bool flipX)
    {
        if (tileId <= 0 || GraphHandle < 0) return;

        var src = GetSourceRect(tileId);
        if (src.Width <= 0 || src.Height <= 0) return;

        GameEngine.DrawRectRotaGraph(
            centerX,
            centerY,
            src.X1,
            src.Y1,
            src.Width,
            src.Height,
            scale,
            angle,
            GraphHandle,
            true,
            flipX);
    }
}