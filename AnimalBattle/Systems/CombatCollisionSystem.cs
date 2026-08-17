using System;
using System.Drawing;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;

namespace AnimalBattle.Systems;

/// <summary>
/// プレイヤー銃弾 vs Boss 矩形、Boss 弾幕 vs プレイヤー、および Boss 本体接触突進 vs プレイヤーの衝突判定、HP 減算、無敵時間、撃破・死亡判定を統括する ECS System。
/// </summary>
public static class CombatCollisionSystem
{
    /// <summary>
    /// 戦闘衝突判定システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    public static void Register(World world)
    {
        world.System("CombatCollisionSystem")
             .Kind(Ecs.OnUpdate)
             .Run((Iter it) =>
             {
                 BossConfig bCfg = ConfigManager.Boss;
                 PlayerConfig pCfg = ConfigManager.Player;

                 // 1. プレイヤー銃弾 vs Boss
                 float bossX = 0.0f;
                 float bossY = 0.0f;
                 float bossLeft = 0.0f;
                 float bossRight = 0.0f;
                 float bossTop = 0.0f;
                 float bossBottom = 0.0f;
                 bool hasBoss = false;
                 bool isInvulnerable = false;

                 world.QueryBuilder<Transform, Health, BossStats, BossStateMachine>()
                      .With<BossTag>()
                      .Build()
                      .Each((ref Transform bTrans, ref Health bHealth, ref BossStats bStats, ref BossStateMachine bFsm) =>
                      {
                          if (bHealth.IsAlive)
                          {
                              bossX = bTrans.X;
                              bossY = bTrans.Y;
                              bossLeft = bTrans.X - bCfg.HitboxWidth / 2.0f;
                              bossRight = bTrans.X + bCfg.HitboxWidth / 2.0f;
                              bossTop = bTrans.Y - bCfg.HitboxHeight / 2.0f;
                              bossBottom = bTrans.Y + bCfg.HitboxHeight / 2.0f;
                              hasBoss = true;
                              isInvulnerable = bFsm.IsInvulnerable;
                          }
                      });

                 if (hasBoss)
                 {
                     float damageTaken = 0.0f;
                     world.QueryBuilder<Transform>()
                          .With<PlayerBulletTag>()
                          .Build()
                          .Each((Entity bulletEntity, ref Transform bulletTrans) =>
                          {
                              if (bulletTrans.X >= bossLeft && bulletTrans.X <= bossRight &&
                                  bulletTrans.Y >= bossTop && bulletTrans.Y <= bossBottom)
                              {
                                  if (!isInvulnerable)
                                  {
                                      damageTaken += 1.0f;
                                  }
                                  CombatEffectHelper.SpawnHitSparkEffect(world, bulletTrans.X, bulletTrans.Y);
                                  bulletEntity.Destruct();
                              }
                          });

                     if (damageTaken > 0.0f)
                     {
                         world.QueryBuilder<Health, BossStats>()
                              .With<BossTag>()
                              .Build()
                              .Each((ref Health bHealth, ref BossStats bStats) =>
                              {
                                  bHealth.Current -= damageTaken;
                                  bStats.HitFlashTimer = bCfg.HitFlashDuration;
                                  if (!bHealth.IsAlive)
                                  {
                                      bHealth.Current = 0.0f;
                                      CombatEffectHelper.ClearAllBossBullets(world);
                                      CombatEffectHelper.SpawnVictoryExplosions(world, bossX, bossY);
                                  }
                              });
                     }
                 }

                 // 2. 敵弾 & Boss 本体突進接触 vs プレイヤー判定点 (HP 減算 & 無敵時間判定)
                 float playerX = 0.0f;
                 float playerY = 0.0f;
                 bool isPlayerAlive = false;
                 bool isPlayerInvincible = false;

                 world.QueryBuilder<Transform, Health, PlayerState>()
                      .With<PlayerTag>()
                      .Build()
                      .Each((ref Transform pTrans, ref Health pHealth, ref PlayerState pState) =>
                      {
                          if (pHealth.IsAlive)
                          {
                              playerX = pTrans.X;
                              playerY = pTrans.Y;
                              isPlayerAlive = true;
                              isPlayerInvincible = pHealth.IsInvincible;
                          }
                      });

                 if (isPlayerAlive && !isPlayerInvincible)
                 {
                     bool playerHit = false;
                     float pRadius = pCfg.HitboxRadius;

                     // 2.1 弾幕との衝突判定
                     world.QueryBuilder<Transform, Bullet>()
                          .With<BossBulletTag>()
                          .Build()
                          .Each((ref Transform bulletTrans, ref Bullet bullet) =>
                          {
                              if (playerHit)
                              {
                                  return;
                              }

                              float dx = bulletTrans.X - playerX;
                              float dy = bulletTrans.Y - playerY;
                              float distanceSq = dx * dx + dy * dy;
                              float combinedRadius = bullet.Radius + pRadius;

                              if (distanceSq <= combinedRadius * combinedRadius)
                              {
                                  playerHit = true;
                              }
                          });

                     // 2.2 Boss 本体との接触・突進衝突判定
                     if (!playerHit && hasBoss)
                     {
                         if (playerX >= bossLeft - pRadius && playerX <= bossRight + pRadius &&
                             playerY >= bossTop - pRadius && playerY <= bossBottom + pRadius)
                         {
                             playerHit = true;
                         }
                     }

                     if (playerHit)
                     {
                         world.QueryBuilder<Health, PlayerState, Sprite>()
                              .With<PlayerTag>()
                              .Build()
                              .Each((ref Health pHealth, ref PlayerState pState, ref Sprite pSprite) =>
                              {
                                  if (!pHealth.IsInvincible)
                                  {
                                      pHealth.Current -= 1.0f;
                                      pHealth.InvincibleTimer = pCfg.InvincibleDuration;
                                      CombatEffectHelper.SpawnHitSparkEffect(world, playerX, playerY);

                                      if (!pHealth.IsAlive)
                                      {
                                          pHealth.Current = 0.0f;
                                          pSprite.IsVisible = false;
                                          CombatEffectHelper.SpawnPlayerDeathParticles(world, playerX, playerY);
                                      }
                                  }
                              });
                     }
                 }
             });
    }
}
