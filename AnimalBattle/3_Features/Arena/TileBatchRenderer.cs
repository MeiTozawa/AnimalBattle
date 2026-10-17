namespace AnimalBattle.Maps;

/// <summary>
///     タイルマップ描画時の視錐台（Viewport）カリングおよびバッチ処理ヘルパークラス。
///     画面外の不可視タイルを CPU 走査および描画コマンドバッファ圧入から除外します。
/// </summary>
public static class TileBatchRenderer
{
    /// <summary>
    ///     指定された視口（Viewport）矩形に基づいて、描画対象となるグリッド行列の最小・最大インデックスを計算します。
    /// </summary>
    /// <param name="map">対象のタイルマップ</param>
    /// <param name="viewportX">カメラ視口 左端 X 座標</param>
    /// <param name="viewportY">カメラ視口 上端 Y 座標</param>
    /// <param name="viewportWidth">カメラ視口 幅（ピクセル）</param>
    /// <param name="viewportHeight">カメラ視口 高さ（ピクセル）</param>
    /// <param name="minCol">可視列 最小インデックス (0-based)</param>
    /// <param name="maxCol">可視列 最大インデックス (0-based)</param>
    /// <param name="minRow">可視行 最小インデックス (0-based)</param>
    /// <param name="maxRow">可視行 最大インデックス (0-based)</param>
    public static void CalculateVisibleBounds(
        TileMap map,
        float viewportX,
        float viewportY,
        float viewportWidth,
        float viewportHeight,
        out int minCol,
        out int maxCol,
        out int minRow,
        out int maxRow)
    {
        if (map.Columns <= 0 || map.Rows <= 0 || map.RenderTileWidth <= 0 || map.RenderTileHeight <= 0)
        {
            minCol = maxCol = minRow = maxRow = 0;
            return;
        }

        var relLeft = viewportX - map.OriginX;
        var relTop = viewportY - map.OriginY;
        var relRight = relLeft + viewportWidth;
        var relBottom = relTop + viewportHeight;

        minCol = Math.Clamp((int)MathF.Floor(relLeft / map.RenderTileWidth), 0, map.Columns - 1);
        maxCol = Math.Clamp((int)MathF.Ceiling(relRight / map.RenderTileWidth), 0, map.Columns - 1);
        minRow = Math.Clamp((int)MathF.Floor(relTop / map.RenderTileHeight), 0, map.Rows - 1);
        maxRow = Math.Clamp((int)MathF.Ceiling(relBottom / map.RenderTileHeight), 0, map.Rows - 1);
    }

    /// <summary>
    ///     カメラ視口に基づいてカリングされた可視タイルのみを抽出して描画コマンドバッファへ投入します。
    /// </summary>
    /// <param name="map">対象のタイルマップ</param>
    /// <param name="viewX">視口 左端 X</param>
    /// <param name="viewY">視口 上端 Y</param>
    /// <param name="viewW">視口 幅</param>
    /// <param name="viewH">視口 高さ</param>
    public static void DrawCulled(TileMap map, float viewX, float viewY, float viewW, float viewH)
    {
        if (map.TileSet.TileWidth <= 0 || map.TileSet.TileHeight <= 0) return;

        CalculateVisibleBounds(map, viewX, viewY, viewW, viewH,
            out var minCol, out var maxCol, out var minRow, out var maxRow);

        var scaleX = (double)map.RenderTileWidth / map.TileSet.TileWidth;
        var scaleY = (double)map.RenderTileHeight / map.TileSet.TileHeight;
        var scale = (scaleX + scaleY) / 2.0;

        for (var r = minRow; r <= maxRow; r++)
        for (var c = minCol; c <= maxCol; c++)
        {
            var tileId = map.GetTile(c, r);
            if (tileId <= 0) continue;

            var centerX = (int)(map.OriginX + c * map.RenderTileWidth + map.RenderTileWidth / 2.0f);
            var centerY = (int)(map.OriginY + r * map.RenderTileHeight + map.RenderTileHeight / 2.0f);

            map.TileSet.DrawTile(tileId, centerX, centerY, scale, 0.0, false);
        }
    }
}
