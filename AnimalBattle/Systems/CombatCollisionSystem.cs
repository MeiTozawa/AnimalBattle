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

        if (vicEntity.Has<BossCombatState>())
        {
            ref readonly BossCombatState combat = ref vicEntity.Get<BossCombatState>();
            return combat.IsInvulnerable;
        }

        return false;
    }

    private static bool IsTargetVulnerable(Entity vicEntity)
    {
        if (vicEntity.Has<BossCombatState>())
        {
            ref readonly BossCombatState combat = ref vicEntity.Get<BossCombatState>();
            return combat.IsVulnerable;
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
        float damage = localHitbox.Damage;
        bool isVulnerable = IsTargetVulnerable(vicEntity);
        if (isVulnerable)
        {
            damage *= pCfg.VulnerableDamageMultiplier;
        }

        vicHealth.Current -= damage;
        CombatEffectHelper.SpawnHitSparkEffect(world, vicTrans.X + hurtbox.Shape.OffsetX, vicTrans.Y + hurtbox.Shape.OffsetY);

        if (isVulnerable)
        {
            // 易傷特効クリティカル火花
            CombatEffectHelper.SpawnHitSparkEffect(world, vicTrans.X + hurtbox.Shape.OffsetX - 8.0f, vicTrans.Y + hurtbox.Shape.OffsetY - 8.0f);
            CombatEffectHelper.SpawnHitSparkEffect(world, vicTrans.X + hurtbox.Shape.OffsetX + 8.0f, vicTrans.Y + hurtbox.Shape.OffsetY + 8.0f);
        }

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
        else if (vicEntity.Has<BossCombatState>())
        {
            ref BossCombatState bCombat = ref vicEntity.GetMut<BossCombatState>();
            bCombat.HitFlashTimer = bCombat.HitFlashDuration;

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

