using AnimalBattle.Components;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     汎用武器射撃システム。
///     <see cref="CharacterWeaponIntent"/> を消費し、射撃クールダウンの管理および弾丸エンティティの射出を行います。
/// </summary>
public static class CharacterWeaponSystem
{
    /// <summary>
    ///     単一エンティティの武器状態を 1 フレーム分更新し、意図に応じて弾丸を発射します。
    /// </summary>
    /// <param name="entity">射撃主体の Flecs エンティティ句柄。</param>
    /// <param name="dt">デルタタイム（秒）。</param>
    /// <param name="world">弾丸生成用の Flecs ワールド。</param>
    public static void UpdateEntity(Entity entity, float dt, World world)
    {
        if (!entity.IsAlive() ||
            !entity.Has<WeaponState>() ||
            !entity.Has<CharacterWeaponIntent>() ||
            !entity.Has<Transform>())
            return;

        ref var weapon = ref entity.GetMut<WeaponState>();
        ref var intent = ref entity.GetMut<CharacterWeaponIntent>();
        ref readonly var trans = ref entity.Get<Transform>();

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
            CombatEntityPoolHelper.RentPlayerBullet(world)
                .Set(new Transform(spawnX, spawnY))
                .Set(new Velocity(vx, vy))
                .Set(new CollisionBox(weapon.BulletWidth, weapon.BulletHeight))
                .Set(new Hitbox(new CollisionShape.Box(weapon.BulletWidth, weapon.BulletHeight), weapon.BulletDamage))
                .Add<PlayerBulletTag>();

            weapon.CooldownRemaining = weapon.CooldownDuration;
            intent.ShootTriggered = false;
        }
    }
}
