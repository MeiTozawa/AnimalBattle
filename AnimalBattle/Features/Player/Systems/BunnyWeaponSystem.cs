using System;
using AnimalBattle.Combat;
using AnimalBattle.Components;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Player.Systems;

/// <summary>
///     Bunny 蟆ら畑霑第磁謇捺茶豁ｦ蝎ｨ繧ｷ繧ｹ繝・Β縲・///     <see cref="BunnyWeaponState"/> 繧呈戟縺､繧ｨ繝ｳ繝・ぅ繝・ぅ縺ｮ <see cref="WeaponIntent"/> 繧呈ｶ郁ｲｻ縺励・///     霑第磁謇捺茶繧ｯ繝ｼ繝ｫ繝繧ｦ繝ｳ邂｡逅・∝ｸｸ鬧占ｿ第磁蛻､螳壹・譛牙柑蛹・辟｡蜉ｹ蛹門宛蠕｡縺翫ｈ縺ｳ蜑埼ｲ遯・ｲ繝ｻ繧ｹ繝励Μ繝ｳ繧ｰ貍泌・繧貞ｮ溯｡後＠縺ｾ縺吶・/// </summary>
public static class BunnyWeaponSystem
{
    /// <summary>
    ///     Bunny 霑第磁豁ｦ蝎ｨ繧ｷ繧ｹ繝・Β繧・Flecs.NET 繝ｯ繝ｼ繝ｫ繝峨∈逋ｻ骭ｲ縺励∪縺吶・    /// </summary>
    /// <param name="world">逋ｻ骭ｲ蜈医・ Flecs.NET 繝ｯ繝ｼ繝ｫ繝・/param>
    /// <returns>繝｡繧ｽ繝・ラ繝√ぉ繝ｼ繝ｳ逕ｨ縺ｮ <see cref="World"/></returns>
    public static World AddBunnyWeaponSystem(this World world)
    {
        world.System<Transform, WeaponIntent, BunnyWeaponState, SpringScale, Animation, CollisionBox>("BunnyWeaponSystem")
            .With<EquippedWeapon>(Ecs.Wildcard)
            .Kind(Ecs.OnUpdate)
            .Without<DeadTag>()
            .Iter(ExecuteBunnyWeaponSystem);

        return world;
    }

    private static void ExecuteBunnyWeaponSystem(
        Iter it,
        Field<Transform> transforms,
        Field<WeaponIntent> intents,
        Field<BunnyWeaponState> weapons,
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
            ref var intent = ref intents[i];
            ref var weapon = ref weapons[i];
            ref var spring = ref springs[i];
            ref var anim = ref anims[i];
            ref readonly var col = ref collisions[i];

            ExecuteBunnyWeapon(ref trans, ref intent, ref weapon, ref spring, melee, ref anim, in col, dt);
        }
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
        // 1. 繧ｯ繝ｼ繝ｫ繝繧ｦ繝ｳ繧ｿ繧､繝槭・縺ｮ貂幄｡ｰ
        if (weapon.CooldownRemaining > 0.0f)
        {
            weapon.CooldownRemaining = Math.Max(0.0f, weapon.CooldownRemaining - dt);
        }

        // 2. 霑第磁謾ｻ謦・い繧ｯ繝・ぅ繝匁凾髢薙・貂幄｡ｰ縺ｨ辟｡蜉ｹ蛹・        if (weapon.AttackActiveRemaining > 0.0f)
        {
            weapon.AttackActiveRemaining -= dt;
            if (weapon.AttackActiveRemaining <= 0.0f)
            {
                weapon.AttackActiveRemaining = 0.0f;
                meleeEntity.Disable();
            }
        }

        // 3. 霑第磁謾ｻ謦・ヨ繝ｪ繧ｬ繝ｼ縺ｮ蛻､螳壹→螳溯｡・        if (intent.ShootTriggered && weapon.CooldownRemaining <= 0.0f)
        {
            var dir = intent.AimDirection;

            // 謾ｻ謦・婿蜷代′譛ｪ謖・ｮ壹・蝣ｴ蜷医・蜿ｳ蜷代″繧偵ョ繝輔か繝ｫ繝医→縺吶ｋ
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
