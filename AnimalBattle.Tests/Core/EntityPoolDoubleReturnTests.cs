using AnimalBattle.Core;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Core;

/// <summary>
/// エンティティの二重返却（Double-Return）防止、および幽霊実体の発生抑止を検証するテスト。
/// </summary>
public class EntityPoolDoubleReturnTests
{
    [Fact]
    public void Return_SameEntityTwice_IsIdempotentAndDoesNotCorruptPool()
    {
        using World world = World.Create();
        EntityPool pool = new(world, initialCapacity: 16);

        Entity rented = pool.Rent();
        Assert.Equal(15, pool.AvailableCount);

        // 正常返却
        pool.Return(rented);
        Assert.Equal(16, pool.AvailableCount);

        // 重複返却：二重加算や多重プッシュが防止され、安全に無視される
        pool.Return(rented);
        Assert.Equal(16, pool.AvailableCount);

        // 再度貸出を行ったとき、同じエンティティが二重貸出されないことを検証
        Entity e1 = pool.Rent();
        Entity e2 = pool.Rent();
        Assert.NotEqual(e1.Id, e2.Id);
    }
}
