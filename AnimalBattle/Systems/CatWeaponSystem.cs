using AnimalBattle.Components;
using AnimalBattle.Events;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

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
            .Iter((Iter it,
                   Field<Transform> transforms,
                   Field<WeaponIntent> intents,
                   Field<CatWeaponState> weapons,
                   Field<SpringScale> springs) =>
            {
                var dt = it.DeltaTime();
                var count = it.Count();

                for (var i = 0; i < count; i++)
                {
                    ref var trans = ref transforms[i];
                    ref var intent = ref intents[i];
                    ref var weapon = ref weapons[i];
                    ref var spring = ref springs[i];
                    var entity = it.Entity(i);

                    ExecuteCatWeapon(entity, ref trans, ref intent, ref weapon, ref spring, dt, world);
                }
            });

        return world;
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
            var dirX = intent.AimDirectionX;
            var dirY = intent.AimDirectionY;

            // 射撃方向が未指定の場合は右向きをデフォルトとする
            if (MathF.Abs(dirX) < 0.001f && MathF.Abs(dirY) < 0.001f)
            {
                dirX = 1.0f;
            }

            var spawnX = trans.X + dirX * weapon.BulletSpawnOffset;
            var spawnY = trans.Y + weapon.BulletSpawnOffsetY + dirY * weapon.BulletSpawnOffset;

            var vx = dirX * weapon.BulletSpeed;
            var vy = dirY * weapon.BulletSpeed;

            // 事前確保されたエンティティプールから自機弾丸を貸出
            CombatPoolService.RentPlayerBullet()
                .Set(new Transform(spawnX, spawnY))
                .Set(new Velocity(vx, vy))
                .Set(new CollisionBox(weapon.BulletWidth, weapon.BulletHeight))
                .Set(new Hitbox(new CollisionShape.Box(weapon.BulletWidth, weapon.BulletHeight), weapon.BulletDamage))
                .Add<PlayerBulletTag>();

            // 銃口閃光（Muzzle Flash）エフェクト生成
            CombatSpawner.SpawnMuzzleFlash(world, spawnX, spawnY);

            // 自機射撃微後退反動（Recoil Impulse & Squash/Stretch）
            trans.X -= dirX * weapon.ShootRecoilDistance;
            trans.Y -= dirY * weapon.ShootRecoilDistance;

            spring.CurrentX = weapon.ShootRecoilStretchX;
            spring.CurrentY = weapon.ShootRecoilStretchY;
            spring.VelX = 0.0f;
            spring.VelY = 0.0f;

            EventBus.Publish(new PlayerShotFiredEvent(spawnX, spawnY, dirX, dirY));

            weapon.CooldownRemaining = weapon.CooldownDuration;
            intent.ShootTriggered = false;
        }
    }
}
