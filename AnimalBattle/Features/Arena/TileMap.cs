using AnimalBattle.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Maps.Navigation;
using AnimalBattle.Maps.Spatial;
using Flecs.NET.Core;

namespace AnimalBattle.Maps;

/// <summary>
///     2D グリッドデータおよびタイルセットを参照し、ステージマップ全体の配置と描画を管理するクラス。
/// </summary>
/// <param name="tileSet">使用するタイルセット</param>
/// <param name="columns">グリッド列数</param>
/// <param name="rows">グリッド行数</param>
/// <param name="renderTileSize">描画時のタイルピクセルサイズ</param>
/// <param name="origin">マップ原点座標</param>
/// <param name="grid">2D タイル ID 配列</param>
public sealed class TileMap(
    TileSet tileSet,
    int columns,
    int rows,
    Vec2i renderTileSize,
    Vec2f origin,
    int[,] grid) {
    /// <summary>関連付けられたタイルセット</summary>
    public TileSet TileSet { get; } = tileSet;

    /// <summary>マップの横方向グリッド列数</summary>
    public int Columns { get; } = columns;

    /// <summary>マップの縦方向グリッド行数</summary>
    public int Rows { get; } = rows;

    /// <summary>描画時のタイルピクセルサイズ</summary>
    public Vec2i RenderTileSize { get; } = renderTileSize;

    /// <summary>マップの左上原点座標</summary>
    public Vec2f Origin { get; } = origin;

    /// <summary>プラットフォーム間跳躍ナビゲーショングラフ</summary>
    public PlatformNavigationGraph NavGraph { get; } = new();

    /// <summary>プラットフォーム足場用の 2D 空間ハッシュグリッド。</summary>
    public PlatformSpatialHash SpatialHash { get; } = new(cellSize: 256.0f);

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
            config.RenderTileSize,
            config.Origin,
            grid);
    }

    /// <summary>
    ///     指定グリッド座標のタイルIDを取得します。範囲外の場合は空白を示すゼロを返します。
    /// </summary>
    /// <param name="col">ゼロ基準の列インデックス</param>
    /// <param name="row">ゼロ基準の行インデックス</param>
    /// <returns>タイル ID</returns>
    public int GetTile(int col, int row)
    {
        if (col < 0 || col >= Columns || row < 0 || row >= Rows) return 0;

        return grid[row, col];
    }

    /// <summary>
    ///     指定グリッド座標にタイル ID を設定します。
    /// </summary>
    /// <param name="col">ゼロ基準の列インデックス</param>
    /// <param name="row">ゼロ基準の行インデックス</param>
    /// <param name="tileId">設定するタイル ID</param>
    public void SetTile(int col, int row, int tileId)
    {
        if (col >= 0 && col < Columns && row >= 0 && row < Rows) grid[row, col] = tileId;
    }

    /// <summary>
    ///     指定されたカメラビューポートに基づいてカリングを行い、画面内タイルのみを描画します。
    /// </summary>
    /// <param name="viewportPos">カメラのビューポート左上座標</param>
    /// <param name="viewportSize">カメラのビューポートサイズ</param>
    public void DrawCulled(Vec2f viewportPos, Vec2f viewportSize)
    {
        TileBatchRenderer.DrawCulled(this, viewportPos, viewportSize);
    }

    /// <summary>
    ///     タイルマップのグリッドデータおよびタイルセットの衝突領域に基づいて、
    ///     水平方向に連続するブロックを結合したプラットフォームコライダーエンティティ群を生成し、
    ///     ナビゲーショングラフを自動構築します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <param name="parent">所属させる親エンティティ。省略可能です</param>
    public void BuildPlatformColliders(World world, Entity parent)
    {
        SpatialHash.Clear();
        if (Columns <= 0 || Rows <= 0 || TileSet.TileWidth <= 0 || TileSet.TileHeight <= 0) return;

        var scaleX = (float)RenderTileSize.X / TileSet.TileWidth;
        var scaleY = (float)RenderTileSize.Y / TileSet.TileHeight;

        var colOffsetX = TileSet.CollisionRect.X1 * scaleX;
        var colOffsetY = TileSet.CollisionRect.Y1 * scaleY;
        var colWidth = TileSet.CollisionRect.Width * scaleX;
        var colHeight = TileSet.CollisionRect.Height * scaleY;

        List<PlatformNode> nodes = [];
        var nodeId = 0;
        var platformId = 0;

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
                RegisterPlatformNodeAndEntity(world, nodes, ref nodeId, ref platformId, r, startCol, tileCount, colOffsetX, colOffsetY,
                    colWidth, colHeight, parent);
            }
        }

        var b2Cfg = world.Get<Boss2Config>();
        var arenaCfg = world.Get<ArenaConfig>();
        NavGraph.Build(nodes, b2Cfg.JumpForce, b2Cfg.Gravity, b2Cfg.MoveSpeed, arenaCfg.NavigationGraph);
        Log.Info("TileMap", 
            $"[TileMap ナビゲーション] プラットフォームナビゲーショングラフを正常に構築しました: {NavGraph.Nodes.Count} ノード, {NavGraph.Links.Count} リンク (跳躍/落下遷移)");
    }

    private void RegisterPlatformNodeAndEntity(
        World world,
        List<PlatformNode> nodes,
        ref int nodeId,
        ref int platformId,
        int row,
        int startCol,
        int tileCount,
        float colOffsetX,
        float colOffsetY,
        float colWidth,
        float colHeight,
        Entity parent)
    {
        var leftX = Origin.X + startCol * RenderTileSize.X;
        var topY = Origin.Y + row * RenderTileSize.Y;

        var platformWidth = tileCount * colWidth;
        var platformHeight = colHeight;

        var centerX = leftX + colOffsetX + platformWidth / 2.0f;
        var centerY = topY + colOffsetY + platformHeight / 2.0f;

        var leftBoundX = leftX + colOffsetX;
        var rightBoundX = leftBoundX + platformWidth;
        var topSurfaceY = topY + colOffsetY;

        var arenaCfg = world.Get<ArenaConfig>();
        var clampedLeft = MathF.Max(leftBoundX, arenaCfg.BoundaryMinX);
        var clampedRight = MathF.Min(rightBoundX, arenaCfg.BoundaryMaxX);

        if (topSurfaceY <= arenaCfg.TileMap.MaxPlatformY && clampedRight > clampedLeft + arenaCfg.TileMap.MinPlatformWidth)
            nodes.Add(new PlatformNode(nodeId++, clampedLeft, clampedRight, topSurfaceY, platformHeight));

        var platformBox = new Box2D(new Vec2f(centerX, centerY), new Vec2f(platformWidth, platformHeight));
        SpatialHash.Insert(platformId++, platformBox);

        var entity = world.Entity($"Platform_{row}_{startCol}")
           .Set(new Transform(new Vec2f(centerX, centerY)))
           .Set(new CollisionBox(new Vec2f(platformWidth, platformHeight)))
           .Add<PlatformTag>();

        entity.ChildOf(parent);
    }
}