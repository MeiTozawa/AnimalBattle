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
        var nodeA = new PlatformNode(0, 50.0f, 120.0f, 500.0f, 20.0f);
        var nodeB = new PlatformNode(1, 150.0f, 220.0f, 420.0f, 20.0f);
        List<PlatformNode> nodes = [nodeA, nodeB];

        // 中間に位置する遮断壁 (X: 125〜145, Y: 400〜500)
        List<Box2D> obstacles = [new Box2D(135.0f, 450.0f, 20.0f, 100.0f)];

        // 遮断壁あり: 衝突跳躍リンクを除外
        graph.BuildWithObstacles(nodes, obstacles, jumpForce: 600.0f, gravity: 980.0f, moveSpeed: 220.0f);
        Assert.Empty(graph.Links);

        // 遮断壁なし: 跳躍リンクを正常生成
        graph.BuildWithObstacles(nodes, [], jumpForce: 600.0f, gravity: 980.0f, moveSpeed: 220.0f);
        Assert.NotEmpty(graph.Links);
    }
}
