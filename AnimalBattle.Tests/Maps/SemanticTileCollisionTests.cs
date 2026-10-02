using AnimalBattle.Components;
using AnimalBattle.Core;
using AnimalBattle.Maps;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Maps;

public class SemanticTileCollisionTests
{
    [Fact]
    public void BuildPlatformColliders_FiltersOutDecoTiles()
    {
        using var world = World.Create();
        var tileSet = new TileSet(1, 70, 70, 6, 7, 1, 21, 41, new Rect(0, 0, 70, 40));
        tileSet.SetCollisionType(1, TileCollisionType.OneWayPlatform);
        tileSet.SetCollisionType(21, TileCollisionType.OneWayPlatform);
        tileSet.SetCollisionType(41, TileCollisionType.OneWayPlatform);
        tileSet.SetCollisionType(99, TileCollisionType.DecoOnly); // 装飾タイル（草花など）

        // 行 0: 装飾タイル 99 と足場タイル 1, 21
        int[,] grid = {
            { 99, 1, 21, 0 }
        };

        var map = new TileMap(tileSet, 4, 1, 70, 70, 0, 0, grid);
        map.BuildPlatformColliders(world);

        // 足場タイルのみ Collider 生成（DecoTile 99 は除外）
        var query = world.QueryBuilder<Transform, CollisionBox>().With<PlatformTag>().Build();
        var count = 0;
        query.Each((ref Transform t, ref CollisionBox c) => {
            count++;
            // 結合幅検証: タイル 2 枚分（2 * 70 = 140）
            Assert.Equal(140.0f, c.Width);
        });

        Assert.Equal(1, count);
        query.Dispose();
    }
}
