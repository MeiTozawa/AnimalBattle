using System;
using AnimalBattle.Core;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Core;

public class EntityPoolTests
{
    [Fact]
    public void EntityPool_PreallocatesAndReusesEntities_WithoutDestruct()
    {
        using World world = World.Create();
        using EntityPool pool = new(world, initialCapacity: 16);

        Assert.Equal(16, pool.TotalAllocated);
        Assert.Equal(16, pool.AvailableCount);

        Entity e1 = pool.Rent();
        Assert.True(e1.IsAlive());
        Assert.Equal(15, pool.AvailableCount);

        pool.Return(e1);
        Assert.Equal(16, pool.AvailableCount);

        Entity e2 = pool.Rent();
        Assert.Equal(e1.Id, e2.Id);
        Assert.Equal(15, pool.AvailableCount);
    }

    [Fact]
    public void EntityPool_ExpandsCapacity_WhenExhausted()
    {
        using World world = World.Create();
        using EntityPool pool = new(world, initialCapacity: 4);

        Entity[] rented = new Entity[5];
        for (int i = 0; i < 5; i++)
        {
            rented[i] = pool.Rent();
            Assert.True(rented[i].IsAlive());
        }

        Assert.True(pool.TotalAllocated >= 5);
        Assert.True(pool.AvailableCount > 0);

        for (int i = 0; i < 5; i++)
        {
            pool.Return(rented[i]);
        }

        Assert.True(pool.AvailableCount >= 5);
    }
}
