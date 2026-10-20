using AnimalBattle.Configs;
using Flecs.NET.Core;

namespace AnimalBattle.Combat.Systems;

/// <summary>
///     蠑ｾ縺ｮ遲蛾溽峩邱夂ｧｻ蜍輔♀繧医・逕ｻ髱｢螟匁ｶ域ｻ・愛螳壹ｒ荳諡ｬ蛻ｶ蠕｡縺吶ｋ ECS System縲・
/// </summary>
public static class BulletPhysicsSystem {
    /// <summary>
    ///     蠑ｾ迚ｩ逅・す繧ｹ繝・Β繧・Flecs.NET 繝ｯ繝ｼ繝ｫ繝峨∈逋ｻ骭ｲ縺励∪縺吶・
    /// </summary>
    public static World AddBulletPhysicsSystem(this World world)
    {
        world.System<Transform, Velocity>("PlayerBulletMovementSystem")
           .With<PlayerBulletTag>()
           .Without<RecycleTag>()
           .Kind(Ecs.OnUpdate)
           .Iter(RunBatchPlayerBulletMovement);

        world.System<Transform, Velocity>("BossBulletMovementSystem")
           .With<BossBulletTag>()
           .Without<OrbitOf>(Ecs.Wildcard)
           .Without<RecycleTag>()
           .Kind(Ecs.OnUpdate)
           .Iter(RunBatchBossBulletMovement);

        return world;
    }

    private static void RunBatchPlayerBulletMovement(Iter it, Field<Transform> trans, Field<Velocity> vel)
    {
        var count = it.Count();
        if (count <= 0) return;

        var dt = it.DeltaTime();
        ref readonly var arenaCfg = ref it.World().Get<ArenaConfig>();
        var minX = arenaCfg.BulletDespawnMinX;
        var maxX = arenaCfg.BulletDespawnMaxX;
        var minY = arenaCfg.BulletDespawnMinY;
        var maxY = arenaCfg.BulletDespawnMaxY;

        for (var i = 0; i < count; ++i)
        {
            ref var t = ref trans[i];
            ref readonly var v = ref vel[i];

            var nextPos = t.Position + v.Value * dt;
            t.Position = nextPos;

            if (nextPos.X < minX || nextPos.X > maxX ||
                nextPos.Y < minY || nextPos.Y > maxY)
                it.Entity(i).Add<RecycleTag>();
        }
    }

    private static void RunBatchBossBulletMovement(Iter it, Field<Transform> trans, Field<Velocity> vel)
    {
        var count = it.Count();
        if (count <= 0) return;

        var dt = it.DeltaTime();
        ref readonly var arenaCfg = ref it.World().Get<ArenaConfig>();
        var minX = arenaCfg.BulletDespawnMinX;
        var maxX = arenaCfg.BulletDespawnMaxX;
        var minY = arenaCfg.BulletDespawnMinY;
        var maxY = arenaCfg.BulletDespawnMaxY;

        for (var i = 0; i < count; ++i)
        {
            ref var t = ref trans[i];
            ref readonly var v = ref vel[i];

            var nextPos = t.Position + v.Value * dt;
            t.Position = nextPos;

            if (nextPos.X < minX || nextPos.X > maxX ||
                nextPos.Y < minY || nextPos.Y > maxY)
                it.Entity(i).Add<RecycleTag>();
        }
    }
}
