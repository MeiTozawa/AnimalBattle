using AnimalBattle.Components;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Render;

public class UnifiedRenderPipelineTests
{
    [Fact]
    public void WorldRenderer_QueriesAllEntitiesWithTransformAndSprite()
    {
        using World world = World.Create();

        // 无论是 Player、Boss 还是普通小怪/子弹，只要挂载 Transform + Sprite，均统一被 Query 捕获
        Entity player = world.Entity("Player").Set(new Transform(100, 100)).Set(new Sprite(1));
        Entity boss = world.Entity("Boss").Set(new Transform(200, 200)).Set(new Sprite(2));
        Entity minion = world.Entity("Minion").Set(new Transform(300, 300)).Set(new Sprite(3));

        int count = 0;
        using Query<Transform, Sprite> query = world.QueryBuilder<Transform, Sprite>().Build();
        query.Each((ref Transform t, ref Sprite s) =>
        {
            count++;
        });

        Assert.Equal(3, count);
    }
}
