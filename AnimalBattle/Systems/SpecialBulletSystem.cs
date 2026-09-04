using System;
using AnimalBattle.Components;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// 特殊弾幕（品紅・黄・青・黒）の周期アニメーション、法線正弦波揺動、および動的拡縮判定を
/// 一括制御する ECS System。
/// </summary>
public static class SpecialBulletSystem
{
    private const float CyanWaveAmplitude = 28.0f;
    private const float CyanWaveFrequency = 7.0f;
    private const float MagentaPulseAmplitude = 0.35f;
    private const float MagentaPulseFrequency = 6.0f;

    /// <summary>
    /// 特殊弾幕変調システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド。</param>
    /// <returns>メソッドチェーン用の <see cref="World"/>。</returns>
    public static World AddSpecialBulletSystem(this World world)
    {
        world.System<Velocity, BulletModifier, Bullet, Hitbox>("SpecialBulletSystem")
             .With<BossBulletTag>()
             .Kind(Ecs.OnUpdate)
             .Each((Iter it, int i, ref Velocity vel, ref BulletModifier modifier, ref Bullet bullet, ref Hitbox hitbox) =>
             {
                 float dt = it.DeltaTime();
                 modifier.ElapsedTimer += dt;
                 float t = modifier.ElapsedTimer;

                 // 1. 青色 & 黒色：進行方向に対する法線方向の左右正弦波揺動
                 if (modifier.Type == BulletType.Cyan || modifier.Type == BulletType.Black)
                 {
                     float baseSpeedSq = modifier.BaseVx * modifier.BaseVx + modifier.BaseVy * modifier.BaseVy;
                     if (baseSpeedSq > 1.0f)
                     {
                         float invSpeed = 1.0f / MathF.Sqrt(baseSpeedSq);
                         float nx = -modifier.BaseVy * invSpeed;
                         float ny = modifier.BaseVx * invSpeed;

                         float waveVelocity = CyanWaveAmplitude * CyanWaveFrequency * MathF.Cos(t * CyanWaveFrequency);
                         vel.Vx = modifier.BaseVx + nx * waveVelocity;
                         vel.Vy = modifier.BaseVy + ny * waveVelocity;
                     }
                 }

                 // 2. 品紅色 & 黒色：呼吸拡縮アニメーション（当たり判定 Circle 同期）
                 if (modifier.Type == BulletType.Magenta || modifier.Type == BulletType.Black)
                 {
                     float scale = 1.0f + MagentaPulseAmplitude * MathF.Sin(t * MagentaPulseFrequency);
                     float newRadius = Math.Max(2.0f, modifier.BaseRadius * scale);
                     bullet.Radius = newRadius;
                     hitbox.Shape = new CollisionShape.Circle(newRadius);
                 }
             });

        return world;
    }
}
