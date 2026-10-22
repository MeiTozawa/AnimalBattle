using AnimalBattle.Core;

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
    /// <param name="viewportPos">カメラ視口 左上座標 (Vec2f)</param>
    /// <param name="viewportSize">カメラ視口 サイズ (Vec2f)</param>
    /// <param name="minCol">可視列 最小インデックス (0-based)</param>
    /// <param name="maxCol">可視列 最大インデックス (0-based)</param>
    /// <param name="minRow">可視行 最小インデックス (0-based)</param>
    /// <param name="maxRow">可視行 最大インデックス (0-based)</param>
    public static void CalculateVisibleBounds(
        TileMap map,
        Vec2f viewportPos,
        Vec2f viewportSize,
        out int minCol,
        out int maxCol,
        out int minRow,
        out int maxRow)
    {
        if (map.Columns <= 0 || map.Rows <= 0 || map.RenderTileSize.X <= 0 || map.RenderTileSize.Y <= 0)
        {
            minCol = maxCol = minRow = maxRow = 0;
            return;
        }

        var relLeft = viewportPos.X - map.Origin.X;
        var relTop = viewportPos.Y - map.Origin.Y;
        var relRight = relLeft + viewportSize.X;
        var relBottom = relTop + viewportSize.Y;

        // 画面端の描画欠け・チラつきを防止するため 1 タイルの保護マージンを適用
        minCol = Math.Clamp((int)MathF.Floor(relLeft / map.RenderTileSize.X) - 1, 0, map.Columns - 1);
        maxCol = Math.Clamp((int)MathF.Ceiling(relRight / map.RenderTileSize.X) + 1, 0, map.Columns - 1);
        minRow = Math.Clamp((int)MathF.Floor(relTop / map.RenderTileSize.Y) - 1, 0, map.Rows - 1);
        maxRow = Math.Clamp((int)MathF.Ceiling(relBottom / map.RenderTileSize.Y) + 1, 0, map.Rows - 1);
    }

    /// <summary>
    ///     カメラ視口に基づいてカリングされた可視タイルのみを抽出して描画コマンドバッファへ投入します。
    /// </summary>
    /// <param name="map">対象のタイルマップ</param>
    /// <param name="viewportPos">カメラ視口 左上座標 (Vec2f)</param>
    /// <param name="viewportSize">カメラ視口 サイズ (Vec2f)</param>
    public static void DrawCulled(TileMap map, Vec2f viewportPos, Vec2f viewportSize)
    {
        if (map.TileSet.TileWidth <= 0 || map.TileSet.TileHeight <= 0) return;

        CalculateVisibleBounds(map, viewportPos, viewportSize,
            out var minCol, out var maxCol, out var minRow, out var maxRow);

        var scaleX = (float)map.RenderTileSize.X / map.TileSet.TileWidth;
        var scaleY = (float)map.RenderTileSize.Y / map.TileSet.TileHeight;
        var scale = new Vec2f(scaleX, scaleY);

        for (var r = minRow; r <= maxRow; r++)
        for (var c = minCol; c <= maxCol; c++)
        {
            var tileId = map.GetTile(c, r);
            if (tileId <= 0) continue;

            var centerX = (int)(map.Origin.X + c * map.RenderTileSize.X + map.RenderTileSize.X / 2.0f);
            var centerY = (int)(map.Origin.Y + r * map.RenderTileSize.Y + map.RenderTileSize.Y / 2.0f);

            map.TileSet.DrawTile(tileId, new Vec2i(centerX, centerY), scale, 0.0, false);
        }
    }
}
