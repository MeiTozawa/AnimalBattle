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

        // 実戦の弾幕環境をシミュレートするために複数の攻撃エンティティを追加
        for (int i = 0; i < 10; i++)
        {
            world.Entity($"Attacker_{i}")
                .Set(new Transform(100.0f + i * 5, 100.0f))
                .Set(new Hitbox(new CollisionShape.Circle(10.0f), damage: 10.0f, faction: Faction.Friendly));
        }

        // JIT と ECS テーブル構造をウォームアップ
        world.Progress(0.016f);

        long bytesBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++)
        {
            world.Progress(0.016f);
        }
        long bytesAllocated = GC.GetAllocatedBytesForCurrentThread() - bytesBefore;

        // 100 フレームのループ内でクロージャ生成によるヒープ割り当てがゼロ（1KB 未満）であることを検証
        Assert.True(bytesAllocated < 1024, $"Allocated {bytesAllocated} bytes during 100 frames with 10 attackers!");
    }
}
