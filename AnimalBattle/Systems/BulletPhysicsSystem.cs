using AnimalBattle.Configs;
using AnimalBattle.Core;
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
           .Kind(Ecs.OnUpdate)
           .Iter(RunBatchBulletMovement);

        world.System<Transform, Velocity>("BossBulletMovementSystem")
           .With<BossBulletTag>()
           .Kind(Ecs.OnUpdate)
           .Iter(RunBatchBulletMovement);

        return world;
    }

    private static void RunBatchBulletMovement(Iter it, Field<Transform> trans, Field<Velocity> vel)
    {
        var count = it.Count();
        if (count <= 0) return;

        var dt = it.DeltaTime();
        var arenaCfg = ConfigManager.Arena;
        var minX = arenaCfg.BulletDespawnMinX;
        var maxX = arenaCfg.BulletDespawnMaxX;
        var minY = arenaCfg.BulletDespawnMinY;
        var maxY = arenaCfg.BulletDespawnMaxY;

        var w = it.World();

        // 原地移動と画面外判定を一括処理
        for (var i = 0; i < count; ++i)
        {
            ref var t = ref trans[i];
            ref readonly var v = ref vel[i];

            var nextX = t.Position.X + v.Vx * dt;
            var nextY = t.Position.Y + v.Vy * dt;
            t.Position = new Vec2f(nextX, nextY);

            if (nextX < minX || nextX > maxX ||
                nextY < minY || nextY > maxY)
            {
                CombatEntityPoolHelper.Recycle(w, it.Entity(i));
            }
        }
    }
}