using AnimalBattle.Core;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Core;

/// <summary>
/// エンティティプールのライフサイクル閉環、および貸出中（In-Flight）エンティティの破棄漏れ防止を検証するテスト。
/// </summary>
public class EntityPoolLifecycleTests
{
    [Fact]
    public void Dispose_DestructsBothAvailableAndInFlightEntities()
    {
        using World world = World.Create();
        EntityPool pool = new(world, initialCapacity: 16);

        // 貸出中（In-Flight）のエンティティを生成
        Entity rented1 = pool.Rent();
        Entity rented2 = pool.Rent();

        Assert.True(rented1.IsAlive());
        Assert.True(rented2.IsAlive());
        Assert.Equal(14, pool.AvailableCount);

        // プールを破棄。貸出中のエンティティも確実に破棄されることを検証
        pool.Dispose();

        Assert.False(rented1.IsAlive());
        Assert.False(rented2.IsAlive());
        Assert.Equal(0, pool.AvailableCount);
    }

    [Fact]
    public void Return_AfterDispose_SafelyDestructsEntity_WithoutLeaking()
    {
        using World world = World.Create();
        EntityPool pool = new(world, initialCapacity: 8);

        Entity rented = pool.Rent();
        pool.Dispose();

        // 破棄後に遅れて返却された場合でも、リークせずに安全に Destruct される
        pool.Return(rented);
        Assert.False(rented.IsAlive());
    }
}
