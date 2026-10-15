using AnimalBattle.Combat;
using AnimalBattle.Components;
using AnimalBattle.Core;
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
        public float CenterX;
        public float CenterY;
        public float HalfW;
        public float HalfH;
    }

    private sealed class CancelContext
    {
        private readonly Ecs.IterFieldCallback<Transform, Hitbox> _collectMeleeIter;
        private readonly Ecs.IterFieldCallback<Transform, Hitbox> _processBulletsIter;
        private readonly World _world;

        public readonly Query<Transform, Hitbox> MeleeQuery;
        public readonly Query<Transform, Hitbox> BulletQuery;
        public readonly MeleeTarget[] Melees = new MeleeTarget[8];
        public int MeleeCount;
        private bool _sfxPlayedThisFrame;

        public CancelContext(World world)
        {
            _world = world;

            MeleeQuery = world.QueryBuilder<Transform, Hitbox>()
                .With<PlayerMeleeTag>()
                .Without<RecycleTag>()
                .Build();

            BulletQuery = world.QueryBuilder<Transform, Hitbox>()
                .With<BossBulletTag>()
                .Without<IndestructibleBulletTag>()
                .Without<RecycleTag>()
                .Build();

            _collectMeleeIter = CollectMelees;
            _processBulletsIter = ProcessBullets;
        }

        public void Execute()
        {
            MeleeCount = 0;
            MeleeQuery.Iter(_collectMeleeIter);
            if (MeleeCount == 0) return;

            _sfxPlayedThisFrame = false;
            BulletQuery.Iter(_processBulletsIter);
        }

        private void CollectMelees(Iter it, Field<Transform> trans, Field<Hitbox> hitboxes)
        {
            var count = it.Count();
            for (var i = 0; i < count; i++)
            {
                if (MeleeCount >= Melees.Length) break;
                ref readonly var h = ref hitboxes[i];
                if (!h.IsActive) continue;

                ref readonly var t = ref trans[i];
                var halfW = h.Shape.Type == CollisionShapeType.Box ? h.Shape.Width * 0.5f : h.Shape.Radius;
                var halfH = h.Shape.Type == CollisionShapeType.Box ? h.Shape.Height * 0.5f : h.Shape.Radius;
                var cX = t.X + h.Shape.OffsetX;
                var cY = t.Y + h.Shape.OffsetY;

                Melees[MeleeCount++] = new MeleeTarget
                {
                    Transform = t,
                    Shape = h.Shape,
                    CenterX = cX,
                    CenterY = cY,
                    HalfW = halfW,
                    HalfH = halfH
                };
            }
        }

        private void ProcessBullets(Iter it, Field<Transform> bTransforms, Field<Hitbox> bHitboxes)
        {
            var count = it.Count();
            for (var b = 0; b < count; b++)
            {
                ref readonly var bHitbox = ref bHitboxes[b];
                if (!bHitbox.IsActive) continue;

                ref readonly var bt = ref bTransforms[b];
                var bEntity = it.Entity(b);

                var bCenterX = bt.X + bHitbox.Shape.OffsetX;
                var bCenterY = bt.Y + bHitbox.Shape.OffsetY;
                var bHalfW = bHitbox.Shape.Type == CollisionShapeType.Box ? bHitbox.Shape.Width * 0.5f : bHitbox.Shape.Radius;
                var bHalfH = bHitbox.Shape.Type == CollisionShapeType.Box ? bHitbox.Shape.Height * 0.5f : bHitbox.Shape.Radius;

                for (var m = 0; m < MeleeCount; m++)
                {
                    ref readonly var melee = ref Melees[m];

                    // AABB 粗判定による早期枝刈り
                    if (MathF.Abs(melee.CenterX - bCenterX) > melee.HalfW + bHalfW ||
                        MathF.Abs(melee.CenterY - bCenterY) > melee.HalfH + bHalfH)
                    {
                        continue;
                    }

                    // 精確な幾何交差判定
                    if (PhysicsService.Intersects(melee.Transform, melee.Shape, bt, bHitbox.Shape))
                    {
                        bEntity.Add<RecycleTag>();
                        CombatSpawner.SpawnHitSparkEffect(_world, bt.X, bt.Y);

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

    /// <summary>
    ///     近接消弾システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World"/></returns>
    public static World AddMeleeBulletCancelSystem(this World world)
    {
        var context = new CancelContext(world);

        world.System("MeleeBulletCancelSystem")
            .Kind(Ecs.OnUpdate)
            .Iter((Iter it) =>
            {
                context.Execute();
            });

        return world;
    }
}
