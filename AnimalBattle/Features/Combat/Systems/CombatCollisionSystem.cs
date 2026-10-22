using System;
using AnimalBattle.Abilities;
using AnimalBattle.Combat.Spatial;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Core.Geometry;
using Flecs.NET.Core;

namespace AnimalBattle.Combat.Systems;

/// <summary>
///     Bevy スタイルのコンポーネント駆動 戦闘衝突判定システム。
///     攻撃判定 <see cref="Hitbox" />：有効状態 IsActive 検査含むと 被弾判定 <see cref="Hurtbox" /> 間の幾何学的交差、
///     陣営判定<see cref="Faction" />、HP 減算、無敵時間、撃破演出を一括処理します。
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
        // 通常単発攻撃判定パイプライン：弾幕等：HitHistory なし
        world.System<Transform, Hitbox>("CombatCollisionSystem")
            .Without<HitHistory>()
            .Without<RecycleTag>()
            .Kind(Ecs.OnUpdate)
            .Iter(ExecuteSingleHit);

        return world;
    }

    /// <summary>
    ///     単発弾幕・投射物の衝突判定パイプライン：名前付き静的メソッド。
    ///     空間ハッシュから候補 Hurtbox を O 抽出して幾何相交を判定します。
    /// </summary>
    public static void ExecuteSingleHit(Iter it, Field<Transform> atkTrans, Field<Hitbox> hitboxes)
    {
        var spatialIndex = it.World().Get<HurtboxSpatialResource>().Index;
        if (spatialIndex.ItemCount == 0) return;

        var count = it.Count();
        var world = it.World();

        Span<int> candidateIndices = stackalloc int[16];

        // 被弾コンポーネント追加や破棄タグ付与などの構造変更によるイテレーション破壊と同一フレーム内の多段ヒットを防止するため、遅延実行モードを開始する
        using (new DeferScope(world))
        {
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

                    ref readonly var vicTrans = ref vicEntity.Get<Transform>();
                    ref readonly var hurtbox = ref vicEntity.Get<Hurtbox>();

                    if (PhysicsService.Intersects(trans, hitbox.Shape, vicTrans, hurtbox.Shape))
                    {
                        ApplyHit(world, vicEntity, in vicTrans, in trans, in hurtbox, in hitbox,
                            ref shouldDestroyAttacker);
                        if (shouldDestroyAttacker)
                        {
                            hitbox.IsActive = false;
                            break;
                        }
                    }
                }

                if (shouldDestroyAttacker)
                {
                    hitbox.IsActive = false;
                    atkEntity.Add<RecycleTag>();
                }
            }
        }
    }

    internal static void ApplyHit(
        World world,
        Entity vicEntity,
        in Transform vicTrans,
        in Transform atkTrans,
        in Hurtbox hurtbox,
        in Hitbox localHitbox,
        ref bool shouldDestroyAttacker)
    {
        var knockbackDir = vicTrans.Position.X >= atkTrans.Position.X ? 1.0f : -1.0f;
        var isHeavy = localHitbox.Damage >= 2.0f;

        // 同一フレーム内で複数弾や近接攻撃が連続ヒットした際のダメージ上書き消失を防止するため
        // Ensure で IncomingDamage の存在を保証しつつ、Amount を累加し IsHeavyImpact を論理和で合成します
        // ECS システムイテレーションおよび DeferScope による遅延キューイング中であっても即時更新できるよう DeferSuspend します
        var isDeferred = world.IsDeferred();
        if (isDeferred) world.DeferSuspend();
        try
        {
            ref var incoming = ref vicEntity.Ensure<IncomingDamage>();
            var isFirstHit = incoming.Amount <= 0.0f;
            incoming.Amount += localHitbox.Damage;
            incoming.SourceFaction = isFirstHit ? localHitbox.Faction : incoming.SourceFaction;
            incoming.Knockback = (isFirstHit || isHeavy) ? new Vec2f(knockbackDir * 180.0f, -90.0f) : incoming.Knockback;
            incoming.IsHeavyImpact |= isHeavy;
        }
        finally
        {
            if (isDeferred) world.DeferResume();
        }

        EffectSpawner.SpawnHitSparkEffect(world, vicTrans.Position + hurtbox.Shape.Offset);

        if (localHitbox.Faction == Faction.Friendly)
        {
            EffectSpawner.SpawnBulletImpactSplatter(world, atkTrans.Position, new Vec2f(knockbackDir, 0.0f));
        }

        if (localHitbox.DestroyOnHit) shouldDestroyAttacker = true;
    }
}