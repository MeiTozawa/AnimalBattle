using AnimalBattle.Core;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Core;

/// <summary>
/// エンティティプールの最大容量ハードキャップ（熔断保護）および運用メトリクス計測を検証するテスト。
/// </summary>
public class EntityPoolMetricsTests
{
    [Fact]
    public void EntityPool_EnforcesMaxCapacity_AndRecordsAccurateMetrics()
    {
        using World world = World.Create();
        EntityPool pool = new(world, initialCapacity: 4, onInit: null, onReset: null, maxCapacity: 8);

        for (int i = 0; i < 4; i++)
        {
            pool.Rent();
        }
        Assert.Equal(4, pool.Metrics.PeakUsage);

        // 8 まで拡張
        for (int i = 0; i < 4; i++)
        {
            pool.Rent();
        }
        Assert.Equal(8, pool.Metrics.PeakUsage);
        Assert.Equal(1, pool.Metrics.ExpansionCount);

        // 最大容量上限到達による熔断（サーキットブレーカー発動）
        Entity overflow = pool.Rent();
        Assert.Equal(0UL, (ulong)overflow.Id); // OOM 防止のため空エンティティ（Id == 0）を返却して安全に熔断
        Assert.Equal(1, pool.Metrics.OverflowCount);
    }
}
