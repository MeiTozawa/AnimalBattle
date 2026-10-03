using AnimalBattle.Characters;
using AnimalBattle.Components;
using AnimalBattle.Systems;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Combat;

public class CollisionSystemTests
{
    private sealed class DummyDamageable(float maxHp) : IDamageable {
        public Health Health { get; } = new(maxHp);
    }

    [Fact]
    public void CombatCollisionSystem_DisjointAabb_PrunesWithoutDamage()
    {
        using World world = World.Create();
        world.AddCombatCollisionSystem();

        var victimCombatant = new DummyDamageable(100.0f);
        Entity victim = world.Entity("Victim")
            .Set(new Transform(800.0f, 600.0f))
            .Set(new Hurtbox(new CollisionShape.Box(40.0f, 40.0f), Faction.Hostile, victimCombatant));

        Entity attacker = world.Entity("Attacker")
            .Set(new Transform(100.0f, 100.0f))
            .Set(new Hitbox(new CollisionShape.Circle(10.0f), damage: 25.0f, faction: Faction.Friendly));

        world.Progress(0.016f);

        Assert.Equal(100.0f, victimCombatant.Health.Current);
        Assert.True(attacker.IsAlive());
    }

    [Fact]
    public void CombatCollisionSystem_OverlappingAabb_AppliesDamageAndDestructsBullet()
    {
        using World world = World.Create();
        world.AddCombatCollisionSystem();

        var victimCombatant = new DummyDamageable(100.0f);
        Entity victim = world.Entity("Victim")
            .Set(new Transform(200.0f, 200.0f))
            .Set(new Hurtbox(new CollisionShape.Box(40.0f, 40.0f), Faction.Hostile, victimCombatant));

        Entity attacker = world.Entity("Attacker")
            .Set(new Transform(205.0f, 200.0f))
            .Set(new Hitbox(new CollisionShape.Circle(10.0f), damage: 25.0f, faction: Faction.Friendly, destroyOnHit: true));

        world.Progress(0.016f);

        Assert.True(victimCombatant.Health.Current < 100.0f);
        Assert.False(attacker.IsAlive());
    }
}
