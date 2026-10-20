using System;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Combat.Systems;

/// <summary>
///     特殊弾幕（青弾・品紅弾・黒弾）の周期アニメーションと動的変調（正弦波揺動・拡縮脈動）を制御する単一責任 ECS システム。
/// </summary>
public static class SpecialBulletSystem {
    /// <summary>
    ///     特殊弾幕変調システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World" /></returns>
    public static World AddSpecialBulletSystem(this World world)
    {
        world.System<Velocity, BulletModifier, Bullet, Hitbox>("SpecialBulletSystem")
           .With<BossBulletTag>()
           .Kind(Ecs.OnUpdate)
           .Iter(ExecuteSpecialBulletUpdate);

        return world;
    }

    private static void ExecuteSpecialBulletUpdate(
        Iter it,
        Field<Velocity> vel,
        Field<BulletModifier> modifier,
        Field<Bullet> bullet,
        Field<Hitbox> hitbox)
    {
        var count = it.Count();
        if (count <= 0) return;

        var dt = it.DeltaTime();
        ref readonly var cfg = ref it.World().Get<SpecialBulletConfig>();
        var cyanAmp = cfg.CyanWaveAmplitude;
        var cyanFreq = cfg.CyanWaveFrequency;
        var magentaAmp = cfg.MagentaPulseAmplitude;
        var magentaFreq = cfg.MagentaPulseFrequency;

        for (var i = 0; i < count; ++i)
        {
            ref var mod = ref modifier[i];
            if (mod.Type == BulletType.Normal) continue;

            mod.ElapsedTimer += dt;

            var type = mod.Type;
            var currentVel = mod.BaseVel;

            // 青弾と黒弾の法線正弦波揺動
            if (type is BulletType.Cyan or BulletType.Black)
            {
                var speedSq = mod.BaseVel.LengthSquared();
                if (speedSq > 1.0f)
                {
                    var invSpeed = 1.0f / MathF.Sqrt(speedSq);
                    var normal = new Vec2f(-mod.BaseVel.Y * invSpeed, mod.BaseVel.X * invSpeed);

                    var waveVelocity = cyanAmp * cyanFreq * MathF.Cos(mod.ElapsedTimer * cyanFreq);
                    currentVel = mod.BaseVel + normal * waveVelocity;
                }
            }

            ref var v = ref vel[i];
            v.Value = currentVel;

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
    }
}