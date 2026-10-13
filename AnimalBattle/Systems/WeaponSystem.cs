using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Events;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     汎用武器射撃システム。
///     <see cref="WeaponIntent"/> を消費し、射撃クールダウンの管理および弾丸エンティティの射出を行います。
/// </summary>
public static class WeaponSystem
{
    /// <summary>
    ///     武器射撃システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World"/></returns>
    public static World AddWeaponSystem(this World world)
    {
        world.System<Transform, WeaponIntent, WeaponState, SpringScale>("WeaponSystem")
            .Kind(Ecs.OnUpdate)
            .Without<DeadTag>()
            .Iter((Iter it,
                   Field<Transform> transforms,
                   Field<WeaponIntent> intents,
                   Field<WeaponState> weapons,
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

                    ExecuteWeapon(ref trans, ref intent, ref weapon, ref spring, dt, world);
                }
            });

        return world;
    }

    /// <summary>
    ///     単一エンティティの武器状態を 1 フレーム分更新し、意図に応じて弾丸を発射します（テストおよび直接呼び出し互換用）。
    /// </summary>
    /// <param name="entity">射撃主体の Flecs エンティティ句柄。</param>
    /// <param name="dt">デルタタイム（秒）。</param>
    /// <param name="world">弾丸生成用の Flecs ワールド。</param>
    public static void UpdateEntity(Entity entity, float dt, World world)
    {
        if (!entity.IsAlive()) return;

        ref var trans = ref entity.GetMut<Transform>();
        ref var intent = ref entity.GetMut<WeaponIntent>();
        ref var weapon = ref entity.GetMut<WeaponState>();
        ref var spring = ref entity.GetMut<SpringScale>();

        ExecuteWeapon(ref trans, ref intent, ref weapon, ref spring, dt, world);
    }

    private static void ExecuteWeapon(
        ref Transform trans,
        ref WeaponIntent intent,
        ref WeaponState weapon,
        ref SpringScale spring,
        float dt,
        World world)
    {
        // 1. クールダウンタイマーの減衰
        if (weapon.CooldownRemaining > 0.0f)
        {
            weapon.CooldownRemaining = Math.Max(0.0f, weapon.CooldownRemaining - dt);
        }

        // 2. 射撃トリガーの判定と弾丸発射
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
            var pCfg = ConfigManager.Player;
            trans.X -= dirX * pCfg.ShootRecoilDistance;
            trans.Y -= dirY * pCfg.ShootRecoilDistance;

            spring.CurrentX = pCfg.ShootRecoilStretchX;
            spring.CurrentY = pCfg.ShootRecoilStretchY;
            spring.VelX = 0.0f;
            spring.VelY = 0.0f;

            EventBus.Publish(new PlayerShotFiredEvent(spawnX, spawnY, dirX, dirY));

            weapon.CooldownRemaining = weapon.CooldownDuration;
            intent.ShootTriggered = false;
        }
    }
}
