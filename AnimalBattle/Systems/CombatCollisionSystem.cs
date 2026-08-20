using System;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core.Physics;

namespace AnimalBattle.Systems;

/// <summary>
/// Bevy スタイルのコンポーネント駆動 (Component-Driven) 戦闘衝突判定システム。
/// 攻撃判定 <see cref="Hitbox"/>（有効状態 IsActive 検査含む）と 被弾判定 <see cref="Hurtbox"/> 間の幾何学的交差、
/// 陣営判定（<see cref="Faction"/>）、HP 減算、無敵時間、撃破演出を一括処理します。
/// </summary>
public static class CombatCollisionSystem
{
    /// <summary>
    /// 戦闘衝突判定システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World"/></returns>
    public static World AddCombatCollisionSystem(this World world)
    {
        Query<Transform, Hurtbox, Health> hurtboxQuery =
            world.QueryBuilder<Transform, Hurtbox, Health>()
                 .Build();

        world.System<Transform, Hitbox>("CombatCollisionSystem")
             .Kind(Ecs.OnUpdate)
             .Each((Entity atkEntity, ref Transform atkTrans, ref Hitbox hitbox) =>
             {
                 BossConfig bCfg = ConfigManager.Boss;
                 PlayerConfig pCfg = ConfigManager.Player;

                 Transform localAtkTrans = atkTrans;
                 Hitbox localHitbox = hitbox;

                 // 1. 攻撃判定が無効（休眠中）の場合は判定をスキップ
                 if (!localHitbox.IsActive)
                 {
                     return;
                 }

                 bool shouldDestroyAttacker = false;

                 hurtboxQuery.Each((Entity vicEntity, ref Transform vicTrans, ref Hurtbox hurtbox, ref Health vicHealth) =>
                 {
                     // 2. 同一陣営または死亡済みの場合は無視
                     if (localHitbox.Faction == hurtbox.Faction || !vicHealth.IsAlive)
                     {
                         return;
                     }

                     // 3. 無敵時間中の場合は無視
                     if (vicHealth.IsInvincible)
                     {
                         return;
                     }

                     // 4. Boss が状態機フェーズ無敵中の場合は無視
                     if (vicEntity.Has<BossStateMachine>())
                     {
                         ref readonly BossStateMachine fsm = ref vicEntity.Get<BossStateMachine>();
                         if (fsm.IsInvulnerable)
                         {
                             return;
                         }
                     }

                     // 5. 幾何学的交差判定 (双方の Offset を加味した Circle vs Circle, Box vs Box, Circle vs Box)
                     if (PhysicsService.Intersects(localAtkTrans, localHitbox.Shape, vicTrans, hurtbox.Shape))
                     {
                         // 5.1 ダメージ適用
                         vicHealth.Current -= localHitbox.Damage;

                         // 5.2 被弾演出 (火花)
                         CombatEffectHelper.SpawnHitSparkEffect(world, vicTrans.X + hurtbox.Shape.OffsetX, vicTrans.Y + hurtbox.Shape.OffsetY);

                         // 5.3 被弾者種別に応じた固有リアクション
                         if (vicEntity.Has<PlayerTag>())
                         {
                             vicHealth.InvincibleTimer = pCfg.InvincibleDuration;
                             if (!vicHealth.IsAlive)
                             {
                                 vicHealth.Current = 0.0f;
                                 if (vicEntity.Has<Sprite>())
                                 {
                                     ref Sprite spr = ref vicEntity.GetMut<Sprite>();
                                     spr.IsVisible = false;
                                 }
                                 CombatEffectHelper.SpawnPlayerDeathParticles(world, vicTrans.X, vicTrans.Y);
                             }
                         }
                         else if (vicEntity.Has<BossStats>())
                         {
                             ref BossStats bStats = ref vicEntity.GetMut<BossStats>();
                             bStats.HitFlashTimer = bCfg.HitFlashDuration;

                             if (!vicHealth.IsAlive)
                             {
                                 vicHealth.Current = 0.0f;
                                 CombatEffectHelper.ClearAllBossBullets(world);
                                 CombatEffectHelper.SpawnVictoryExplosions(world, vicTrans.X, vicTrans.Y);
                             }
                         }

                         // 5.4 攻撃エンティティの消滅判定（銃弾など）
                         if (localHitbox.DestroyOnHit)
                         {
                             shouldDestroyAttacker = true;
                         }
                     }
                 });

                 if (shouldDestroyAttacker)
                 {
                     atkEntity.Destruct();
                 }
             });

        return world;
    }
}
