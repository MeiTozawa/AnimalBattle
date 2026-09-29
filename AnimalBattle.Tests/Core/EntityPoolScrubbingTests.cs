using AnimalBattle.Components;
using AnimalBattle.Core;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Core;

/// <summary>
/// エンティティプールの状態初期化・クリーンアップ（Scrubbing）契約およびダーティデータ汚染防止を検証するテスト。
/// </summary>
public class EntityPoolScrubbingTests
{
    [Fact]
    public void Rent_ExecutesOnReset_AndScrubsTransientDirtyComponents()
    {
        using World world = World.Create();
        EntityPool pool = new(world, initialCapacity: 8, onInit: null, onReset: e =>
        {
            if (e.Has<HitFlashEffect>())
            {
                e.Remove<HitFlashEffect>();
            }
            if (e.Has<Velocity>())
            {
                e.Set(new Velocity(0, 0));
            }
        });

        Entity e = pool.Rent();
        e.Set(new Velocity(500, 300));
        e.Set(new HitFlashEffect(1.0f));

        pool.Return(e);

        // 再度貸出された際、クリーンな初期状態にリセットされていることを検証
        Entity recycled = pool.Rent();
        Assert.False(recycled.Has<HitFlashEffect>());
        Assert.Equal(0.0f, recycled.Get<Velocity>().Vx);
        Assert.Equal(0.0f, recycled.Get<Velocity>().Vy);
    }
}
