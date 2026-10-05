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

    [Fact]
    public void BossControllers_AreIndependentlyConstructed_WithoutBossCharacterMonolith()
    {
        using var world = World.Create();

        // 1. Boss1Controller の独立初期化とコンポーネント構成を検証
        var b1Ent = world.Entity("B1");
        using IBossController boss1 = new Boss1Controller(b1Ent, -1);
        Assert.Equal(BossType.Boss1, boss1.BossType);
        Assert.True(b1Ent.Has<Boss1Tag>());
        Assert.False(b1Ent.Has<Boss2Tag>());
        Assert.True(b1Ent.Has<Health>());

        // 2. Boss2Controller の独立初期化とコンポーネント構成を検証
        var b2Ent = world.Entity("B2");
        using IBossController boss2 = new Boss2Controller(b2Ent, -1);
        Assert.Equal(BossType.Boss2, boss2.BossType);
        Assert.True(b2Ent.Has<Boss2Tag>());
        Assert.False(b2Ent.Has<Boss1Tag>());
        Assert.True(b2Ent.Has<CollisionBox>()); // Boss2 特有の地形衝突コンポーネント
    }
}

