using AnimalBattle.Ai.Contracts;
using AnimalBattle.Characters;
using AnimalBattle.Characters.Boss;
using AnimalBattle.Components;
using AnimalBattle.Core;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Ai;

/// <summary>
///     多態 Boss コントローラー（IBossController / Boss1Controller / Boss2Controller）の動作検証テスト。
/// </summary>
public class BossPolymorphismTests
{
    [Fact]
    public void Boss1Controller_ImplementsIBossController_And_UpdatesSuccessfully()
    {
        using var world = World.Create();
        var entity = world.Entity("Boss1Test");
        using IBossController controller = new Boss1Controller(entity, -1);

        Assert.Equal(BossType.Boss1, controller.BossType);
        Assert.True(controller.IsAlive);

        var platformQuery = world.QueryBuilder<Transform, CollisionBox>().With<PlatformTag>().Build();
        controller.Update(0.016f, Vec2f.Zero, Vec2f.Zero, world, platformQuery);

        Assert.True(controller.IsAlive);
    }

    [Fact]
    public void Boss2Controller_ImplementsIBossController_And_UpdatesSuccessfully()
    {
        using var world = World.Create();
        var entity = world.Entity("Boss2Test");
        using IBossController controller = new Boss2Controller(entity, -1);

        Assert.Equal(BossType.Boss2, controller.BossType);
        Assert.True(controller.IsAlive);

        var platformQuery = world.QueryBuilder<Transform, CollisionBox>().With<PlatformTag>().Build();
        controller.Update(0.016f, Vec2f.Zero, Vec2f.Zero, world, platformQuery);

        Assert.True(controller.IsAlive);
    }
}
