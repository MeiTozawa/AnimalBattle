using AnimalBattle.Core;
using AnimalBattle.Maps.Navigation;
using Xunit;

namespace AnimalBattle.Tests.Maps;

public class TrajectoryRaycastTests
{
    [Fact]
    public void BuildWithObstacles_WhenSolidWallBlocksTrajectory_DoesNotCreateLink()
    {
        var graph = new PlatformNavigationGraph();
        var nodeA = new PlatformNode(0, 0.0f, 100.0f, 500.0f, 20.0f);
        var nodeB = new PlatformNode(1, 300.0f, 400.0f, 500.0f, 20.0f);
        List<PlatformNode> nodes = [nodeA, nodeB];

        // 中間に位置する遮断壁 (X: 180〜220, Y: 200〜600)
        List<Box2D> obstacles = [new Box2D(200.0f, 400.0f, 40.0f, 400.0f)];

        graph.BuildWithObstacles(nodes, obstacles, jumpForce: 600.0f, gravity: 980.0f, moveSpeed: 220.0f);

        // 遮断壁により、nodeA から nodeB への跳躍リンクが除外されたことを検証
        Assert.Empty(graph.Links);
    }
}
