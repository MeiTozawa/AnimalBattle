namespace AnimalBattle.Combat.Systems;

/// <summary>
///     周回衛星弾幕の公転軌道座標および接線速度を計算する単一責任 ECS システム。
///     親弾が消滅またはプールへ返却された場合、衛星弾を自動的に回収プールへ返却します。
/// </summary>
public static class OrbitSatelliteMovementSystem
{
    /// <summary>
    ///     周回衛星弾幕移動システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World" /></returns>
    public static World AddOrbitSatelliteMovementSystem(this World world)
    {
        world.System<Transform, Velocity, OrbitSatellite, BulletModifier>("OrbitSatelliteMovementSystem")
           .With<BossBulletTag>()
           .With<OrbitOf>(Ecs.Wildcard)
           .Without<RecycleTag>()
           .Kind(Ecs.OnUpdate)
           .Iter(ExecuteOrbitSatelliteMovement);

        return world;
    }

    private static void ExecuteOrbitSatelliteMovement(
        Iter it,
        Field<Transform> trans,
        Field<Velocity> vel,
        Field<OrbitSatellite> satellites,
        Field<BulletModifier> modifiers)
    {
        var count = it.Count();
        if (count <= 0) return;

        var dt = it.DeltaTime();
        var w = it.World();
        ref readonly var cfg = ref w.Get<SpecialBulletConfig>();
        var orbitSpeed = cfg.YellowOrbitSpeed;
        ref readonly var arenaCfg = ref w.Get<ArenaConfig>();
        var minX = arenaCfg.BulletDespawnMinX;
        var maxX = arenaCfg.BulletDespawnMaxX;
        var minY = arenaCfg.BulletDespawnMinY;
        var maxY = arenaCfg.BulletDespawnMaxY;

        for (var i = 0; i < count; ++i)
        {
            var e = it.Entity(i);
            var parent = e.Target<OrbitOf>();
            if (!parent.IsAlive() || !parent.Enabled() || parent.Has<RecycleTag>())
            {
                e.Add<RecycleTag>();
                continue;
            }

            // プール所属エンティティの場合の貸出フラグ確認：二重探索を回避
            if (parent.Has<PoolLeaseState>())
            {
                ref readonly var lease = ref parent.Get<PoolLeaseState>();
                if (!lease.IsRented)
                {
                    e.Add<RecycleTag>();
                    continue;
                }
            }

            ref var sat = ref satellites[i];
            ref var mod = ref modifiers[i];
            mod.ElapsedTimer += dt;

            ref readonly var parentTrans = ref parent.Get<Transform>();
            var angle = sat.InitialAngle + mod.ElapsedTimer * orbitSpeed;
            var cos = MathF.Cos(angle);
            var sin = MathF.Sin(angle);

            var satOffset = new Vec2f(cos * sat.OrbitDistance, sin * sat.OrbitDistance);
            var satPos = parentTrans.Position + satOffset;

            if (satPos.X < minX || satPos.X > maxX || satPos.Y < minY || satPos.Y > maxY)
            {
                e.Add<RecycleTag>();
                continue;
            }

            ref var t = ref trans[i];
            t.Position = satPos;

            ref readonly var parentVel = ref parent.Get<Velocity>();
            var tangentSpeed = sat.OrbitDistance * orbitSpeed;
            ref var v = ref vel[i];
            v.Value = parentVel.Value + new Vec2f(-sin * tangentSpeed, cos * tangentSpeed);
        }
    }
}
