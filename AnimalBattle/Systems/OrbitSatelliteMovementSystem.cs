using System;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     周回衛星弾幕（<see cref="OrbitSatellite" />）の公転軌道座標および接線速度を計算する単一責任 ECS システム。
///     親弾が消滅または無効化された場合、衛星弾を自動的に回収プールへ返却します。
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
           .Kind(Ecs.OnUpdate)
           .Iter((Iter it, Field<Transform> trans, Field<Velocity> vel, Field<OrbitSatellite> satellites,
                Field<BulletModifier> modifiers) =>
            {
                var count = it.Count();
                if (count <= 0) return;

                var dt = it.DeltaTime();
                var cfg = ConfigManager.SpecialBullets;
                var orbitSpeed = cfg.YellowOrbitSpeed;
                var w = it.World();

                for (var i = 0; i < count; ++i)
                {
                    ref var sat = ref satellites[i];
                    if (!sat.Parent.IsAlive() || !sat.Parent.Has<BossBulletTag>())
                    {
                        CombatEntityPoolHelper.Recycle(w, it.Entity(i));
                        continue;
                    }

                    ref var mod = ref modifiers[i];
                    mod.ElapsedTimer += dt;

                    ref readonly var parentTrans = ref sat.Parent.Get<Transform>();
                    var angle = sat.InitialAngle + mod.ElapsedTimer * orbitSpeed;
                    var cos = MathF.Cos(angle);
                    var sin = MathF.Sin(angle);

                    var satX = parentTrans.X + cos * sat.OrbitDistance;
                    var satY = parentTrans.Y + sin * sat.OrbitDistance;

                    ref var t = ref trans[i];
                    t.Position = new Vec2f(satX, satY);

                    ref readonly var parentVel = ref sat.Parent.Get<Velocity>();
                    var tangentSpeed = sat.OrbitDistance * orbitSpeed;
                    ref var v = ref vel[i];
                    v.Value = new Vec2f(parentVel.Vx - sin * tangentSpeed, parentVel.Vy + cos * tangentSpeed);
                }
            });

        return world;
    }
}
