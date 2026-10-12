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

                        var vicEntity = target.Entity;
                        if (!vicEntity.IsAlive()) continue;
                        ref readonly var hp = ref vicEntity.Get<Health>();
                        if (hp.Current <= 0.0f || hp.InvincibleTimer > 0.0f) continue;

                        ref var vicTrans = ref vicEntity.GetMut<Transform>();
                        ref var hurtbox = ref vicEntity.GetMut<Hurtbox>();

                        if (PhysicsService.Intersects(trans, hitbox.Shape, vicTrans, hurtbox.Shape))
                        {
                            ApplyHit(world, vicEntity, ref vicTrans, in trans, ref hurtbox, in hitbox,
                                ref shouldDestroyAttacker);
                            if (shouldDestroyAttacker) break;
                        }
                    }

                    if (shouldDestroyAttacker) CombatPoolService.Recycle(atkEntity);
                }
            });

        return world;
    }

    private static void ApplyHit(
        World world,
        Entity vicEntity,
        ref Transform vicTrans,
        in Transform atkTrans,
        ref Hurtbox hurtbox,
        in Hitbox localHitbox,
        ref bool shouldDestroyAttacker)
    {
        var knockbackDir = vicTrans.X >= atkTrans.X ? 1.0f : -1.0f;
        var isHeavy = localHitbox.Damage >= 2.0f;
        vicEntity.Set(new IncomingDamage {
            Amount = localHitbox.Damage,
            SourceFaction = localHitbox.Faction,
            KnockbackX = knockbackDir * 180.0f,
            KnockbackY = -90.0f,
            IsHeavyImpact = isHeavy
        });
        CombatEffectHelper.SpawnHitSparkEffect(world, vicTrans.X + hurtbox.Shape.OffsetX,
            vicTrans.Y + hurtbox.Shape.OffsetY);

        if (localHitbox.Faction == Faction.Friendly)
        {
            CombatEffectHelper.SpawnBulletImpactSplatter(world, atkTrans.X, atkTrans.Y, knockbackDir, 0.0f);
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
    }

    private sealed class CollisionContext {
        private readonly Ecs.IterFieldCallback<Transform, Hurtbox, Health> _collectIter;
        public Query<Transform, Hurtbox, Health> HurtboxQuery;
        public readonly HurtboxTarget[] Targets = new HurtboxTarget[32];
        public int TargetCount;

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

        private void CollectTargets(Iter it, Field<Transform> trans, Field<Hurtbox> hurtboxes, Field<Health> hps)
        {
            var count = it.Count();
            for (var i = 0; i < count; i++)
            {
                if (TargetCount >= Targets.Length) break;

                ref readonly var hp = ref hps[i];
                if (hp.Current <= 0.0f || hp.InvincibleTimer > 0.0f)
                    continue;

                var vicEntity = it.Entity(i);
                ref readonly var hb = ref hurtboxes[i];
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
                    Shape = hb.Shape
                };
            }
        }
    }
}