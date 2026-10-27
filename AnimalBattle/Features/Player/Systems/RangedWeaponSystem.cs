using System;
using AnimalBattle.Combat;
using AnimalBattle.Components;
using AnimalBattle.Core;
using AnimalBattle.Core.Geometry;
using AnimalBattle.Events;
using AnimalBattle.Framework.Pooling;
using AnimalBattle.Player.Components;
using AnimalBattle.Presentation.Vfx;
using Flecs.NET.Core;

namespace AnimalBattle.Player.Systems;

/// <summary>
///     遠距離投射武器機構システム。
///     <see cref="RangedWeaponComponent"/> を持つエンティティの <see cref="WeaponIntent"/> を消費し、
///     クールダウン管理、弾丸射出および射撃反動演出を実行します。
/// </summary>
public static class RangedWeaponSystem
{
    /// <summary>
    ///     遠距離武器システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World"/></returns>
    public static World AddRangedWeaponSystem(this World world)
    {
        world.System<Transform, Velocity, WeaponIntent, RangedWeaponComponent, SpringScale, CollisionBox>("RangedWeaponSystem")
            .Kind(Ecs.OnUpdate)
            .Without<DeadTag>()
            .Iter(ExecuteRangedWeaponSystem);

        return world;
    }

    private static void ExecuteRangedWeaponSystem(
        Iter it,
        Field<Transform> transforms,
        Field<Velocity> velocities,
        Field<WeaponIntent> intents,
        Field<RangedWeaponComponent> weapons,
        Field<SpringScale> springs,
        Field<CollisionBox> collisionBoxes)
    {
        var count = it.Count();
        if (count == 0) return;

        var dt = it.DeltaTime();
        var world = it.World();

        for (var i = 0; i < count; i++)
        {
            ref var trans = ref transforms[i];
            ref var vel = ref velocities[i];
            ref var intent = ref intents[i];
            ref var weapon = ref weapons[i];
            ref var spring = ref springs[i];
            ref readonly var col = ref collisionBoxes[i];
            var entity = it.Entity(i);

            ExecuteRangedWeapon(entity, ref trans, ref vel, ref intent, ref weapon, ref spring, in col, dt, world);
        }
    }

    private static void ExecuteRangedWeapon(
        Entity entity,
        ref Transform trans,
        ref Velocity vel,
        ref WeaponIntent intent,
        ref RangedWeaponComponent weapon,
        ref SpringScale spring,
        in CollisionBox col,
        float dt,
        World world)
    {
        // 1. クールダウンタイマーの減衰
        if (weapon.Timer > 0.0f)
        {
            weapon.Timer = Math.Max(0.0f, weapon.Timer - dt);
        }

        // 2. 射撃トリガーの判定と実行
        if (intent.ShootTriggered && weapon.Timer <= 0.0f)
        {
            var dir = intent.AimDirection;

            // 射撃方向が未指定の場合は右向きをデフォルトとする
            if (dir.LengthSquared() < 0.0001f)
            {
                dir = new Vec2f(1.0f, 0.0f);
            }
            else
            {
                dir = dir.Normalized();
            }

            var spawnPos = trans.Position + new Vec2f(0.0f, weapon.SpawnOffset.Y) + dir * weapon.SpawnOffset.X;
            var bulletVel = dir * weapon.BulletSpeed;

            // 事前確保されたエンティティプールから自機弾丸を貸出
            var bulletEntity = CombatPoolService.RentPlayerBullet();
            bulletEntity
                .Set(new Transform(spawnPos))
                .Set(new Velocity(bulletVel))
                .Set(new Hitbox(new CollisionShape.Box(weapon.BulletSize), weapon.BulletDamage));

            // 銃口閃光：Muzzle Flashエフェクト生成
            EffectSpawner.SpawnMuzzleFlash(world, spawnPos, weapon.MuzzleFlashRadius, weapon.MuzzleFlashDuration);

            // 自機射撃反動：外力物理インパルスとして付与
            var recoilSpeed = weapon.RecoilImpulse;
            var recoilDir = -dir;
            var recoilY = (col.IsGrounded && recoilDir.Y > 0.0f) ? 0.0f : recoilDir.Y * recoilSpeed;
            var recoilImpulse = new Vec2f(recoilDir.X * recoilSpeed, recoilY);
            vel.Knockback += recoilImpulse;

            spring.Current = weapon.RecoilStretch;
            spring.Velocity = Vec2f.Zero;

            EventBus.Publish(new PlayerShotFiredEvent(spawnPos, dir));

            weapon.Timer = weapon.Cooldown;
            intent.ShootTriggered = false;
        }
    }
}
