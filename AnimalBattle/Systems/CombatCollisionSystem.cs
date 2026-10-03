using System.Drawing;
using AnimalBattle.Characters;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     Bevy スタイルのコンポーネント駆動 (Component-Driven) 戦闘衝突判定システム。
///     攻撃判定 <see cref="Hitbox" />（有効状態 IsActive 検査含む）と 被弾判定 <see cref="Hurtbox" /> 間の幾何学的交差、
///     陣営判定（<see cref="Faction" />）、HP 減算、無敵時間、撃破演出を一括処理します。
/// </summary>
public static class CombatCollisionSystem {
    /// <summary>
    ///     戦闘衝突判定システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World" /></returns>
    public static World AddCombatCollisionSystem(this World world)
    {
        CollisionContext context = new(world);

        world.System<Transform, Hitbox>("CombatCollisionSystem")
           .Kind(Ecs.OnUpdate)
           .Iter((Iter it, Field<Transform> atkTrans, Field<Hitbox> hitboxes) =>
            {
                context.Refresh();
                if (context.TargetCount == 0) return;

                var pCfg = ConfigManager.Player;
                var count = it.Count();

                for (var i = 0; i < count; i++)
                {
                    ref var hitbox = ref hitboxes[i];
                    if (!hitbox.IsActive) continue;

                    ref var trans = ref atkTrans[i];
                    var atkEntity = it.Entity(i);
                    var shouldDestroyAttacker = false;

                    var aCenterX = trans.X + hitbox.Shape.OffsetX;
                    var aCenterY = trans.Y + hitbox.Shape.OffsetY;
                    var aHalfW = hitbox.Shape.Type == CollisionShapeType.Box
                        ? hitbox.Shape.Width * 0.5f
                        : hitbox.Shape.Radius;
                    var aHalfH = hitbox.Shape.Type == CollisionShapeType.Box
                        ? hitbox.Shape.Height * 0.5f
                        : hitbox.Shape.Radius;

                    for (var tIdx = 0; tIdx < context.TargetCount; tIdx++)
                    {
                        ref readonly var target = ref context.Targets[tIdx];

                        if (hitbox.Faction == target.Faction) continue;

                        // AABB Broadphase 枝刈り（空間粗判定による早期リターン）
                        if (MathF.Abs(aCenterX - target.CenterX) > aHalfW + target.HalfW ||
                            MathF.Abs(aCenterY - target.CenterY) > aHalfH + target.HalfH)
                            continue;

                        if (!target.Entity.IsAlive()) continue;
                        if (target.Owner != null && (!target.Owner.Health.IsAlive || target.Owner.IsInvulnerable || target.Owner.Health.IsInvincible)) continue;

                        ref var vicTrans = ref target.Entity.GetMut<Transform>();
                        ref var hurtbox = ref target.Entity.GetMut<Hurtbox>();

                        if (PhysicsService.Intersects(trans, hitbox.Shape, vicTrans, hurtbox.Shape))
                        {
                            ApplyDamageAndEffects(world, target.Entity, ref vicTrans, ref hurtbox, target.Owner,
                                in hitbox, ref shouldDestroyAttacker);
                            if (shouldDestroyAttacker) break;
                        }
                    }

                    if (shouldDestroyAttacker) CombatEntityPoolHelper.Recycle(world, atkEntity);
                }
            });

        return world;
    }

    private static void ApplyDamageAndEffects(
        World world,
        Entity vicEntity,
        ref Transform vicTrans,
        ref Hurtbox hurtbox,
        IDamageable? owner,
        in Hitbox localHitbox,
        ref bool shouldDestroyAttacker)
    {
        var damage = localHitbox.Damage;
        var isVulnerable = owner != null && owner.DamageMultiplier > 1.0f;

        owner?.TakeDamage(damage);

        CombatEffectHelper.SpawnHitSparkEffect(world, vicTrans.X + hurtbox.Shape.OffsetX,
            vicTrans.Y + hurtbox.Shape.OffsetY);

        if (isVulnerable)
        {
            // 易傷特効クリティカル火花
            CombatEffectHelper.SpawnHitSparkEffect(world, vicTrans.X + hurtbox.Shape.OffsetX - 8.0f,
                vicTrans.Y + hurtbox.Shape.OffsetY - 8.0f);
            CombatEffectHelper.SpawnHitSparkEffect(world, vicTrans.X + hurtbox.Shape.OffsetX + 8.0f,
                vicTrans.Y + hurtbox.Shape.OffsetY + 8.0f);
        }

        // 受撃白閃演出を付与（Ease-Out 減衰）
        vicEntity.Set(new HitFlashEffect(0.08f, Color.White));

        var isAlive = owner?.Health.IsAlive ?? true;
        if (vicEntity.Has<PlayerTag>())
        {
            if (!isAlive)
            {
                if (vicEntity.Has<Sprite>())
                {
                    ref var spr = ref vicEntity.GetMut<Sprite>();
                    spr.IsVisible = false;
                }

                CombatEffectHelper.SpawnPlayerDeathParticles(world, vicTrans.X, vicTrans.Y);
            }
        }
        else if (vicEntity.Has<BossTag>())
        {
            if (!isAlive)
            {
                if (vicEntity.Has<Sprite>())
                {
                    ref var spr = ref vicEntity.GetMut<Sprite>();
                    spr.IsVisible = false;
                }

                CombatEffectHelper.ClearAllBossBullets(world);
                CombatEffectHelper.SpawnVictoryExplosions(world, vicTrans.X, vicTrans.Y);
            }
            else if (vicEntity.Has<BossCombatState>())
            {
                ref readonly var combat = ref vicEntity.Get<BossCombatState>();
                // Boss 被弾反撃：現在の Phase に応じた数量（P1: 1枚, P2: 2枚, P3: 3枚）の黑色弾幕を全方位ランダムに発射
                CombatEffectHelper.SpawnBossCounterAttack(world, vicTrans.X, vicTrans.Y, combat.Phase);
            }
        }

        if (localHitbox.DestroyOnHit) shouldDestroyAttacker = true;
    }

    private struct HurtboxTarget {
        public Entity Entity;
        public float CenterX;
        public float CenterY;
        public float HalfW;
        public float HalfH;
        public Faction Faction;
        public CollisionShape Shape;
        public IDamageable? Owner;
    }

    private sealed class CollisionContext {
        private readonly Ecs.IterFieldCallback<Transform, Hurtbox> _collectIter;
        public readonly Query<Transform, Hurtbox> HurtboxQuery;
        public readonly HurtboxTarget[] Targets = new HurtboxTarget[32];
        public int TargetCount;

        public CollisionContext(World world)
        {
            HurtboxQuery = world.QueryBuilder<Transform, Hurtbox>().Build();
            _collectIter = CollectTargets;
        }

        public void Refresh()
        {
            TargetCount = 0;
            HurtboxQuery.Iter(_collectIter);
        }

        private void CollectTargets(Iter it, Field<Transform> trans, Field<Hurtbox> hurtboxes)
        {
            var count = it.Count();
            for (var i = 0; i < count; i++)
            {
                if (TargetCount >= Targets.Length) break;

                ref readonly var hb = ref hurtboxes[i];
                var owner = hb.Owner;
                if (owner != null)
                {
                    if (!owner.Health.IsAlive || owner.IsInvulnerable || owner.Health.IsInvincible)
                        continue;
                }

                var vicEntity = it.Entity(i);
                ref readonly var t = ref trans[i];

                var cX = t.X + hb.Shape.OffsetX;
                var cY = t.Y + hb.Shape.OffsetY;
                var halfW = hb.Shape.Type == CollisionShapeType.Box ? hb.Shape.Width * 0.5f : hb.Shape.Radius;
                var halfH = hb.Shape.Type == CollisionShapeType.Box ? hb.Shape.Height * 0.5f : hb.Shape.Radius;

                Targets[TargetCount++] = new HurtboxTarget {
                    Entity = vicEntity,
                    CenterX = cX,
                    CenterY = cY,
                    HalfW = halfW,
                    HalfH = halfH,
                    Faction = hb.Faction,
                    Shape = hb.Shape,
                    Owner = owner
                };
            }
        }
    }
}