using System;
using AnimalBattle.Abilities;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Core.Geometry;
using AnimalBattle.Effects;
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

        // 1. 通常単発攻撃判定パイプライン（弾幕等：HitHistory なし・entity.Has 一切なし）
        world.System<Transform, Hitbox>("CombatCollisionSystem")
           .Without<HitHistory>()
           .Without<RecycleTag>()
           .Kind(Ecs.OnUpdate)
           .Iter((Iter it, Field<Transform> atkTrans, Field<Hitbox> hitboxes) =>
            {
                context.Refresh();
                if (context.TargetCount == 0) return;

                var count = it.Count();

                for (var i = 0; i < count; i++)
                {
                    ref var hitbox = ref hitboxes[i];
                    if (!hitbox.IsActive) continue;

                    ref var trans = ref atkTrans[i];
                    var atkEntity = it.Entity(i);
                    var shouldDestroyAttacker = false;

                    var aCenter = trans.Position + hitbox.Shape.Offset;
                    var aHalf = hitbox.Shape.Type == CollisionShapeType.Box
                        ? hitbox.Shape.Size * 0.5f
                        : new Vec2f(hitbox.Shape.Radius, hitbox.Shape.Radius);

                    for (var tIdx = 0; tIdx < context.TargetCount; tIdx++)
                    {
                        ref readonly var target = ref context.Targets[tIdx];

                        if (hitbox.Faction == target.Faction) continue;

                        // AABB Broadphase 枝刈り（空間粗判定による早期リターン）
                        var diff = aCenter - target.Center;
                        if (MathF.Abs(diff.X) > aHalf.X + target.HalfExtents.X ||
                            MathF.Abs(diff.Y) > aHalf.Y + target.HalfExtents.Y)
                            continue;

                        var vicEntity = target.Entity;
                        if (!vicEntity.IsAlive()) continue;
                        ref readonly var hp = ref vicEntity.Get<Health>();
                        if (hp.Current <= 0.0f) continue;

                        ref var vicTrans = ref vicEntity.GetMut<Transform>();
                        ref var hurtbox = ref vicEntity.GetMut<Hurtbox>();

                        if (PhysicsService.Intersects(trans, hitbox.Shape, vicTrans, hurtbox.Shape))
                        {
                            ApplyHit(world, vicEntity, ref vicTrans, in trans, ref hurtbox, in hitbox,
                                ref shouldDestroyAttacker);
                            if (shouldDestroyAttacker) break;
                        }
                    }

                    if (shouldDestroyAttacker) atkEntity.Add<RecycleTag>();
                }
            });

        return world;
    }

    internal static void ApplyHit(
        World world,
        Entity vicEntity,
        ref Transform vicTrans,
        in Transform atkTrans,
        ref Hurtbox hurtbox,
        in Hitbox localHitbox,
        ref bool shouldDestroyAttacker)
    {
        var knockbackDir = vicTrans.Position.X >= atkTrans.Position.X ? 1.0f : -1.0f;
        var isHeavy = localHitbox.Damage >= 2.0f;
        vicEntity.Set(new IncomingDamage {
            Amount = localHitbox.Damage,
            SourceFaction = localHitbox.Faction,
            Knockback = new Vec2f(knockbackDir * 180.0f, -90.0f),
            IsHeavyImpact = isHeavy
        });
        EffectSpawner.SpawnHitSparkEffect(world, vicTrans.Position + hurtbox.Shape.Offset);

        if (localHitbox.Faction == Faction.Friendly)
        {
            EffectSpawner.SpawnBulletImpactSplatter(world, atkTrans.Position, new Vec2f(knockbackDir, 0.0f));
        }

        if (localHitbox.DestroyOnHit) shouldDestroyAttacker = true;
    }

    internal struct HurtboxTarget {
        public Entity Entity;
        public Vec2f Center;
        public Vec2f HalfExtents;
        public Faction Faction;
        public CollisionShape Shape;
    }

    internal sealed class CollisionContext {
        private readonly Ecs.IterFieldCallback<Transform, Hurtbox, Health, GameplayTagState> _collectIter;
        public Query<Transform, Hurtbox, Health, GameplayTagState> HurtboxQuery;
        public readonly HurtboxTarget[] Targets = new HurtboxTarget[32];
        public int TargetCount;

        public CollisionContext(World world)
        {
            HurtboxQuery = world.QueryBuilder<Transform, Hurtbox, Health, GameplayTagState>().Build();
            _collectIter = CollectTargets;
        }

        public void Refresh()
        {
            TargetCount = 0;
            HurtboxQuery.Iter(_collectIter);
        }

        private void CollectTargets(Iter it, Field<Transform> trans, Field<Hurtbox> hurtboxes, Field<Health> hps, Field<GameplayTagState> tags)
        {
            var count = it.Count();
            for (var i = 0; i < count; i++)
            {
                if (TargetCount >= Targets.Length) break;

                ref readonly var hp = ref hps[i];
                if (hp.Current <= 0.0f)
                    continue;

                ref readonly var tag = ref tags[i];
                if (tag.HasTag(GameplayTag.Untargetable))
                    continue;

                var vicEntity = it.Entity(i);
                ref readonly var hb = ref hurtboxes[i];
                ref readonly var t = ref trans[i];

                var c = t.Position + hb.Shape.Offset;
                var half = hb.Shape.Type == CollisionShapeType.Box ? hb.Shape.Size * 0.5f : new Vec2f(hb.Shape.Radius, hb.Shape.Radius);

                Targets[TargetCount++] = new HurtboxTarget {
                    Entity = vicEntity,
                    Center = c,
                    HalfExtents = half,
                    Faction = hb.Faction,
                    Shape = hb.Shape
                };
            }
        }
    }
}