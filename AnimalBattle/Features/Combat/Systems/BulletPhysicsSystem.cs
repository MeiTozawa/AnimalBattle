namespace AnimalBattle.Combat.Systems;

/// <summary>
///     弾の等速直線移動および画面外デスポーン判定を一括制御する ECS システム。
/// </summary>
public static class BulletPhysicsSystem {
    /// <summary>
    ///     弾物理システムを Flecs.NET ワールドへ登録します。
    ///     自機弾（PlayerBulletTag）および通常 Boss 弾（BossBulletTag、周回衛星弾 OrbitOf を除く）の直線等速移動を統一管理します。
    /// </summary>
    public static World AddBulletPhysicsSystem(this World world)
    {
        world.System<Transform, Velocity>("BulletMovementSystem")
           .With<PlayerBulletTag>()
           .Or()
           .With<BossBulletTag>()
           .Without<OrbitOf>(Ecs.Wildcard)
           .Without<RecycleTag>()
           .Kind(Ecs.OnUpdate)
           .Iter(RunBatchBulletMovement);

        return world;
    }

    private static void RunBatchBulletMovement(Iter it, Field<Transform> trans, Field<Velocity> vel)
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
