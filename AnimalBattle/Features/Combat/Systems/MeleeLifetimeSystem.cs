using AnimalBattle.Components;
using AnimalBattle.Framework.Pooling;
using Flecs.NET.Core;

namespace AnimalBattle.Combat.Systems;

/// <summary>
///     近接攻撃判定等の持続時間を監視し、満了時に安全に回収タグ（RecycleTag）を付与する ECS システム。
/// </summary>
public static class MeleeLifetimeSystem
{
    /// <summary>
    ///     近接判定寿命更新システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    public static World AddMeleeLifetimeSystem(this World world)
    {
        world.System<HitboxLifetime>("MeleeLifetimeSystem")
            .Without<RecycleTag>()
            .Kind(Ecs.OnUpdate)
            .Iter(ExecuteMeleeLifetime);

        return world;
    }

    /// <summary>
    ///     近接判定寿命更新パイプライン（名前付き静的メソッド）。
    /// </summary>
    public static void ExecuteMeleeLifetime(Iter it, Field<HitboxLifetime> lifetimes)
    {
        var count = it.Count();
        if (count <= 0) return;

        var dt = it.DeltaTime();
        for (var i = 0; i < count; i++)
        {
            ref var lt = ref lifetimes[i];
            lt.ElapsedTimer += dt;
            if (lt.ElapsedTimer >= lt.Duration)
            {
                it.Entity(i).Add<RecycleTag>();
            }
        }
    }
}
