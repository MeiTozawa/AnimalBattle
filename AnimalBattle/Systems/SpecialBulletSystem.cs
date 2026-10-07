using AnimalBattle.Configs;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     特殊弾幕の周期アニメーションと動的変調を制御する ECS System。
/// </summary>
public static class SpecialBulletSystem {
    /// <summary>
    ///     特殊弾幕変調システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    public static World AddSpecialBulletSystem(this World world)
    {
        world.System<Velocity, BulletModifier, Bullet, Hitbox>("SpecialBulletSystem")
           .With<BossBulletTag>()
           .Kind(Ecs.OnUpdate)
           .Iter((Iter it, Field<Velocity> vel, Field<BulletModifier> modifier, Field<Bullet> bullet,
                Field<Hitbox> hitbox) =>
            {
                var count = it.Count();
                if (count <= 0) return;

                var dt = it.DeltaTime();
                var cfg = ConfigManager.SpecialBullets;
                var cyanAmp = cfg.CyanWaveAmplitude;
                var cyanFreq = cfg.CyanWaveFrequency;
                var magentaAmp = cfg.MagentaPulseAmplitude;
                var magentaFreq = cfg.MagentaPulseFrequency;

                for (var i = 0; i < count; ++i)
                {
                    ref var mod = ref modifier[i];
                    mod.ElapsedTimer += dt;

                    var type = mod.Type;
                    var vx = mod.BaseVx;
                    var vy = mod.BaseVy;

                    // 青弾と黒弾の法线正弦波揺動
                    if (type is BulletType.Cyan or BulletType.Black)
                    {
                        var speedSq = mod.BaseVx * mod.BaseVx + mod.BaseVy * mod.BaseVy;
                        if (speedSq > 1.0f)
                        {
                            var invSpeed = 1.0f / MathF.Sqrt(speedSq);
                            var nx = -mod.BaseVy * invSpeed;
                            var ny = mod.BaseVx * invSpeed;

                            var waveVelocity = cyanAmp * cyanFreq * MathF.Cos(mod.ElapsedTimer * cyanFreq);
                            vx = mod.BaseVx + nx * waveVelocity;
                            vy = mod.BaseVy + ny * waveVelocity;
                        }
                    }

                    ref var v = ref vel[i];
                    v.Value = new Vec2f(vx, vy);

                    // 品紅弾と黒弾の拡縮脈動
                    if (type is BulletType.Magenta or BulletType.Black)
                    {
                        var scale = 1.0f + magentaAmp * MathF.Sin(mod.ElapsedTimer * magentaFreq);
                        var radius = MathF.Max(2.0f, mod.BaseRadius * scale);

                        ref var b = ref bullet[i];
                        b.Radius = radius;
                        ref var h = ref hitbox[i];
                        h.Shape = new CollisionShape.Circle(radius);
                    }
                }
            });

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