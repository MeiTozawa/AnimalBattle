using System;
using AnimalBattle.Abilities;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     多フレーム持続判定・近接攻撃専用の衝突判定 ECS システム。
///     <see cref="HitHistory" /> を保持する攻撃判定（近接斬撃や貫通攻撃）を対象とし、
///     同一エンティティに対する毎フレームの多段ヒット（重複ダメージ）を防止しつつ、
///     <see cref="Hurtbox" /> との幾何学的交差判定およびダメージ適用を行います。
/// </summary>
public static class PersistentCollisionSystem
{
    /// <summary>
    ///     持続・近接衝突判定システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World" /></returns>
    public static World AddPersistentCollisionSystem(this World world)
    {
        CombatCollisionSystem.CollisionContext context = new(world);

        world.System<Transform, Hitbox, HitHistory>("PersistentCollisionSystem")
            .Without<RecycleTag>()
            .Kind(Ecs.OnUpdate)
            .Iter((Iter it, Field<Transform> atkTrans, Field<Hitbox> hitboxes, Field<HitHistory> histories) =>
            {
                context.Refresh();
                if (context.TargetCount == 0) return;

                var count = it.Count();

                for (var i = 0; i < count; i++)
                {
                    ref var hitbox = ref hitboxes[i];
                    if (!hitbox.IsActive) continue;

                    ref var trans = ref atkTrans[i];
                    ref var history = ref histories[i];
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

                        // 単発弾（DestroyOnHit = true）は履歴検査をスキップ
                        // 貫通・近接判定（DestroyOnHit = false）は同一対象への重複多段ヒットを防止
                        if (!hitbox.DestroyOnHit && history.HasHit(target.Entity.Id)) continue;

                        // AABB Broadphase 枝刈り（空間粗判定による早期リターン）
                        if (MathF.Abs(aCenterX - target.CenterX) > aHalfW + target.HalfW ||
                            MathF.Abs(aCenterY - target.CenterY) > aHalfH + target.HalfH)
                            continue;

                        var vicEntity = target.Entity;
                        if (!vicEntity.IsAlive()) continue;
                        ref readonly var hp = ref vicEntity.Get<Health>();
                        if (hp.Current <= 0.0f) continue;
                        if (vicEntity.Has<AbilitySystemComponent>() && vicEntity.Get<AbilitySystemComponent>().HasTag(GameplayTag.Untargetable)) continue;

                        ref var vicTrans = ref vicEntity.GetMut<Transform>();
                        ref var hurtbox = ref vicEntity.GetMut<Hurtbox>();

                        if (PhysicsService.Intersects(trans, hitbox.Shape, vicTrans, hurtbox.Shape))
                        {
                            if (!hitbox.DestroyOnHit)
                            {
                                history.TryRecordHit(vicEntity.Id);
                            }

                            CombatCollisionSystem.ApplyHit(world, vicEntity, ref vicTrans, in trans, ref hurtbox, in hitbox,
                                ref shouldDestroyAttacker);
                            if (shouldDestroyAttacker) break;
                        }
                    }

                    if (shouldDestroyAttacker) atkEntity.Add<RecycleTag>();
                }
            });

        return world;
    }
}
