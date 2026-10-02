using AnimalBattle.Maps.Data;
using Xunit;

namespace AnimalBattle.Tests.Maps;

public class LevelDataLoaderTests
{
    [Fact]
    public void LoadFromJson_ValidJson_ReturnsPopulatedLevelData()
    {
        const string json = """
        {
            "width": 4,
            "height": 3,
            "tileWidth": 70,
            "tileHeight": 70,
            "layers": [
                {
                    "name": "Platforms",
                    "data": [
                        0, 0, 0, 0,
                        1, 21, 21, 41,
                        0, 0, 0, 0
                    ]
                }
            ]
        }
        """;

        var result = LevelDataLoader.LoadFromJson(json);

        Assert.False(result.IsError);
        var level = result.Value;
        Assert.Equal(4, level.Width);
        Assert.Equal(3, level.Height);
        Assert.Single(level.Layers);
        Assert.Equal(21, level.Layers[0].Data[5]);
    }

    [Fact]
    public void LoadFromJson_InvalidDimension_ReturnsError()
    {
        const string invalidJson = """
        {
            "width": 0,
            "height": 3,
            "tileWidth": 70,
            "tileHeight": 70,
            "layers": []
        }
        """;

        var result = LevelDataLoader.LoadFromJson(invalidJson);

        Assert.True(result.IsError);
    }
}
