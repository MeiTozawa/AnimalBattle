using AnimalBattle.Components;
using AnimalBattle.Core;
using AnimalBattle.Systems;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Combat;

public class PooledCombatEntityTests
{
    [Fact]
    public void CombatEntities_AreRecycledToPool_InsteadOfDestructed()
    {
        using World world = World.Create();
        CombatEntityPoolComponent pool = world.GetCombatEntityPool();

        Assert.True(pool.PlayerBullets.AvailableCount > 0);
        int initialAvailable = pool.PlayerBullets.AvailableCount;

        Entity bullet = CombatEntityPoolHelper.RentPlayerBullet(world);
        Assert.True(bullet.IsAlive());
        Assert.Equal(initialAvailable - 1, pool.PlayerBullets.AvailableCount);

        CombatEntityPoolHelper.Recycle(world, bullet);
        Assert.Equal(initialAvailable, pool.PlayerBullets.AvailableCount);
    }
}
