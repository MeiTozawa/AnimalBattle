using AnimalBattle.Core;
using AnimalBattle.Maps;
using Xunit;

namespace AnimalBattle.Tests.Maps;

public class TileBatchRenderTests
{
    [Fact]
    public void CalculateVisibleGridBounds_ReturnsOnlyTilesInsideViewport()
    {
        // 100 列 x 100 行（各タイル 70x70）
        var tileSet = new TileSet(1, 70, 70, 6, 7, 1, 21, 41, new Rect(0, 0, 70, 40));
        var map = new TileMap(tileSet, 100, 100, 70, 70, 0, 0, new int[100, 100]);

        // ビューポート領域 (X: 140〜350, Y: 210〜420)
        TileBatchRenderer.CalculateVisibleBounds(map, 140.0f, 210.0f, 210.0f, 210.0f,
            out var minCol, out var maxCol, out var minRow, out var maxRow);

        Assert.Equal(2, minCol); // 140 / 70 = 2
        Assert.Equal(5, maxCol); // 350 / 70 = 5
        Assert.Equal(3, minRow); // 210 / 70 = 3
        Assert.Equal(6, maxRow); // 420 / 70 = 6
    }
}
