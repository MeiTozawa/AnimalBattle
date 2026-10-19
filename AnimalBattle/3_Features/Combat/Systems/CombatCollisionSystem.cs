using System;
using AnimalBattle.Abilities;
using AnimalBattle.Combat.Spatial;
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
public static class CombatCollisionSystem
{
    /// <summary>
    ///     戦闘衝突判定システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World" /></returns>
    public static World AddCombatCollisionSystem(this World world)
    {
        // 通常単発攻撃判定パイプライン（弾幕等：HitHistory なし）
        world.System<Transform, Hitbox>("CombatCollisionSystem")
            .Without<HitHistory>()
            .Without<RecycleTag>()
            .Kind(Ecs.OnUpdate)
            .Iter(ExecuteSingleHit);

        return world;
    }

    /// <summary>
    ///     単発弾幕・投射物の衝突判定パイプライン（名前付き静的メソッド）。
    ///     空間ハッシュから候補 Hurtbox を O(1) 抽出して幾何相交を判定します。
    /// </summary>
    public static void ExecuteSingleHit(Iter it, Field<Transform> atkTrans, Field<Hitbox> hitboxes)
    {
        var spatialIndex = HurtboxSpatialIndexSystem.SpatialIndex;
        if (spatialIndex.ItemCount == 0) return;

        var count = it.Count();
        var world = it.World();

        Span<int> candidateIndices = stackalloc int[16];

        for (var i = 0; i < count; i++)
        {
            ref var hitbox = ref hitboxes[i];
            if (!hitbox.IsActive) continue;

            ref var trans = ref atkTrans[i];
            var atkEntity = it.Entity(i);
            var shouldDestroyAttacker = false;

            var aCenterX = trans.Position.X + hitbox.Shape.Offset.X;
            var aCenterY = trans.Position.Y + hitbox.Shape.Offset.Y;
            var aHalfW = hitbox.Shape.Type == CollisionShapeType.Box
                ? hitbox.Shape.Size.X * 0.5f
                : hitbox.Shape.Radius;
            var aHalfH = hitbox.Shape.Type == CollisionShapeType.Box
                ? hitbox.Shape.Size.Y * 0.5f
                : hitbox.Shape.Radius;

            var matchCount = spatialIndex.QueryCandidates(
                aCenterX, aCenterY, aHalfW, aHalfH, hitbox.Faction, candidateIndices);

            for (var c = 0; c < matchCount; c++)
            {
                ref readonly var target = ref spatialIndex.GetItem(candidateIndices[c]);
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
        vicEntity.Set(new IncomingDamage
        {
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
}