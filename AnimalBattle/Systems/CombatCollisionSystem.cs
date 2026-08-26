using System;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core.Physics;

namespace AnimalBattle.Systems;

/// <summary>
/// Bevy スタイルのコンポーネント駆動 (Component-Driven) 戦闘衝突判定システム。
/// 攻撃判定 <see cref="Hitbox"/>（有効状態 IsActive 検査含む）と 被弾判定 <see cref="Hurtbox"/> 間の幾何学的交差、
/// 陣営判定（<see cref="Faction"/>）、HP 減算、無敵時間、撃破演出を一括処理します。
/// </summary>
public static class CombatCollisionSystem
{
    /// <summary>
    /// 戦闘衝突判定システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World"/></returns>
    public static World AddCombatCollisionSystem(this World world)
    {
        Query<Transform, Hurtbox, Health> hurtboxQuery =
            world.QueryBuilder<Transform, Hurtbox, Health>()
                 .Build();

        world.System<Transform, Hitbox>("CombatCollisionSystem")
             .Kind(Ecs.OnUpdate)
             .Each((Entity atkEntity, ref Transform atkTrans, ref Hitbox hitbox) =>
             {
                 PlayerConfig pCfg = ConfigManager.Player;
                 Transform localAtkTrans = atkTrans;
                 Hitbox localHitbox = hitbox;

                 if (!localHitbox.IsActive)
                 {
                     return;
                 }

                 bool shouldDestroyAttacker = false;

                 hurtboxQuery.Each((Entity vicEntity, ref Transform vicTrans, ref Hurtbox hurtbox, ref Health vicHealth) =>
                 {
                     if (localHitbox.Faction == hurtbox.Faction || !vicHealth.IsAlive || IsTargetInvulnerable(vicEntity, in vicHealth))
                     {
                         return;
                     }

                     if (PhysicsService.Intersects(localAtkTrans, localHitbox.Shape, vicTrans, hurtbox.Shape))
                     {
                         ApplyDamageAndEffects(world, vicEntity, ref vicTrans, ref hurtbox, ref vicHealth, in localHitbox, pCfg, ref shouldDestroyAttacker);
                     }
                 });

                 if (shouldDestroyAttacker)
                 {
                     atkEntity.Destruct();
                 }
             });

        return world;
    }

    private static bool IsTargetInvulnerable(Entity vicEntity, in Health vicHealth)
    {
        if (vicHealth.IsInvincible)
        {
            return true;
        }

        if (vicEntity.Has<BossCompatibilityData>())
        {
            ref readonly BossCompatibilityData compatibility = ref vicEntity.Get<BossCompatibilityData>();
            if (compatibility.IsInvulnerable)
            {
                return true;
            }
        }

        if (vicEntity.Has<Boss1StateMachine>())
        {
            ref readonly Boss1StateMachine fsm1 = ref vicEntity.Get<Boss1StateMachine>();
            if (fsm1.IsInvulnerable)
            {
                return true;
            }
        }
        else if (vicEntity.Has<Boss2StateMachine>())
        {
            ref readonly Boss2StateMachine fsm2 = ref vicEntity.Get<Boss2StateMachine>();
            if (fsm2.IsInvulnerable)
            {
                return true;
            }
        }

        return false;
    }

    private static void ApplyDamageAndEffects(
        World world,
        Entity vicEntity,
        ref Transform vicTrans,
        ref Hurtbox hurtbox,
        ref Health vicHealth,
        in Hitbox localHitbox,
        PlayerConfig pCfg,
        ref bool shouldDestroyAttacker)
    {
        vicHealth.Current -= localHitbox.Damage;
        CombatEffectHelper.SpawnHitSparkEffect(world, vicTrans.X + hurtbox.Shape.OffsetX, vicTrans.Y + hurtbox.Shape.OffsetY);

        if (vicEntity.Has<PlayerTag>())
        {
            vicHealth.InvincibleTimer = pCfg.InvincibleDuration;
            if (!vicHealth.IsAlive)
            {
                vicHealth.Current = 0.0f;
                if (vicEntity.Has<Sprite>())
                {
                    ref Sprite spr = ref vicEntity.GetMut<Sprite>();
                    spr.IsVisible = false;
                }
                CombatEffectHelper.SpawnPlayerDeathParticles(world, vicTrans.X, vicTrans.Y);
            }
        }
        else if (vicEntity.Has<Boss1Stats>())
        {
            ref Boss1Stats bStats1 = ref vicEntity.GetMut<Boss1Stats>();
            bStats1.HitFlashTimer = ConfigManager.Boss1.HitFlashDuration;

            if (!vicHealth.IsAlive)
            {
                vicHealth.Current = 0.0f;
                CombatEffectHelper.ClearAllBossBullets(world);
                CombatEffectHelper.SpawnVictoryExplosions(world, vicTrans.X, vicTrans.Y);
            }
        }
        else if (vicEntity.Has<Boss2Stats>())
        {
            ref Boss2Stats bStats2 = ref vicEntity.GetMut<Boss2Stats>();
            bStats2.HitFlashTimer = ConfigManager.Boss2.HitFlashDuration;

            if (!vicHealth.IsAlive)
            {
                vicHealth.Current = 0.0f;
                CombatEffectHelper.ClearAllBossBullets(world);
                CombatEffectHelper.SpawnVictoryExplosions(world, vicTrans.X, vicTrans.Y);
            }
        }

        if (localHitbox.DestroyOnHit)
        {
            shouldDestroyAttacker = true;
        }
    }
}
