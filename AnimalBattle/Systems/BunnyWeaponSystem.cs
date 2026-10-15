using AnimalBattle.Components;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     Bunny 専用近接打撃武器システム。
///     <see cref="BunnyWeaponState"/> を持つエンティティの <see cref="WeaponIntent"/> を消費し、
///     近接打撃クールダウン管理、近接判定・斬撃エフェクトの生成および前進突進・スプリング演出を実行します。
/// </summary>
public static class BunnyWeaponSystem
{
    /// <summary>
    ///     Bunny 近接武器システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World"/></returns>
    public static World AddBunnyWeaponSystem(this World world)
    {
        world.System<Transform, WeaponIntent, BunnyWeaponState, SpringScale>("BunnyWeaponSystem")
            .Kind(Ecs.OnUpdate)
            .Without<DeadTag>()
            .Iter((Iter it,
                   Field<Transform> transforms,
                   Field<WeaponIntent> intents,
                   Field<BunnyWeaponState> weapons,
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

                    ExecuteBunnyWeapon(entity, ref trans, ref intent, ref weapon, ref spring, dt, world);
                }
            });

        return world;
    }

    /// <summary>
    ///     単一エンティティの Bunny 武器状態を 1 フレーム分更新します（テストおよび直接呼び出し互換用）。
    /// </summary>
    public static void UpdateEntity(Entity entity, float dt, World world)
    {
        if (!entity.IsAlive()) return;

        ref var trans = ref entity.GetMut<Transform>();
        ref var intent = ref entity.GetMut<WeaponIntent>();
        ref var weapon = ref entity.GetMut<BunnyWeaponState>();
        ref var spring = ref entity.GetMut<SpringScale>();

        ExecuteBunnyWeapon(entity, ref trans, ref intent, ref weapon, ref spring, dt, world);
    }

    private static void ExecuteBunnyWeapon(
        Entity entity,
        ref Transform trans,
        ref WeaponIntent intent,
        ref BunnyWeaponState weapon,
        ref SpringScale spring,
        float dt,
        World world)
    {
        // 1. クールダウンタイマーの減衰
        if (weapon.CooldownRemaining > 0.0f)
        {
            weapon.CooldownRemaining = Math.Max(0.0f, weapon.CooldownRemaining - dt);
        }

        // 2. 近接攻撃トリガーの判定と実行
        if (intent.ShootTriggered && weapon.CooldownRemaining <= 0.0f)
        {
            var dirX = intent.AimDirectionX;
            var dirY = intent.AimDirectionY;

            // 攻撃方向が未指定の場合は右向きをデフォルトとする
            if (MathF.Abs(dirX) < 0.001f && MathF.Abs(dirY) < 0.001f)
            {
                dirX = 1.0f;
            }

            var spawnX = trans.X + dirX * weapon.AttackRange;
            var spawnY = trans.Y + dirY * weapon.AttackRange;

            SlashDirection slashDir;
            if (MathF.Abs(dirX) >= MathF.Abs(dirY))
            {
                slashDir = dirX >= 0.0f ? SlashDirection.Right : SlashDirection.Left;
            }
            else
            {
                slashDir = dirY >= 0.0f ? SlashDirection.Down : SlashDirection.Up;
            }

            CombatSpawner.SpawnPlayerMeleeAttack(
                world,
                spawnX,
                spawnY,
                weapon.AttackWidth,
                weapon.AttackHeight,
                weapon.AttackDamage,
                weapon.AttackDuration,
                slashDir);

            if (entity.IsAlive() && entity.Has<Animation>())
            {
                ref var anim = ref entity.GetMut<Animation>();
                anim.Play("Attack");
            }

            // 近接打撃前進インパルスおよびスプリング伸縮（bunny.toml 定義値から適用）
            trans.X += dirX * weapon.AttackLungeDistance;
            if (!entity.Get<CollisionBox>().IsGrounded)
            {
                trans.Y += dirY * weapon.AttackLungeDistance;
            }

            spring.CurrentX = weapon.AttackStretchScaleX;
            spring.CurrentY = weapon.AttackStretchScaleY;
            spring.VelX = 0.0f;
            spring.VelY = 0.0f;

            SoundService.PlaySfx(SfxType.PlayerDash);

            weapon.CooldownRemaining = weapon.CooldownDuration;
            intent.ShootTriggered = false;
        }
    }
}
