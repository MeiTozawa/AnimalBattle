using System;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Combat.Systems;

/// <summary>
///     蜻ｨ蝗櫁｡帶弌蠑ｾ蟷包ｼ・see cref="OrbitSatellite" />・峨・蜈ｬ霆｢霆碁％蠎ｧ讓吶♀繧医・謗･邱夐溷ｺｦ繧定ｨ育ｮ励☆繧句腰荳雋ｬ莉ｻ ECS 繧ｷ繧ｹ繝・Β縲・///     隕ｪ蠑ｾ縺梧ｶ域ｻ・∫┌蜉ｹ蛹悶√∪縺溘・繝励・繝ｫ縺ｸ霑泌唆繝ｻ蜀榊茜逕ｨ縺輔ｌ縺溷ｴ蜷医∬｡帶弌蠑ｾ繧定・蜍慕噪縺ｫ蝗槫庶繝励・繝ｫ縺ｸ霑泌唆縺励∪縺吶・/// </summary>
public static class OrbitSatelliteMovementSystem
{
    /// <summary>
    ///     蜻ｨ蝗櫁｡帶弌蠑ｾ蟷慕ｧｻ蜍輔す繧ｹ繝・Β繧・Flecs.NET 繝ｯ繝ｼ繝ｫ繝峨∈逋ｻ骭ｲ縺励∪縺吶・    /// </summary>
    /// <param name="world">蟇ｾ雎｡縺ｮ Flecs.NET 繝ｯ繝ｼ繝ｫ繝・/param>
    /// <returns>繝｡繧ｽ繝・ラ繝√ぉ繝ｼ繝ｳ逕ｨ縺ｮ <see cref="World" /></returns>
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

            if (parent.Has<PoolLeaseState>() && !parent.Get<PoolLeaseState>().IsRented)
            {
                e.Add<RecycleTag>();
                continue;
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
