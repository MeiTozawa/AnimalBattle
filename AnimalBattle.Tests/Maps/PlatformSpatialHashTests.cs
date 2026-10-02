using AnimalBattle.Core;
using AnimalBattle.Maps.Spatial;
using Xunit;

namespace AnimalBattle.Tests.Maps;

public class PlatformSpatialHashTests
{
    [Fact]
    public void QueryCandidatePlatforms_ReturnsOnlyPlatformsInSameBucket()
    {
        var spatialHash = new PlatformSpatialHash(cellSize: 200.0f);
        Box2D platA = new(100.0f, 100.0f, 100.0f, 20.0f); // セル (0, 0)
        Box2D platB = new(900.0f, 900.0f, 100.0f, 20.0f); // セル (4, 4)

        spatialHash.Insert(1, platA);
        spatialHash.Insert(2, platB);

        Box2D queryBox = new(120.0f, 110.0f, 40.0f, 40.0f);
        List<int> results = [];
        spatialHash.Query(queryBox, results);

        Assert.Single(results);
        Assert.Equal(1, results[0]);
    }
}
