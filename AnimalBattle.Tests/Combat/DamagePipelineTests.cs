using System.Drawing;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Components;
using AnimalBattle.Systems;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Combat;

/// <summary>
///     純粋な ECS ダメージ裁定流水線（DamageResolutionSystem）のテストスイート。
/// </summary>
public class DamagePipelineTests
{
    [Fact]
    public void DamageResolutionSystem_ProcessesIncomingDamage_DirectlyInHotPath()
    {
        using var world = World.Create();
        world.AddDamageResolutionSystem();

        var victim = world.Entity("Victim")
            .Set(new Transform(100.0f, 100.0f, 1.0f, 1.0f))
            .Set(new Health(100.0f))
            .Set(new InvincibleOnHit(1.5f))
            .Set(new HitFlashConfig(0.08f, Color.White))
            .Set(new BossCombatState(isVulnerable: true)) // 隙あり易傷状態
            .Set(new IncomingDamage { Amount = 20.0f });

        world.Progress(0.016f);

        ref readonly var hp = ref victim.Get<Health>();
        Assert.Equal(60.0f, hp.Current); // 20 * 2.0 = 40 ダメージ、100 - 40 = 60
        Assert.Equal(1.5f, hp.InvincibleTimer); // 被弾無敵タイマーが自動設定される
        Assert.False(victim.Has<IncomingDamage>()); // ダメージ意図が正常に消費される
        Assert.True(victim.Has<HitFlashEffect>()); // 白閃演出が自動付与される
    }

    [Fact]
    public void DamageResolutionSystem_IgnoresDamage_WhenInvincibleByHealthTimer()
    {
        using var world = World.Create();
        world.AddDamageResolutionSystem();

        var victim = world.Entity("Victim")
            .Set(new Transform(100.0f, 100.0f, 1.0f, 1.0f))
            .Set(new Health(100.0f) { InvincibleTimer = 1.0f }) // Health.InvincibleTimer による無敵判定
            .Set(new InvincibleOnHit(1.0f))
            .Set(new HitFlashConfig(0.08f, Color.White))
            .Set(new IncomingDamage { Amount = 50.0f });

        world.Progress(0.016f);

        ref readonly var hp = ref victim.Get<Health>();
        Assert.Equal(100.0f, hp.Current); // 無敵のためダメージ無効
        Assert.False(victim.Has<IncomingDamage>());
    }

    [Fact]
    public void DamageResolutionSystem_TagsDeadTag_WhenHpReachesZero()
    {
        using var world = World.Create();
        world.AddDamageResolutionSystem();

        var victim = world.Entity("Victim")
            .Set(new Transform(100.0f, 100.0f, 1.0f, 1.0f))
            .Set(new Health(10.0f))
            .Set(new InvincibleOnHit(1.0f))
            .Set(new HitFlashConfig(0.08f, Color.White))
            .Set(new IncomingDamage { Amount = 50.0f });

        world.Progress(0.016f);

        ref readonly var hp = ref victim.Get<Health>();
        Assert.Equal(0.0f, hp.Current);
        Assert.True(victim.Has<DeadTag>()); // DeadTag が自動付与される
    }

    [Fact]
    public void HealthInvincibilitySystem_DecaysInvincibleTimer_OverTime()
    {
        using var world = World.Create();
        world.AddDamageResolutionSystem();

        var entity = world.Entity("Target")
            .Set(new Health(100.0f) { InvincibleTimer = 1.0f });

        world.Progress(0.2f);

        ref readonly var hp = ref entity.Get<Health>();
        Assert.True(hp.InvincibleTimer < 0.85f && hp.InvincibleTimer > 0.75f);
    }
}
