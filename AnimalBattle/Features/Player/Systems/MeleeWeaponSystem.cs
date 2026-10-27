using System;
using AnimalBattle.Combat;
using AnimalBattle.Components;
using AnimalBattle.Core;
using AnimalBattle.Player.Components;
using Flecs.NET.Core;

namespace AnimalBattle.Player.Systems;

/// <summary>
///     近接判定展開武器機構システム。
///     <see cref="MeleeWeaponComponent"/> を持つエンティティの <see cref="WeaponIntent"/> を消費し、
///     近接打撃クールダウン管理、常駐近接判定の有効化/無効化制御および前進突進・スプリング演出を実行します。
/// </summary>
public static class MeleeWeaponSystem
{
    /// <summary>
    ///     近接武器システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World"/></returns>
    public static World AddMeleeWeaponSystem(this World world)
    {
        world.System<Transform, Velocity, WeaponIntent, MeleeWeaponComponent, SpringScale, Animation, CollisionBox>("MeleeWeaponSystem")
            .With<EquippedWeapon>(Ecs.Wildcard)
            .Kind(Ecs.OnUpdate)
            .Without<DeadTag>()
            .Iter(ExecuteMeleeWeaponSystem);

        return world;
    }

    private static void ExecuteMeleeWeaponSystem(
        Iter it,
        Field<Transform> transforms,
        Field<Velocity> velocities,
        Field<WeaponIntent> intents,
        Field<MeleeWeaponComponent> weapons,
        Field<SpringScale> springs,
        Field<Animation> anims,
        Field<CollisionBox> collisions)
    {
        var count = it.Count();
        if (count == 0) return;

        var dt = it.DeltaTime();

        for (var i = 0; i < count; i++)
        {
            var player = it.Entity(i);
            var melee = player.Target<EquippedWeapon>();
            if (!melee.IsAlive()) continue;

            ref var trans = ref transforms[i];
            ref var vel = ref velocities[i];
            ref var intent = ref intents[i];
            ref var weapon = ref weapons[i];
            ref var spring = ref springs[i];
            ref var anim = ref anims[i];
            ref readonly var col = ref collisions[i];

            ExecuteMeleeWeapon(ref trans, ref vel, ref intent, ref weapon, ref spring, melee, ref anim, in col, dt);
        }
    }

    private static void ExecuteMeleeWeapon(
        ref Transform trans,
        ref Velocity vel,
        ref WeaponIntent intent,
        ref MeleeWeaponComponent weapon,
        ref SpringScale spring,
        Entity meleeEntity,
        ref Animation anim,
        in CollisionBox col,
        float dt)
    {
        // 1. クールダウンタイマーの減衰
        if (weapon.Timer > 0.0f)
        {
            weapon.Timer = Math.Max(0.0f, weapon.Timer - dt);
        }

        // 2. 近接攻撃アクティブ時間の減衰と無効化
        if (weapon.ActiveRemaining > 0.0f)
        {
            weapon.ActiveRemaining -= dt;
            if (weapon.ActiveRemaining <= 0.0f)
            {
                weapon.ActiveRemaining = 0.0f;
                meleeEntity.Disable();
            }
        }

        // 3. 近接攻撃トリガーの判定と実行
        if (intent.ShootTriggered && weapon.Timer <= 0.0f)
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

            var spawnPos = trans.Position + dir * weapon.Range;

            SlashDirection slashDir;
            if (MathF.Abs(dir.X) >= MathF.Abs(dir.Y))
            {
                slashDir = dir.X >= 0.0f ? SlashDirection.Right : SlashDirection.Left;
            }
            else
            {
                slashDir = dir.Y >= 0.0f ? SlashDirection.Down : SlashDirection.Up;
            }

            weapon.ActiveRemaining = weapon.Duration;
            MeleeSpawner.ActivatePlayerMeleeAttack(
                meleeEntity,
                spawnPos,
                weapon.HitboxSize,
                weapon.Damage,
                slashDir);

            anim.Play("Attack");

            // 近接打撃前進突進：外力物理インパルスとして付与
            var lungeSpeed = weapon.LungeImpulse;
            var lungeY = col.IsGrounded ? 0.0f : dir.Y * lungeSpeed;
            var lungeImpulse = new Vec2f(dir.X * lungeSpeed, lungeY);
            vel.Knockback += lungeImpulse;

            spring.Current = weapon.StretchScale;
            spring.Velocity = Vec2f.Zero;

            SoundService.PlaySfx(SfxType.PlayerDash);

            weapon.Timer = weapon.Cooldown;
            intent.ShootTriggered = false;
        }
    }
}
