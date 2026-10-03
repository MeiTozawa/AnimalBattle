using System.Drawing;
using AnimalBattle.Components;
using AnimalBattle.Systems;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Combat;

/// <summary>
///     戦闘衝突判定システム（CombatCollisionSystem）およびダメージ連携テスト。
/// </summary>
public class CollisionSystemTests
{
    [Fact]
    public void CombatCollisionSystem_DisjointAabb_PrunesWithoutDamage()
    {
        using World world = World.Create();
        world.AddCombatCollisionSystem();

        Entity victim = world.Entity("Victim")
            .Set(new Transform(800.0f, 600.0f))
            .Set(new Health(100.0f))
            .Set(new Hurtbox(new CollisionShape.Box(40.0f, 40.0f), Faction.Hostile));

        Entity attacker = world.Entity("Attacker")
            .Set(new Transform(100.0f, 100.0f))
            .Set(new Hitbox(new CollisionShape.Circle(10.0f), damage: 25.0f, faction: Faction.Friendly));

        world.Progress(0.016f);

        Assert.False(victim.Has<IncomingDamage>());
        Assert.Equal(100.0f, victim.Get<Health>().Current);
        Assert.True(attacker.IsAlive());
    }

    [Fact]
    public void CombatCollisionSystem_OverlappingAabb_EmitsIncomingDamageAndDestructsBullet()
    {
        using World world = World.Create();
        world.AddCombatCollisionSystem();

        Entity victim = world.Entity("Victim")
            .Set(new Transform(200.0f, 200.0f))
            .Set(new Health(100.0f))
            .Set(new Hurtbox(new CollisionShape.Box(40.0f, 40.0f), Faction.Hostile));

        Entity attacker = world.Entity("Attacker")
            .Set(new Transform(205.0f, 200.0f))
            .Set(new Hitbox(new CollisionShape.Circle(10.0f), damage: 25.0f, faction: Faction.Friendly, destroyOnHit: true));

        world.Progress(0.016f);

        // 衝突システム単体では Health を直接変更せず、IncomingDamage を発行して弾丸を破棄
        Assert.True(victim.Has<IncomingDamage>());
        Assert.Equal(25.0f, victim.Get<IncomingDamage>().Amount);
        Assert.False(attacker.IsAlive());
    }

    [Fact]
    public void CombatCollisionAndDamagePipeline_OverlappingAabb_ResolvesDamageDirectly()
    {
        using World world = World.Create();
        world.AddCombatCollisionSystem()
             .AddDamageResolutionSystem();

        Entity victim = world.Entity("Victim")
            .Set(new Transform(200.0f, 200.0f))
            .Set(new Health(100.0f))
            .Set(new InvincibleOnHit(1.0f))
            .Set(new HitFlashConfig(0.08f, Color.White))
            .Set(new Hurtbox(new CollisionShape.Box(40.0f, 40.0f), Faction.Hostile));

        Entity attacker = world.Entity("Attacker")
            .Set(new Transform(205.0f, 200.0f))
            .Set(new Hitbox(new CollisionShape.Circle(10.0f), damage: 25.0f, faction: Faction.Friendly, destroyOnHit: true));

        world.Progress(0.016f);

        // パイプライン連携により、IncomingDamage が消費されて Health.Current が減少
        Assert.False(victim.Has<IncomingDamage>());
        Assert.Equal(75.0f, victim.Get<Health>().Current);
        Assert.False(attacker.IsAlive());
    }
}
