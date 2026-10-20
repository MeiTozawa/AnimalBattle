using System;
using AnimalBattle.Combat;
using AnimalBattle.Components;
using AnimalBattle.Core;
using AnimalBattle.Core.Geometry;
using AnimalBattle.Events;
using AnimalBattle.Framework.Pooling;
using Flecs.NET.Core;

namespace AnimalBattle.Player.Systems;

/// <summary>
///     Cat 専用遠隔射撃武器システム。
///     <see cref="CatWeaponState"/> を持つエンティティの <see cref="WeaponIntent"/> を消費し、
///     射撃クールダウン管理、弾丸エンティティの射出および反動演出を実行します。
/// </summary>
public static class CatWeaponSystem
{
    /// <summary>
    ///     Cat 射撃武器システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World"/></returns>
    public static World AddCatWeaponSystem(this World world)
    {
        world.System<Transform, WeaponIntent, CatWeaponState, SpringScale>("CatWeaponSystem")
            .Kind(Ecs.OnUpdate)
            .Without<DeadTag>()
            .Iter(ExecuteCatWeaponSystem);

        return world;
    }

    private static void ExecuteCatWeaponSystem(
        Iter it,
        Field<Transform> transforms,
        Field<WeaponIntent> intents,
        Field<CatWeaponState> weapons,
        Field<SpringScale> springs)
    {
        var count = it.Count();
        if (count == 0) return;

        var dt = it.DeltaTime();
        var world = it.World();

        for (var i = 0; i < count; i++)
        {
            ref var trans = ref transforms[i];
            ref var intent = ref intents[i];
            ref var weapon = ref weapons[i];
            ref var spring = ref springs[i];
            var entity = it.Entity(i);

            ExecuteCatWeapon(entity, ref trans, ref intent, ref weapon, ref spring, dt, world);
        }
    }

    private static void ExecuteCatWeapon(
        Entity entity,
        ref Transform trans,
        ref WeaponIntent intent,
        ref CatWeaponState weapon,
        ref SpringScale spring,
        float dt,
        World world)
    {
        // 1. クールダウンタイマーの減衰
        if (weapon.CooldownRemaining > 0.0f)
        {
            weapon.CooldownRemaining = Math.Max(0.0f, weapon.CooldownRemaining - dt);
        }

        // 2. 射撃トリガーの判定と実行
        if (intent.ShootTriggered && weapon.CooldownRemaining <= 0.0f)
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

            var spawnPos = trans.Position + new Vec2f(0.0f, weapon.BulletSpawnOffset.Y) + dir * weapon.BulletSpawnOffset.X;
            var vel = dir * weapon.BulletSpeed;

            // 事前確保されたエンティティプールから自機弾丸を貸出
            var bulletEntity = CombatPoolService.RentPlayerBullet();
            bulletEntity
                .Set(new Transform(spawnPos))
                .Set(new Velocity(vel))
                .Set(new Hitbox(new CollisionShape.Box(weapon.BulletSize), weapon.BulletDamage));

            // 銃口閃光（Muzzle Flash）エフェクト生成
            EffectSpawner.SpawnMuzzleFlash(world, spawnPos);

            // 自機射撃微後退反動（Recoil Impulse & Squash/Stretch）
            trans.Position -= dir * weapon.ShootRecoilDistance;

            spring.Current = weapon.ShootRecoilStretch;
            spring.Velocity = Vec2f.Zero;

            EventBus.Publish(new PlayerShotFiredEvent(spawnPos, dir));

            weapon.CooldownRemaining = weapon.CooldownDuration;
            intent.ShootTriggered = false;
        }
    }
}
