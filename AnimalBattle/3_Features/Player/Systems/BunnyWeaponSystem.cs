using System;
using AnimalBattle.Combat;
using AnimalBattle.Components;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     Bunny 専用近接打撃武器システム。
///     <see cref="BunnyWeaponState"/> を持つエンティティの <see cref="WeaponIntent"/> を消費し、
///     近接打撃クールダウン管理、常駐近接判定の有効化/無効化制御および前進突進・スプリング演出を実行します。
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
        world.System<Transform, WeaponIntent, BunnyWeaponState, SpringScale, PlayerMeleeWeapon, Animation, CollisionBox>("BunnyWeaponSystem")
            .Kind(Ecs.OnUpdate)
            .Without<DeadTag>()
            .Iter((Iter it,
                   Field<Transform> transforms,
                   Field<WeaponIntent> intents,
                   Field<BunnyWeaponState> weapons,
                   Field<SpringScale> springs,
                   Field<PlayerMeleeWeapon> melees,
                   Field<Animation> anims,
                   Field<CollisionBox> collisions) =>
            {
                var dt = it.DeltaTime();
                var count = it.Count();

                for (var i = 0; i < count; i++)
                {
                    ref var trans = ref transforms[i];
                    ref var intent = ref intents[i];
                    ref var weapon = ref weapons[i];
                    ref var spring = ref springs[i];
                    ref readonly var melee = ref melees[i];
                    ref var anim = ref anims[i];
                    ref readonly var col = ref collisions[i];

                    ExecuteBunnyWeapon(ref trans, ref intent, ref weapon, ref spring, melee.Entity, ref anim, in col, dt);
                }
            });

        return world;
    }

    private static void ExecuteBunnyWeapon(
        ref Transform trans,
        ref WeaponIntent intent,
        ref BunnyWeaponState weapon,
        ref SpringScale spring,
        Entity meleeEntity,
        ref Animation anim,
        in CollisionBox col,
        float dt)
    {
        // 1. クールダウンタイマーの減衰
        if (weapon.CooldownRemaining > 0.0f)
        {
            weapon.CooldownRemaining = Math.Max(0.0f, weapon.CooldownRemaining - dt);
        }

        // 2. 近接攻撃アクティブ時間の減衰と無効化
        if (weapon.AttackActiveRemaining > 0.0f)
        {
            weapon.AttackActiveRemaining -= dt;
            if (weapon.AttackActiveRemaining <= 0.0f)
            {
                weapon.AttackActiveRemaining = 0.0f;
                meleeEntity.Disable();
            }
        }

        // 3. 近接攻撃トリガーの判定と実行
        if (intent.ShootTriggered && weapon.CooldownRemaining <= 0.0f)
        {
            var dir = intent.AimDirection;

            // 攻撃方向が未指定の場合は右向きをデフォルトとする
            if (dir.LengthSquared() < 0.0001f)
            {
                dir = new Vec2f(1.0f, 0.0f);
            }
            else
            {
                dir = dir.Normalized();
            }

            var spawnPos = trans.Position + dir * weapon.AttackRange;

            SlashDirection slashDir;
            if (MathF.Abs(dir.X) >= MathF.Abs(dir.Y))
            {
                slashDir = dir.X >= 0.0f ? SlashDirection.Right : SlashDirection.Left;
            }
            else
            {
                slashDir = dir.Y >= 0.0f ? SlashDirection.Down : SlashDirection.Up;
            }

            weapon.AttackActiveRemaining = weapon.AttackDuration;
            MeleeSpawner.ActivatePlayerMeleeAttack(
                meleeEntity,
                spawnPos,
                weapon.AttackSize,
                weapon.AttackDamage,
                slashDir);

            anim.Play("Attack");

            // 近接打撃前進インパルスおよびスプリング伸縮（bunny.toml 定義値から適用）
            var lunge = new Vec2f(dir.X * weapon.AttackLungeDistance, col.IsGrounded ? 0.0f : dir.Y * weapon.AttackLungeDistance);
            trans.Position += lunge;

            spring.Current = weapon.AttackStretchScale;
            spring.Velocity = Vec2f.Zero;

            SoundService.PlaySfx(SfxType.PlayerDash);

            weapon.CooldownRemaining = weapon.CooldownDuration;
            intent.ShootTriggered = false;
        }
    }
}
