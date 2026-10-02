using AnimalBattle.Components;
using AnimalBattle.Core;
using AnimalBattle.Maps;
using AnimalBattle.Maps.Data;
using AnimalBattle.Maps.Navigation;
using AnimalBattle.Maps.Spatial;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Integration;

public class TileMapIntegrationTests
{
    [Fact]
    public void TileMap_FullPipeline_GeneratesCollidersAndValidNavLinks()
    {
        using var world = World.Create();
        var tileSet = new TileSet(1, 70, 70, 6, 7, 1, 21, 41, new Rect(0, 0, 70, 40));
        tileSet.SetCollisionType(1, TileCollisionType.OneWayPlatform);
        tileSet.SetCollisionType(21, TileCollisionType.OneWayPlatform);
        tileSet.SetCollisionType(41, TileCollisionType.OneWayPlatform);

        // アリーナの代表的な地面と空中足場
        int[,] grid = {
            { 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 1, 21, 41, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0 },
            { 1, 21, 21, 21, 21, 21, 41 }
        };

        var map = new TileMap(tileSet, 7, 4, 70, 70, 0, 0, grid);
        map.BuildPlatformColliders(world);

        // 地面と足場ノードの検証
        Assert.Equal(2, map.NavGraph.Nodes.Count);
        // 跳躍・落下リンクの存在検証
        Assert.NotEmpty(map.NavGraph.Links);

        // ECS プラットフォームエンティティの生成検証
        var query = world.QueryBuilder<Transform, CollisionBox>().With<PlatformTag>().Build();
        var entityCount = 0;
        query.Each((ref Transform t, ref CollisionBox c) => entityCount++);
        Assert.Equal(2, entityCount);
        query.Dispose();
    }

    [Fact]
    public void TileMap_LevelDataLoader_Integration_BuildsWorkingTileMap()
    {
        const string json = """
        {
            "width": 5,
            "height": 3,
            "tileWidth": 70,
            "tileHeight": 70,
            "layers": [
                {
                    "name": "Platforms",
                    "data": [
                        0, 0, 0, 0, 0,
                        1, 21, 41, 0, 0,
                        0, 0, 0, 0, 0
                    ]
                }
            ]
        }
        """;

        var loadResult = LevelDataLoader.LoadFromJson(json);
        Assert.False(loadResult.IsError);

        var level = loadResult.Value;
        var layer = level.Layers[0];
        var grid = new int[level.Height, level.Width];
        for (var r = 0; r < level.Height; r++)
        for (var c = 0; c < level.Width; c++)
        {
            grid[r, c] = layer.Data[r * level.Width + c];
        }

        using var world = World.Create();
        var tileSet = new TileSet(1, 70, 70, 6, 7, 1, 21, 41, new Rect(0, 0, 70, 40));
        var map = new TileMap(tileSet, level.Width, level.Height, level.TileWidth, level.TileHeight, 0, 0, grid);
        map.BuildPlatformColliders(world);

        var query = world.QueryBuilder<Transform, CollisionBox>().With<PlatformTag>().Build();
        var count = 0;
        query.Each((ref Transform t, ref CollisionBox c) =>
        {
            count++;
            Assert.Equal(210.0f, c.Width); // タイル 3 枚分（3 * 70 = 210）
        });
        Assert.Equal(1, count);
        query.Dispose();
    }

    [Fact]
    public void SpatialHash_And_Raycasting_Integration()
    {
        var spatialHash = new PlatformSpatialHash(cellSize: 128.0f);
        Box2D obstacle = new(135.0f, 450.0f, 30.0f, 100.0f);
        spatialHash.Insert(99, obstacle);

        List<int> candidates = [];
        spatialHash.Query(new Box2D(135.0f, 450.0f, 20.0f, 20.0f), candidates);
        Assert.Contains(99, candidates);

        var graph = new PlatformNavigationGraph();
        var nodeA = new PlatformNode(0, 50.0f, 120.0f, 500.0f, 20.0f);
        var nodeB = new PlatformNode(1, 150.0f, 220.0f, 420.0f, 20.0f);

        // 遮断障害物あり: 壁貫通跳躍リンクを除外
        graph.BuildWithObstacles([nodeA, nodeB], [obstacle], jumpForce: 600.0f, gravity: 980.0f, moveSpeed: 220.0f);
        Assert.Empty(graph.Links);

        // 遮断障害物なし: 跳躍リンクを正常生成
        graph.BuildWithObstacles([nodeA, nodeB], [], jumpForce: 600.0f, gravity: 980.0f, moveSpeed: 220.0f);
        Assert.NotEmpty(graph.Links);
    }
}
