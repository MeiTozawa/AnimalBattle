using AnimalBattle.Configs;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     弾の等速直線移動および画面外消滅判定を一括制御する ECS System。
/// </summary>
public static class BulletPhysicsSystem {
    /// <summary>
    ///     弾物理システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    public static World AddBulletPhysicsSystem(this World world)
    {
        world.System<Transform, Velocity>("PlayerBulletMovementSystem")
           .With<PlayerBulletTag>()
           .Without<RecycleTag>()
           .Kind(Ecs.OnUpdate)
           .Iter(RunBatchPlayerBulletMovement);

        world.System<Transform, Velocity, OrbitSatellite>("BossBulletMovementSystem")
           .With<BossBulletTag>()
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

            var nextX = t.Position.X + v.Vx * dt;
            var nextY = t.Position.Y + v.Vy * dt;
            t.Position = new Vec2f(nextX, nextY);

            if (nextX < minX || nextX > maxX ||
                nextY < minY || nextY > maxY)
                it.Entity(i).Add<RecycleTag>();
        }
    }

    private static void RunBatchBossBulletMovement(Iter it, Field<Transform> trans, Field<Velocity> vel, Field<OrbitSatellite> satellites)
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
            if (satellites[i].IsActive) continue;

            ref var t = ref trans[i];
            ref readonly var v = ref vel[i];

            var nextX = t.Position.X + v.Vx * dt;
            var nextY = t.Position.Y + v.Vy * dt;
            t.Position = new Vec2f(nextX, nextY);

            if (nextX < minX || nextX > maxX ||
                nextY < minY || nextY > maxY)
                it.Entity(i).Add<RecycleTag>();
        }
    }
}