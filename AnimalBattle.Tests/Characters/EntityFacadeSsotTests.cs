using AnimalBattle.Ai.Contracts;
using AnimalBattle.Characters;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Characters;

public class EntityFacadeSsotTests
{
    [Fact]
    public void PlayerFacade_DirectlyReflectsEcsTransform_WithoutShadowCopies()
    {
        using World world = World.Create();
        Entity entity = world.Entity("TestPlayer");
        PlayerConfig cfg = new() { SpawnX = 100.0f, SpawnY = 200.0f };
        Player player = new(entity, cfg, -1);

        Assert.Equal(100.0f, player.X);
        Assert.Equal(200.0f, player.Y);

        // 通过 Facade 修改坐标，必须直接修改底层的 ECS Transform 组件
        player.X = 350.0f;
        Assert.Equal(350.0f, entity.Get<Transform>().X);

        // 反之，底层 ECS 组件被物理系统修改，Facade 必须立刻呈现，无需手动同步
        entity.GetMut<Transform>().X = 480.0f;
        Assert.Equal(480.0f, player.X);
    }

    [Fact]
    public void BossFacade_DirectlyReflectsEcsTransform_WithoutShadowCopies()
    {
        using World world = World.Create();
        Entity entity = world.Entity("TestBoss");
        BossCharacter boss = new(entity, BossType.Boss1, -1);

        // 验证 Boss Facade 的坐标直接读写底层 ECS Transform
        boss.X = 600.0f;
        boss.Y = 320.0f;
        Assert.Equal(600.0f, entity.Get<Transform>().X);
        Assert.Equal(320.0f, entity.Get<Transform>().Y);

        entity.GetMut<Transform>().X = 750.0f;
        Assert.Equal(750.0f, boss.X);
    }
}
