using System;
using AnimalBattle.Combat;
using AnimalBattle.Components;
using AnimalBattle.Core;
using AnimalBattle.Core.Geometry;
using Flecs.NET.Core;

namespace AnimalBattle.Combat.Systems;

/// <summary>
///     World 単位で独立して保持される近接消弾システム用コンテキスト。
/// </summary>
internal sealed class MeleeBulletCancelContext
{
    public struct MeleeTarget
    {
        public Transform Transform;
        public CollisionShape Shape;
        public Vec2f Center;
        public Vec2f HalfExtents;
    }

    public required Query<Transform, Hitbox> MeleeQuery { get; init; }
    public required Query<Transform, Hitbox, Bullet> BulletQuery { get; init; }
    public readonly MeleeTarget[] Melees = new MeleeTarget[8];
    public int MeleeCount;
    public bool SfxPlayedThisFrame;
}

/// <summary>
///     Bunny 自機プレイヤーの近接攻撃判定 (<see cref="PlayerMeleeTag"/>) と
///     敵弾幕 (<see cref="BossBulletTag"/>) の衝突交差を判定し、弾幕を斬落消去するシステム。
///     地面衝撃波等の破壊不可弾 (<see cref="Bullet.IsIndestructible"/>) は対象外とし、消去時は <see cref="RecycleTag"/> を
///     付与してフレーム末尾で安全回収すると共に、被弾火花演出と音響効果を再生します。
/// </summary>
public static class MeleeBulletCancelSystem
{
    /// <summary>
    ///     近接消弾システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World"/></returns>
    public static World AddMeleeBulletCancelSystem(this World world)
    {
        var meleeQuery = world.QueryBuilder<Transform, Hitbox>()
            .With<PlayerMeleeTag>()
            .Without<RecycleTag>()
            .Build();

        var bulletQuery = world.QueryBuilder<Transform, Hitbox, Bullet>()
            .With<BossBulletTag>()
            .Without<RecycleTag>()
            .Build();

        world.Set(new MeleeBulletCancelContext
        {
            MeleeQuery = meleeQuery,
            BulletQuery = bulletQuery
        });

        world.System("MeleeBulletCancelSystem")
            .Kind(Ecs.OnUpdate)
            .Iter(Execute);

        return world;
    }

    /// <summary>
    ///     近接攻撃判定と敵弾幕の相殺消去パイプライン（名前付き静的メソッド）。
    /// </summary>
    public static void Execute(Iter it)
    {
        var ctx = it.World().Get<MeleeBulletCancelContext>();
        ctx.MeleeCount = 0;
        ctx.MeleeQuery.Iter(CollectMelees);
        if (ctx.MeleeCount == 0) return;

        ctx.SfxPlayedThisFrame = false;
        ctx.BulletQuery.Iter(ProcessBullets);
    }

    private static void CollectMelees(Iter it, Field<Transform> trans, Field<Hitbox> hitboxes)
    {
        var ctx = it.World().Get<MeleeBulletCancelContext>();
        var count = it.Count();
        for (var i = 0; i < count; i++)
        {
            if (ctx.MeleeCount >= ctx.Melees.Length) break;
            ref readonly var h = ref hitboxes[i];
            if (!h.IsActive) continue;

            ref readonly var t = ref trans[i];
            var half = h.Shape.Type == CollisionShapeType.Box ? h.Shape.Size * 0.5f : new Vec2f(h.Shape.Radius, h.Shape.Radius);
            var c = t.Position + h.Shape.Offset;

            ctx.Melees[ctx.MeleeCount++] = new MeleeBulletCancelContext.MeleeTarget
            {
                Transform = t,
                Shape = h.Shape,
                Center = c,
                HalfExtents = half
            };
        }
    }

    private static void ProcessBullets(Iter it, Field<Transform> bTransforms, Field<Hitbox> bHitboxes, Field<Bullet> bBullets)
    {
        var ctx = it.World().Get<MeleeBulletCancelContext>();
        var count = it.Count();
        var world = it.World();

        for (var b = 0; b < count; b++)
        {
            if (bBullets[b].IsIndestructible) continue;

            ref readonly var bHitbox = ref bHitboxes[b];
            if (!bHitbox.IsActive) continue;

            ref readonly var bt = ref bTransforms[b];
            var bEntity = it.Entity(b);

            var bCenter = bt.Position + bHitbox.Shape.Offset;
            var bHalf = bHitbox.Shape.Type == CollisionShapeType.Box ? bHitbox.Shape.Size * 0.5f : new Vec2f(bHitbox.Shape.Radius, bHitbox.Shape.Radius);

            for (var m = 0; m < ctx.MeleeCount; m++)
            {
                ref readonly var melee = ref ctx.Melees[m];

                // AABB 粗判定による早期枝刈り
                var diff = melee.Center - bCenter;
                if (MathF.Abs(diff.X) > melee.HalfExtents.X + bHalf.X ||
                    MathF.Abs(diff.Y) > melee.HalfExtents.Y + bHalf.Y)
                {
                    continue;
                }

                // 精確な幾何交差判定
                if (PhysicsService.Intersects(melee.Transform, melee.Shape, bt, bHitbox.Shape))
                {
                    bEntity.Add<RecycleTag>();
                    EffectSpawner.SpawnHitSparkEffect(world, bt.Position);

                    if (!ctx.SfxPlayedThisFrame)
                    {
                        SoundService.PlaySfx(SfxType.BossHit);
                        ctx.SfxPlayedThisFrame = true;
                    }
                    break;
                }
            }
        }
    }
}
