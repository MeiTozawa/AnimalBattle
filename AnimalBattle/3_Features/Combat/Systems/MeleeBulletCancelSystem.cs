using System;
using AnimalBattle.Combat;
using AnimalBattle.Components;
using AnimalBattle.Core;
using AnimalBattle.Core.Geometry;
using AnimalBattle.Effects;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     Bunny 自機プレイヤーの近接攻撃判定 (<see cref="PlayerMeleeTag"/>) と
///     敵弾幕 (<see cref="BossBulletTag"/>) の衝突交差を判定し、弾幕を斬落消去するシステム。
///     地面衝撃波 (<see cref="IndestructibleBulletTag"/>) は対象外とし、消去時は <see cref="RecycleTag"/> を
///     付与してフレーム末尾で安全回収すると共に、被弾火花演出と音響効果を再生します。
/// </summary>
public static class MeleeBulletCancelSystem
{
    private struct MeleeTarget
    {
        public Transform Transform;
        public CollisionShape Shape;
        public Vec2f Center;
        public Vec2f HalfExtents;
    }

    private static Query<Transform, Hitbox> _meleeQuery;
    private static Query<Transform, Hitbox, Bullet> _bulletQuery;
    private static readonly MeleeTarget[] _melees = new MeleeTarget[8];
    private static int _meleeCount;
    private static bool _sfxPlayedThisFrame;

    /// <summary>
    ///     近接消弾システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World"/></returns>
    public static World AddMeleeBulletCancelSystem(this World world)
    {
        _meleeQuery = world.QueryBuilder<Transform, Hitbox>()
            .With<PlayerMeleeTag>()
            .Without<RecycleTag>()
            .Build();

        _bulletQuery = world.QueryBuilder<Transform, Hitbox, Bullet>()
            .With<BossBulletTag>()
            .Without<RecycleTag>()
            .Build();

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
        _meleeCount = 0;
        _meleeQuery.Iter(CollectMelees);
        if (_meleeCount == 0) return;

        _sfxPlayedThisFrame = false;
        _bulletQuery.Iter(ProcessBullets);
    }

    private static void CollectMelees(Iter it, Field<Transform> trans, Field<Hitbox> hitboxes)
    {
        var count = it.Count();
        for (var i = 0; i < count; i++)
        {
            if (_meleeCount >= _melees.Length) break;
            ref readonly var h = ref hitboxes[i];
            if (!h.IsActive) continue;

            ref readonly var t = ref trans[i];
            var half = h.Shape.Type == CollisionShapeType.Box ? h.Shape.Size * 0.5f : new Vec2f(h.Shape.Radius, h.Shape.Radius);
            var c = t.Position + h.Shape.Offset;

            _melees[_meleeCount++] = new MeleeTarget
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

            for (var m = 0; m < _meleeCount; m++)
            {
                ref readonly var melee = ref _melees[m];

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

                    if (!_sfxPlayedThisFrame)
                    {
                        SoundService.PlaySfx(SfxType.BossHit);
                        _sfxPlayedThisFrame = true;
                    }
                    break;
                }
            }
        }
    }
}
