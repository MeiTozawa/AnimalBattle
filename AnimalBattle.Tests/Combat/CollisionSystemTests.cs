using AnimalBattle.Components;
using AnimalBattle.Systems;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Combat;

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

        Assert.Equal(100.0f, victim.Get<Health>().Current);
        Assert.True(attacker.IsAlive());
    }

    [Fact]
    public void CombatCollisionSystem_OverlappingAabb_AppliesDamageAndDestructsBullet()
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

        Assert.True(victim.Get<Health>().Current < 100.0f);
        Assert.False(attacker.IsAlive());
    }
}
