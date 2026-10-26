using System;
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

        // グローバル戦闘物理設定の取得
        var physicsCfg = world.Get<CombatPrefabConfig>().Physics;
        var baseKnockback = localHitbox.Knockback != Vec2f.Zero ? localHitbox.Knockback : physicsCfg.DefaultKnockback;
        var isHeavy = localHitbox.IsHeavy || (localHitbox.Damage >= physicsCfg.HeavyDamageThreshold);
        var targetKnockback = baseKnockback with { X = knockbackDir * baseKnockback.X };

        // 常駐 IncomingDamage へのインライン書き込み（Archetype 変更や遅延キューを発生させず即時反映）
        ref var incoming = ref vicEntity.GetMut<IncomingDamage>();
        if (incoming.Amount <= 0.0f)
        {
            incoming.Amount = localHitbox.Damage;
            incoming.SourceFaction = localHitbox.Faction;
            incoming.Knockback = targetKnockback;
            incoming.IsHeavyImpact = isHeavy;
        }
        else
        {
            // 敵対判定（Boss攻撃等）から自機への同一フレーム多重被弾は最大値を採用して多重被弾死を防止
            // プレイヤーからの弾幕は同一フレームの散弾累加を許可
            if (localHitbox.Faction == Faction.Hostile)
            {
                incoming.Amount = MathF.Max(incoming.Amount, localHitbox.Damage);
            }
            else
            {
                incoming.Amount += localHitbox.Damage;
            }

            incoming.IsHeavyImpact |= isHeavy;
            if (isHeavy)
            {
                incoming.Knockback = targetKnockback;
            }
        }

        EffectSpawner.SpawnHitSparkEffect(world, vicTrans.Position + hurtbox.Shape.Offset);

        if (localHitbox.Faction == Faction.Friendly)
        {
            EffectSpawner.SpawnBulletImpactSplatter(world, atkTrans.Position, new Vec2f(knockbackDir, 0.0f));
        }

        if (localHitbox.DestroyOnHit) shouldDestroyAttacker = true;
    }
}
