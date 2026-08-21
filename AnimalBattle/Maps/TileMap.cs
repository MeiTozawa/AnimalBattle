using System;
using System.Collections.Generic;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Maps.Navigation;

namespace AnimalBattle.Maps;

/// <summary>
/// 2D グリッドデータおよびタイルセットを参照し、ステージマップ全体の配置と描画を管理するクラス。
/// </summary>
public sealed class TileMap
{
    private readonly TileSet _tileSet;
    private readonly int[,] _grid;
    private readonly int _columns;
    private readonly int _rows;
    private readonly int _renderTileWidth;
    private readonly int _renderTileHeight;
    private readonly float _originX;
    private readonly float _originY;
    private readonly PlatformNavigationGraph _navGraph = new();

    /// <summary>関連付けられたタイルセット</summary>
    public TileSet TileSet => _tileSet;

    /// <summary>マップの横方向グリッド列数</summary>
    public int Columns => _columns;

    /// <summary>マップの縦方向グリッド行数</summary>
    public int Rows => _rows;

    /// <summary>描画時のタイル単体幅（ピクセル）</summary>
    public int RenderTileWidth => _renderTileWidth;

    /// <summary>描画時のタイル単体高さ（ピクセル）</summary>
    public int RenderTileHeight => _renderTileHeight;

    /// <summary>マップ原点 X 座標（左上）</summary>
    public float OriginX => _originX;

    /// <summary>マップ原点 Y 座標（左上）</summary>
    public float OriginY => _originY;

    /// <summary>プラットフォーム間跳躍ナビゲーショングラフ</summary>
    public PlatformNavigationGraph NavGraph => _navGraph;

    /// <summary>
    /// 指定されたパラメータで <see cref="TileMap"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="tileSet">使用するタイルセット</param>
    /// <param name="columns">グリッド列数</param>
    /// <param name="rows">グリッド行数</param>
    /// <param name="renderTileWidth">描画時のタイル幅（ピクセル）</param>
    /// <param name="renderTileHeight">描画時のタイル高さ（ピクセル）</param>
    /// <param name="originX">マップ原点 X 座標</param>
    /// <param name="originY">マップ原点 Y 座標</param>
    /// <param name="grid">2D タイル ID 配列</param>
    public TileMap(
        TileSet tileSet,
        int columns,
        int rows,
        int renderTileWidth,
        int renderTileHeight,
        float originX,
        float originY,
        int[,] grid)
    {
        _tileSet = tileSet;
        _columns = columns;
        _rows = rows;
        _renderTileWidth = renderTileWidth;
        _renderTileHeight = renderTileHeight;
        _originX = originX;
        _originY = originY;
        _grid = grid;
    }

    /// <summary>
    /// <see cref="TileMapConfig"/> 設定データおよび <see cref="TileSet"/> から <see cref="TileMap"/> を構築します。
    /// </summary>
    /// <param name="config">タイルマップ設定</param>
    /// <param name="tileSet">使用するタイルセット</param>
    /// <returns>構築された <see cref="TileMap"/> インスタンス</returns>
    public static TileMap FromConfig(TileMapConfig config, TileSet tileSet)
    {
        int rows = config.Data.Count;
        int cols = 0;
        for (int r = 0; r < rows; r++)
        {
            if (config.Data[r].Count > cols)
            {
                cols = config.Data[r].Count;
            }
        }

        int[,] grid = new int[rows, cols];
        for (int r = 0; r < rows; r++)
        {
            List<int> rowList = config.Data[r];
            for (int c = 0; c < rowList.Count; c++)
            {
                grid[r, c] = rowList[c];
            }
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
    /// 指定グリッド座標のタイル ID を取得します。範囲外の場合は 0（空気/空白）を返します。
    /// </summary>
    /// <param name="col">列インデックス (0-based)</param>
    /// <param name="row">行インデックス (0-based)</param>
    /// <returns>タイル ID</returns>
    public int GetTile(int col, int row)
    {
        if (col < 0 || col >= _columns || row < 0 || row >= _rows)
        {
            return 0;
        }

        return _grid[row, col];
    }

    /// <summary>
    /// 指定グリッド座標にタイル ID を設定します。
    /// </summary>
    /// <param name="col">列インデックス (0-based)</param>
    /// <param name="row">行インデックス (0-based)</param>
    /// <param name="tileId">設定するタイル ID</param>
    public void SetTile(int col, int row, int tileId)
    {
        if (col >= 0 && col < _columns && row >= 0 && row < _rows)
        {
            _grid[row, col] = tileId;
        }
    }

    /// <summary>
    /// マップ全体の可視タイルを順次描画します。
    /// </summary>
    public void Draw()
    {
        if (_tileSet.TileWidth <= 0 || _tileSet.TileHeight <= 0)
        {
            return;
        }

        double scaleX = (double)_renderTileWidth / _tileSet.TileWidth;
        double scaleY = (double)_renderTileHeight / _tileSet.TileHeight;
        double scale = (scaleX + scaleY) / 2.0;

        for (int r = 0; r < _rows; r++)
        {
            for (int c = 0; c < _columns; c++)
            {
                int tileId = _grid[r, c];
                if (tileId <= 0)
                {
                    continue;
                }

                int centerX = (int)(_originX + c * _renderTileWidth + _renderTileWidth / 2.0f);
                int centerY = (int)(_originY + r * _renderTileHeight + _renderTileHeight / 2.0f);

                _tileSet.DrawTile(tileId, centerX, centerY, scale, 0.0, false);
            }
        }
    }

    /// <summary>
    /// タイルマップのグリッドデータおよびタイルセットの衝突領域（CollisionRect）に基づいて、
    /// 水平方向に連続するブロックを結合したプラットフォームコライダーエンティティ群を生成し、
    /// ナビゲーショングラフ (<see cref="PlatformNavigationGraph"/>) を自動構築します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    public void BuildPlatformColliders(World world)
    {
        if (_columns <= 0 || _rows <= 0 || _tileSet.TileWidth <= 0 || _tileSet.TileHeight <= 0)
        {
            return;
        }

        float scaleX = (float)_renderTileWidth / _tileSet.TileWidth;
        float scaleY = (float)_renderTileHeight / _tileSet.TileHeight;

        float colOffsetX = _tileSet.CollisionRect.X1 * scaleX;
        float colOffsetY = _tileSet.CollisionRect.Y1 * scaleY;
        float colWidth = _tileSet.CollisionRect.Width * scaleX;
        float colHeight = _tileSet.CollisionRect.Height * scaleY;

        List<PlatformNode> nodes = new();
        int nodeId = 0;

        for (int r = 0; r < _rows; r++)
        {
            int c = 0;
            while (c < _columns)
            {
                if (_grid[r, c] <= 0)
                {
                    c++;
                    continue;
                }

                int startCol = c;
                while (c < _columns && _grid[r, c] > 0)
                {
                    c++;
                }

                int tileCount = c - startCol;
                float leftX = _originX + startCol * _renderTileWidth;
                float topY = _originY + r * _renderTileHeight;

                float platformWidth = tileCount * colWidth;
                float platformHeight = colHeight;

                float centerX = leftX + colOffsetX + platformWidth / 2.0f;
                float centerY = topY + colOffsetY + platformHeight / 2.0f;

                float leftBoundX = leftX + colOffsetX;
                float rightBoundX = leftBoundX + platformWidth;
                float topSurfaceY = topY + colOffsetY;

                nodes.Add(new PlatformNode(nodeId++, leftBoundX, rightBoundX, topSurfaceY, platformHeight));

                world.Entity($"Platform_{r}_{startCol}")
                    .Set(new Transform(centerX, centerY))
                    .Set(new CollisionBox(platformWidth, platformHeight))
                    .Add<PlatformTag>();
            }
        }

        Boss2Config b2Cfg = ConfigManager.Boss2;
        _navGraph.Build(nodes, b2Cfg.JumpForce, b2Cfg.Gravity, b2Cfg.MoveSpeed);
        Console.WriteLine($"[TileMap ナビゲーション] プラットフォームナビゲーショングラフを正常に構築しました: {_navGraph.Nodes.Count} ノード, {_navGraph.Links.Count} リンク (跳躍/落下遷移)");
    }
}

