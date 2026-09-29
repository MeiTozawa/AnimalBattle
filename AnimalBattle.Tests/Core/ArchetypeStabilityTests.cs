using AnimalBattle.Core;
using AnimalBattle.Systems;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Core;

/// <summary>
/// 特殊化されたエフェクトプールにおいて、貸出・更新時に Archetype の動的変動（Table Move）が発生しないことを検証するテスト。
/// </summary>
public class ArchetypeStabilityTests
{
    [Fact]
    public void SpecializedEffectPools_MaintainHomogeneousArchetypes_WithoutTableMoves()
    {
        using World world = World.Create();
        CombatEntityPoolComponent pool = world.GetCombatEntityPool();

        // 1. Spark 特効貸出と安定性検証
        Entity spark = pool.RentSparkEffect();
        string initialSparkType = spark.Type().Str();

        CombatEffectHelper.SpawnHitSparkEffect(world, 100, 200);

        Entity nextSpark = pool.RentSparkEffect();
        Assert.Equal(initialSparkType, nextSpark.Type().Str());

        // 2. Ring 特効貸出と安定性検証
        Entity ring = pool.RentRingEffect();
        string initialRingType = ring.Type().Str();

        CombatEffectHelper.SpawnJumpRingEffect(world, 150, 250);

        Entity nextRing = pool.RentRingEffect();
        Assert.Equal(initialRingType, nextRing.Type().Str());

        // 3. Area 特効貸出と安定性検証
        Entity area = pool.RentAreaEffect();
        string initialAreaType = area.Type().Str();

        CombatEffectHelper.SpawnMeleeSlashHitbox(world, 200, 300, 0, 0, 50, 50, 10, 0.5f, System.Drawing.Color.Red);

        Entity nextArea = pool.RentAreaEffect();
        Assert.Equal(initialAreaType, nextArea.Type().Str());
    }
}
