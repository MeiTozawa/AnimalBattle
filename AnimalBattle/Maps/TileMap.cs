using AnimalBattle.Configs;
using AnimalBattle.Maps.Navigation;
using Flecs.NET.Core;

namespace AnimalBattle.Maps;

/// <summary>
///     2D グリッドデータおよびタイルセットを参照し、ステージマップ全体の配置と描画を管理するクラス。
/// </summary>
/// <param name="tileSet">使用するタイルセット</param>
/// <param name="columns">グリッド列数</param>
/// <param name="rows">グリッド行数</param>
/// <param name="renderTileWidth">描画時のタイル幅（ピクセル）</param>
/// <param name="renderTileHeight">描画時のタイル高さ（ピクセル）</param>
/// <param name="originX">マップ原点 X 座標</param>
/// <param name="originY">マップ原点 Y 座標</param>
/// <param name="grid">2D タイル ID 配列</param>
public sealed class TileMap(
    TileSet tileSet,
    int columns,
    int rows,
    int renderTileWidth,
    int renderTileHeight,
    float originX,
    float originY,
    int[,] grid) {
    /// <summary>関連付けられたタイルセット</summary>
    public TileSet TileSet { get; } = tileSet;

    /// <summary>マップの横方向グリッド列数</summary>
    public int Columns { get; } = columns;

    /// <summary>マップの縦方向グリッド行数</summary>
    public int Rows { get; } = rows;

    /// <summary>描画時のタイル単体幅（ピクセル）</summary>
    public int RenderTileWidth { get; } = renderTileWidth;

    /// <summary>描画時のタイル単体高さ（ピクセル）</summary>
    public int RenderTileHeight { get; } = renderTileHeight;

    /// <summary>マップ原点 X 座標（左上）</summary>
    public float OriginX { get; } = originX;

    /// <summary>マップ原点 Y 座標（左上）</summary>
    public float OriginY { get; } = originY;

    /// <summary>プラットフォーム間跳躍ナビゲーショングラフ</summary>
    public PlatformNavigationGraph NavGraph { get; } = new();

    /// <summary>
    ///     <see cref="TileMapConfig" /> 設定データおよび <see cref="TileSet" /> から <see cref="TileMap" /> を構築します。
    /// </summary>
    /// <param name="config">タイルマップ設定</param>
    /// <param name="tileSet">使用するタイルセット</param>
    /// <returns>構築された <see cref="TileMap" /> インスタンス</returns>
    public static TileMap FromConfig(TileMapConfig config, TileSet tileSet)
    {
        var rows = config.Data.Count;
        var cols = 0;
        for (var r = 0; r < rows; r++)
            if (config.Data[r].Count > cols)
                cols = config.Data[r].Count;

        var grid = new int[rows, cols];
        for (var r = 0; r < rows; r++)
        {
            var rowList = config.Data[r];
            for (var c = 0; c < rowList.Count; c++) grid[r, c] = rowList[c];
        }

        return new TileMap(
            tileSet,
            cols,
            rows,
            config.RenderTileWidth,
            config.RenderTileHeight,
            config.OriginX,
            config.OriginY,
            grid);
    }

    /// <summary>
    ///     指定グリッド座標のタイル ID を取得します。範囲外の場合は 0（空気/空白）を返します。
    /// </summary>
    /// <param name="col">列インデックス (0-based)</param>
    /// <param name="row">行インデックス (0-based)</param>
    /// <returns>タイル ID</returns>
    public int GetTile(int col, int row)
    {
        if (col < 0 || col >= Columns || row < 0 || row >= Rows) return 0;

        return grid[row, col];
    }

    /// <summary>
    ///     指定グリッド座標にタイル ID を設定します。
    /// </summary>
    /// <param name="col">列インデックス (0-based)</param>
    /// <param name="row">行インデックス (0-based)</param>
    /// <param name="tileId">設定するタイル ID</param>
    public void SetTile(int col, int row, int tileId)
    {
        if (col >= 0 && col < Columns && row >= 0 && row < Rows) grid[row, col] = tileId;
    }

    /// <summary>
    ///     指定されたカメラ視口（Viewport）に基づいてカリングを行い、画面内タイルのみを描画します。
    /// </summary>
    /// <param name="viewX">視口 左端 X</param>
    /// <param name="viewY">視口 上端 Y</param>
    /// <param name="viewW">視口 幅</param>
    /// <param name="viewH">視口 高さ</param>
    public void DrawCulled(float viewX, float viewY, float viewW, float viewH)
    {
        TileBatchRenderer.DrawCulled(this, viewX, viewY, viewW, viewH);
    }

    /// <summary>
    ///     タイルマップのグリッドデータおよびタイルセットの衝突領域（CollisionRect）に基づいて、
    ///     水平方向に連続するブロックを結合したプラットフォームコライダーエンティティ群を生成し、
    ///     ナビゲーショングラフ (<see cref="PlatformNavigationGraph" />) を自動構築します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    public void BuildPlatformColliders(World world)
    {
        if (Columns <= 0 || Rows <= 0 || TileSet.TileWidth <= 0 || TileSet.TileHeight <= 0) return;

        var scaleX = (float)RenderTileWidth / TileSet.TileWidth;
        var scaleY = (float)RenderTileHeight / TileSet.TileHeight;

        var colOffsetX = TileSet.CollisionRect.X1 * scaleX;
        var colOffsetY = TileSet.CollisionRect.Y1 * scaleY;
        var colWidth = TileSet.CollisionRect.Width * scaleX;
        var colHeight = TileSet.CollisionRect.Height * scaleY;

        List<PlatformNode> nodes = [];
        var nodeId = 0;

        for (var r = 0; r < Rows; r++)
        {
            var c = 0;
            while (c < Columns)
            {
                var tileType = TileSet.GetCollisionType(grid[r, c]);
                if (tileType != TileCollisionType.OneWayPlatform && tileType != TileCollisionType.Solid)
                {
                    c++;
                    continue;
                }

                var startCol = c;
                while (c < Columns)
                {
                    var nextType = TileSet.GetCollisionType(grid[r, c]);
                    if (nextType != TileCollisionType.OneWayPlatform && nextType != TileCollisionType.Solid)
                        break;
                    c++;
                }

                var tileCount = c - startCol;
                RegisterPlatformNodeAndEntity(world, nodes, ref nodeId, r, startCol, tileCount, colOffsetX, colOffsetY,
                    colWidth, colHeight);
            }
        }

        var b2Cfg = ConfigManager.Boss2;
        NavGraph.Build(nodes, b2Cfg.JumpForce, b2Cfg.Gravity, b2Cfg.MoveSpeed);
        Console.WriteLine(
            $"[TileMap ナビゲーション] プラットフォームナビゲーショングラフを正常に構築しました: {NavGraph.Nodes.Count} ノード, {NavGraph.Links.Count} リンク (跳躍/落下遷移)");
    }

    private void RegisterPlatformNodeAndEntity(
        World world,
        List<PlatformNode> nodes,
        ref int nodeId,
        int row,
        int startCol,
        int tileCount,
        float colOffsetX,
        float colOffsetY,
        float colWidth,
        float colHeight)
    {
        var leftX = OriginX + startCol * RenderTileWidth;
        var topY = OriginY + row * RenderTileHeight;

        var platformWidth = tileCount * colWidth;
        var platformHeight = colHeight;

        var centerX = leftX + colOffsetX + platformWidth / 2.0f;
        var centerY = topY + colOffsetY + platformHeight / 2.0f;

        var leftBoundX = leftX + colOffsetX;
        var rightBoundX = leftBoundX + platformWidth;
        var topSurfaceY = topY + colOffsetY;

        var arenaCfg = ConfigManager.Arena;
        var clampedLeft = MathF.Max(leftBoundX, arenaCfg.BoundaryMinX);
        var clampedRight = MathF.Min(rightBoundX, arenaCfg.BoundaryMaxX);

        if (topSurfaceY <= 720.0f && clampedRight > clampedLeft + 10.0f)
            nodes.Add(new PlatformNode(nodeId++, clampedLeft, clampedRight, topSurfaceY, platformHeight));

        world.Entity($"Platform_{row}_{startCol}")
           .Set(new Transform(centerX, centerY))
           .Set(new CollisionBox(platformWidth, platformHeight))
           .Add<PlatformTag>();
    }
}