using System;
using System.Drawing;
using AnimalBattle.Configs;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// Bevy スタイルのコンポーネント駆動 (Component-Driven) 戦闘衝突判定システム。
/// 攻撃判定 <see cref="Hitbox"/>（有効状態 IsActive 検査含む）と 被弾判定 <see cref="Hurtbox"/> 間の幾何学的交差、
/// 陣営判定（<see cref="Faction"/>）、HP 減算、無敵時間、撃破演出を一括処理します。
/// </summary>
public static class CombatCollisionSystem
{
    private struct HurtboxTarget
    {
        public Entity Entity;
        public float CenterX;
        public float CenterY;
        public float HalfW;
        public float HalfH;
        public Faction Faction;
        public CollisionShape Shape;
    }

    private sealed class CollisionContext
    {
        public readonly HurtboxTarget[] Targets = new HurtboxTarget[32];
        public int TargetCount;
        public readonly Query<Transform, Hurtbox, Health> HurtboxQuery;
        private readonly Ecs.IterFieldCallback<Transform, Hurtbox, Health> _collectIter;

        public CollisionContext(World world)
        {
            HurtboxQuery = world.QueryBuilder<Transform, Hurtbox, Health>().Build();
            _collectIter = CollectTargets;
        }

        public void Refresh()
        {
            TargetCount = 0;
            HurtboxQuery.Iter(_collectIter);
        }

        private void CollectTargets(Iter it, Field<Transform> trans, Field<Hurtbox> hurtboxes, Field<Health> healths)
        {
            int count = it.Count();
            for (int i = 0; i < count; i++)
            {
                if (TargetCount >= Targets.Length)
                {
                    break;
                }

                ref readonly Health hp = ref healths[i];
                if (!hp.IsAlive)
                {
                    continue;
                }

                Entity vicEntity = it.Entity(i);
                if (IsTargetInvulnerable(vicEntity, in hp))
                {
                    continue;
                }

                ref readonly Transform t = ref trans[i];
                ref readonly Hurtbox hb = ref hurtboxes[i];

                float cX = t.X + hb.Shape.OffsetX;
                float cY = t.Y + hb.Shape.OffsetY;
                float halfW = hb.Shape.Type == CollisionShapeType.Box ? hb.Shape.Width * 0.5f : hb.Shape.Radius;
                float halfH = hb.Shape.Type == CollisionShapeType.Box ? hb.Shape.Height * 0.5f : hb.Shape.Radius;

                Targets[TargetCount++] = new HurtboxTarget
                {
                    Entity = vicEntity,
                    CenterX = cX,
                    CenterY = cY,
                    HalfW = halfW,
                    HalfH = halfH,
                    Faction = hb.Faction,
                    Shape = hb.Shape
                };
            }
        }
    }

    /// <summary>
    /// 戦闘衝突判定システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World"/></returns>
    public static World AddCombatCollisionSystem(this World world)
    {
        CollisionContext context = new(world);

        world.System<Transform, Hitbox>("CombatCollisionSystem")
             .Kind(Ecs.OnUpdate)
             .Iter((Iter it, Field<Transform> atkTrans, Field<Hitbox> hitboxes) =>
             {
                 context.Refresh();
                 if (context.TargetCount == 0)
                 {
                     return;
                 }

                 PlayerConfig pCfg = ConfigManager.Player;
                 int count = it.Count();

                 for (int i = 0; i < count; i++)
                 {
                     ref Hitbox hitbox = ref hitboxes[i];
                     if (!hitbox.IsActive)
                     {
                         continue;
                     }

                     ref Transform trans = ref atkTrans[i];
                     Entity atkEntity = it.Entity(i);
                     bool shouldDestroyAttacker = false;

                     float aCenterX = trans.X + hitbox.Shape.OffsetX;
                     float aCenterY = trans.Y + hitbox.Shape.OffsetY;
                     float aHalfW = hitbox.Shape.Type == CollisionShapeType.Box ? hitbox.Shape.Width * 0.5f : hitbox.Shape.Radius;
                     float aHalfH = hitbox.Shape.Type == CollisionShapeType.Box ? hitbox.Shape.Height * 0.5f : hitbox.Shape.Radius;

                     for (int tIdx = 0; tIdx < context.TargetCount; tIdx++)
                     {
                         ref readonly HurtboxTarget target = ref context.Targets[tIdx];

                         if (hitbox.Faction == target.Faction)
                         {
                             continue;
                         }

                         // AABB Broadphase 枝刈り（空間粗判定による早期リターン）
                         if (MathF.Abs(aCenterX - target.CenterX) > (aHalfW + target.HalfW) ||
                             MathF.Abs(aCenterY - target.CenterY) > (aHalfH + target.HalfH))
                         {
                             continue;
                         }

                         if (!target.Entity.IsAlive() || !target.Entity.Has<Health>())
                         {
                             continue;
                         }

                         ref Health vicHealth = ref target.Entity.GetMut<Health>();
                         if (!vicHealth.IsAlive || IsTargetInvulnerable(target.Entity, in vicHealth))
                         {
                             continue;
                         }

                         ref Transform vicTrans = ref target.Entity.GetMut<Transform>();
                         ref Hurtbox hurtbox = ref target.Entity.GetMut<Hurtbox>();

                         if (PhysicsService.Intersects(trans, hitbox.Shape, vicTrans, hurtbox.Shape))
                         {
                             ApplyDamageAndEffects(world, target.Entity, ref vicTrans, ref hurtbox, ref vicHealth, in hitbox, pCfg, ref shouldDestroyAttacker);
                             if (shouldDestroyAttacker)
                             {
                                 break;
                             }
                         }
                     }

                     if (shouldDestroyAttacker)
                     {
                         atkEntity.Destruct();
                     }
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

        // 受撃白閃演出を付与（Ease-Out 減衰）
        vicEntity.Set(new HitFlashEffect(0.08f, Color.White));

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
            ref readonly BossCombatState combat = ref vicEntity.Get<BossCombatState>();

            if (!vicHealth.IsAlive)
            {
                vicHealth.Current = 0.0f;
                if (vicEntity.Has<Sprite>())
                {
                    ref Sprite spr = ref vicEntity.GetMut<Sprite>();
                    spr.IsVisible = false;
                }
                CombatEffectHelper.ClearAllBossBullets(world);
                CombatEffectHelper.SpawnVictoryExplosions(world, vicTrans.X, vicTrans.Y);
            }
            else
            {
                // Boss 被弾反撃：現在の Phase に応じた数量（P1: 1枚, P2: 2枚, P3: 3枚）の黑色弾幕を全方位ランダムに発射
                CombatEffectHelper.SpawnBossCounterAttack(world, vicTrans.X, vicTrans.Y, combat.Phase);
            }
        }

        if (localHitbox.DestroyOnHit)
        {
            shouldDestroyAttacker = true;
        }
    }
}

