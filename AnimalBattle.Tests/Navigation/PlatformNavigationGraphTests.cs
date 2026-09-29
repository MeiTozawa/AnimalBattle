using System.Collections.Generic;
using AnimalBattle.Maps.Navigation;
using Xunit;

namespace AnimalBattle.Tests.Navigation;

public class PlatformNavigationGraphTests
{
    private static PlatformNavigationGraph CreateTestGraph()
    {
        // 构造三层平台：
        // 1. 地面 (Id: 1, Left: 0, Right: 1000, TopY: 560, BottomY: 600)
        // 2. 中层左平台 (Id: 2, Left: 100, Right: 400, TopY: 420, BottomY: 440)
        // 3. 高层右平台 (Id: 3, Left: 350, Right: 650, TopY: 280, BottomY: 300)
        PlatformNode ground = new(1, 0.0f, 1000.0f, 560.0f, 600.0f);
        PlatformNode midPlatform = new(2, 100.0f, 400.0f, 420.0f, 440.0f);
        PlatformNode highPlatform = new(3, 350.0f, 650.0f, 280.0f, 300.0f);

        PlatformNavigationGraph graph = new();
        graph.Build(new[] { ground, midPlatform, highPlatform }, jumpForce: 650.0f, gravity: 980.0f, moveSpeed: 200.0f);
        return graph;
    }

    [Fact]
    public void BuildGraph_CreatesNodesAndLinks()
    {
        PlatformNavigationGraph graph = CreateTestGraph();

        Assert.Equal(3, graph.Nodes.Count);
        Assert.NotEmpty(graph.Links);
    }

    [Fact]
    public void FindNodeAt_FindsCorrectPlatform()
    {
        PlatformNavigationGraph graph = CreateTestGraph();

        PlatformNode? groundNode = graph.FindNodeAt(500.0f, 560.0f);
        Assert.NotNull(groundNode);
        Assert.Equal(1, groundNode.Id);

        PlatformNode? midNode = graph.FindNodeAt(250.0f, 420.0f);
        Assert.NotNull(midNode);
        Assert.Equal(2, midNode.Id);

        PlatformNode? outsideNode = graph.FindNodeAt(2000.0f, 560.0f);
        Assert.Null(outsideNode);
    }

    [Fact]
    public void FindPath_SamePlatform_ReturnsDirectWalk()
    {
        PlatformNavigationGraph graph = CreateTestGraph();

        // 都在地面，从 200 到 800
        NavPlan plan = graph.FindPath(currentX: 200.0f, currentFootY: 560.0f, targetX: 800.0f, targetFootY: 560.0f, isGrounded: true, moveSpeed: 200.0f);

        Assert.Equal(NavStatus.DirectWalk, plan.Status);
        Assert.Equal(800.0f, plan.TargetX);
    }

    [Fact]
    public void FindPath_CrossPlatform_FindsValidPath()
    {
        PlatformNavigationGraph graph = CreateTestGraph();

        // 从地面 (200, 560) 前往中层平台 (250, 420)
        NavPlan plan = graph.FindPath(currentX: 200.0f, currentFootY: 560.0f, targetX: 250.0f, targetFootY: 420.0f, isGrounded: true, moveSpeed: 200.0f);

        Assert.True(plan.Status == NavStatus.MoveToTakeoff || plan.Status == NavStatus.ExecuteJump);
        Assert.NotNull(plan.NextLink);
        Assert.Equal(NavActionType.Jump, plan.NextLink.Action);
    }

    [Fact]
    public void FindPath_MultipleConsecutiveCalls_MaintainsStateIntegrity()
    {
        // 关键用例：验证 Task 1 改造的内部 A* 容器（_aStarGScore, _aStarCameFrom, _aStarOpenSet）多次连续复用后无污染
        PlatformNavigationGraph graph = CreateTestGraph();

        for (int i = 0; i < 50; i++)
        {
            NavPlan plan1 = graph.FindPath(currentX: 200.0f, currentFootY: 560.0f, targetX: 250.0f, targetFootY: 420.0f, isGrounded: true, moveSpeed: 200.0f);
            Assert.True(plan1.Status != NavStatus.NoPath);

            NavPlan plan2 = graph.FindPath(currentX: 250.0f, currentFootY: 420.0f, targetX: 500.0f, targetFootY: 560.0f, isGrounded: true, moveSpeed: 200.0f);
            Assert.True(plan2.Status != NavStatus.NoPath);
        }
    }
}
