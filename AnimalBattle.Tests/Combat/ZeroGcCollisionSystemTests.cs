using System;
using AnimalBattle.Components;
using AnimalBattle.Systems;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Combat;

public class ZeroGcCollisionSystemTests
{
    [Fact]
    public void CombatCollisionSystem_RunsWithoutAllocatingClosures()
    {
        using World world = World.Create();
        world.AddCombatCollisionSystem();

        world.Entity("Victim")
            .Set(new Transform(200.0f, 200.0f))
            .Set(new Hurtbox(new CollisionShape.Box(40.0f, 40.0f), Faction.Hostile))
            .Set(new Health(100.0f));

        // 添加多个攻击者模拟真实弹幕环境
        for (int i = 0; i < 10; i++)
        {
            world.Entity($"Attacker_{i}")
                .Set(new Transform(100.0f + i * 5, 100.0f))
                .Set(new Hitbox(new CollisionShape.Circle(10.0f), damage: 10.0f, faction: Faction.Friendly));
        }

        // 预热 JIT 与 ECS 表结构
        world.Progress(0.016f);

        long bytesBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++)
        {
            world.Progress(0.016f);
        }
        long bytesAllocated = GC.GetAllocatedBytesForCurrentThread() - bytesBefore;

        // 验证 100 帧循环中堆分配控制在 1KB 以内（理想为 0 闭包分配）
        Assert.True(bytesAllocated < 1024, $"Allocated {bytesAllocated} bytes during 100 frames with 10 attackers!");
    }
}
