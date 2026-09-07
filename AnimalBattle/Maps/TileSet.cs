using System;
using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Maps;

/// <summary>
/// スプライトシートテクスチャからタイル UV 切り出しおよび単一タイルの描画を提供するタイルセットクラス。
/// すべてのパラメータを外部設定または明示的な引数から受け取り、内部でデフォルト値を持ちません。
/// </summary>
public sealed class TileSet
{
    private readonly int _graphHandle;
    private readonly int _tileWidth;
    private readonly int _tileHeight;
    private readonly int _columns;
    private readonly int _rows;
    private readonly int _leftBlock;
    private readonly int _middleBlock;
    private readonly int _rightBlock;
    private readonly Rect _collisionRect;

    /// <summary>Native テクスチャグラフィックハンドル</summary>
    public int GraphHandle => _graphHandle;

    /// <summary>タイル単体の幅（ピクセル）</summary>
    public int TileWidth => _tileWidth;

    /// <summary>タイル単体の高さ（ピクセル）</summary>
    public int TileHeight => _tileHeight;

    /// <summary>テクスチャシート内の横方向タイル列数</summary>
    public int Columns => _columns;

    /// <summary>テクスチャシート内の縦方向タイル行数</summary>
    public int Rows => _rows;

    /// <summary>左端ブロックのタイル ID (1-based)</summary>
    public int LeftBlock => _leftBlock;

    /// <summary>中間ブロックのタイル ID (1-based)</summary>
    public int MiddleBlock => _middleBlock;

    /// <summary>右端ブロックのタイル ID (1-based)</summary>
    public int RightBlock => _rightBlock;

    /// <summary>プラットフォームタイルの衝突判定領域矩形</summary>
    public Rect CollisionRect => _collisionRect;

    /// <summary>
    /// 指定されたパラメータで <see cref="TileSet"/> の新しいインスタンスを初期化します。
    /// デフォルト値を持たず、すべての引数を明示的に指定する必要があります。
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
    public TileSet(
        int graphHandle,
        int tileWidth,
        int tileHeight,
        int columns,
        int rows,
        int leftBlock,
        int middleBlock,
        int rightBlock,
        Rect collisionRect)
    {
        _graphHandle = graphHandle;
        _tileWidth = tileWidth;
        _tileHeight = tileHeight;
        _columns = columns;
        _rows = rows;
        _leftBlock = leftBlock;
        _middleBlock = middleBlock;
        _rightBlock = rightBlock;
        _collisionRect = collisionRect;
    }

    /// <summary>
    /// <see cref="TileSetConfig"/> 設定データおよびテクスチャハンドルから <see cref="TileSet"/> の新しいインスタンスを初期化します。
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

    /// <summary>
    /// 1-based のタイル ID からスプライトシート上の UV 切り出し矩形 (<see cref="Rect"/>) を取得します。
    /// ID が 0 以下の場合は空の矩形を返します。
    /// </summary>
    /// <param name="tileId">1-based タイル ID (0 は空白/空気)</param>
    /// <returns>スプライトシート切り出し領域</returns>
    public Rect GetSourceRect(int tileId)
    {
        if (tileId <= 0 || _columns <= 0 || _rows <= 0)
        {
            return Rect.Zero;
        }

        int index = tileId - 1;
        int col = index % _columns;
        int row = index / _columns;

        int x1 = col * _tileWidth;
        int y1 = row * _tileHeight;
        int x2 = x1 + _tileWidth;
        int y2 = y1 + _tileHeight;

        return new Rect(x1, y1, x2, y2);
    }

    /// <summary>
    /// 指定された中心座標に単一タイルを描画します。
    /// </summary>
    /// <param name="tileId">1-based タイル ID</param>
    /// <param name="centerX">描画中心 X 座標</param>
    /// <param name="centerY">描画中心 Y 座標</param>
    /// <param name="scale">拡大縮小率</param>
    /// <param name="angle">回転角度（ラジアン）</param>
    /// <param name="flipX">左右反転フラグ</param>
    public void DrawTile(int tileId, int centerX, int centerY, double scale, double angle, bool flipX)
    {
        if (tileId <= 0 || _graphHandle < 0)
        {
            return;
        }

        Rect src = GetSourceRect(tileId);
        if (src.Width <= 0 || src.Height <= 0)
        {
            return;
        }

        GameEngine.DrawRectRotaGraph(
            centerX,
            centerY,
            src.X1,
            src.Y1,
            src.Width,
            src.Height,
            scale,
            angle,
            _graphHandle,
            true,
            flipX);
    }
}
