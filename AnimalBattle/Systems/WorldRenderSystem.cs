using System;
using System.Drawing;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// アリーナ足場、Boss (オーラ・無敵シールド・予兆リング・スプライト・被弾フラッシュ・突進残影)、
/// プレイヤー自機 (スプライト・無敵点滅・判定点)、および 8 方向プレイヤー弾・Boss 弾幕を描画する ECS 描画 System。
/// </summary>
public static class WorldRenderSystem
{
    /// <summary>
    /// ワールド描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    public static void Register(World world)
    {
        world.System("WorldRenderSystem")
             .Kind(Ecs.OnStore)
             .Run((Iter it) =>
             {
                 RenderConfig renderCfg = ConfigManager.Render;
                 PlatformRenderConfig platCfg = renderCfg.Platform;
                 BossEffectsRenderConfig bossFx = renderCfg.BossEffects;
                 PlayerRenderConfig playerRenderCfg = renderCfg.Player;
                 BulletRenderConfig bulletCfg = renderCfg.Bullets;

                 world.QueryBuilder<Transform, CollisionBox>()
                      .With<PlatformTag>()
                      .Build()
                      .Each((ref Transform trans, ref CollisionBox col) =>
                      {
                          int x1 = (int)(trans.X - col.Width / 2.0f);
                          int y1 = (int)(trans.Y - col.Height / 2.0f);
                          int x2 = (int)(trans.X + col.Width / 2.0f);
                          int y2 = (int)(trans.Y + col.Height / 2.0f);

                          GameEngine.DrawBox(x1, y1, x2, y2, platCfg.FillColor, true);
                          GameEngine.DrawBox(x1, y1, x2, y2, platCfg.BorderColor, false);
                          GameEngine.DrawBox(
                              x1 + platCfg.TopHighlightOffset,
                              y1 + platCfg.TopHighlightOffset,
                              x2 - platCfg.TopHighlightOffset,
                              y1 + platCfg.TopHighlightOffset + platCfg.TopHighlightThickness,
                              platCfg.TopHighlightColor,
                              true);
                      });

                 world.QueryBuilder<Transform, Health, BossStats, BossStateMachine, Sprite>()
                      .With<BossTag>()
                      .Build()
                      .Each((ref Transform trans, ref Health bHealth, ref BossStats stats, ref BossStateMachine fsm, ref Sprite sprite) =>
                      {
                          Color auraColor = stats.Phase switch
                          {
                              1 => bossFx.Phase1AuraColor,
                              2 => bossFx.Phase2AuraColor,
                              _ => bossFx.Phase3AuraColor
                          };

                          int bx = (int)trans.X;
                          int by = (int)trans.Y;

                          if (fsm.CurrentState == BossStateKind.Telegraphing)
                          {
                              float progress = Math.Clamp(fsm.StateTimer / Math.Max(0.01f, fsm.StateDuration), 0.0f, 1.0f);
                              int ringRadius = (int)(bossFx.TelegraphStartRadius - progress * bossFx.TelegraphEndRadiusDelta);
                              GameEngine.DrawCircle(bx, by, ringRadius, bossFx.TelegraphRingColor, false);
                              GameEngine.DrawCircle(bx, by, ringRadius - 2, bossFx.TelegraphInnerRingColor, false);
                              GameEngine.DrawCircle(bx, by, (int)bossFx.TelegraphCoreRadius, bossFx.TelegraphCoreColor, true);

                              if (fsm.CurrentSkillName == "DiveRam")
                              {
                                  int tx = (int)fsm.TargetX;
                                  int ty = (int)fsm.TargetY;
                                  int lockRadius = (int)(45.0f - progress * 15.0f);
                                  GameEngine.DrawCircle(tx, ty, lockRadius, ColorPalette.RedBright, false);
                                  GameEngine.DrawCircle(tx, ty, lockRadius - 2, ColorPalette.Gold, false);
                              }
                          }
                          else if (fsm.CurrentState == BossStateKind.Casting && fsm.CurrentSkillName == "DiveRam")
                          {
                              GameEngine.DrawCircle(bx, by, 90, ColorPalette.RedBright, false);
                              GameEngine.DrawCircle(bx, by, 85, ColorPalette.Gold, false);
                              GameEngine.DrawCircle(bx, by, 60, ColorPalette.RedTelegraphCore, true);
                          }
                          else if (fsm.CurrentState == BossStateKind.PhaseTransition || fsm.IsInvulnerable)
                          {
                              int shieldRadius = (int)(bossFx.ShieldBaseRadius + MathF.Sin(fsm.StateTimer * bossFx.ShieldPulseSpeed) * bossFx.ShieldPulseAmp);
                              GameEngine.DrawCircle(bx, by, shieldRadius, bossFx.ShieldOuterColor, false);
                              GameEngine.DrawCircle(bx, by, shieldRadius - 3, bossFx.ShieldInnerColor, false);
                              GameEngine.DrawCircle(bx, by, shieldRadius - 8, bossFx.ShieldFillColor, true);
                          }
                          else if (fsm.CurrentState == BossStateKind.Recovering)
                          {
                              GameEngine.DrawCircle(bx, by, (int)bossFx.RecoveringAuraRadius, bossFx.RecoveringAuraColor, true);
                          }

                          int auraRadius = (int)(bossFx.AuraBaseRadius + MathF.Sin(stats.HoverTimer * bossFx.AuraPulseSpeed) * bossFx.AuraPulseAmp);
                          GameEngine.DrawCircle(bx, by, auraRadius, auraColor, false);
                          GameEngine.DrawCircle(bx, by, auraRadius - 2, auraColor, false);

                          if (sprite.IsVisible && sprite.GraphHandle >= 0 && bHealth.IsAlive)
                          {
                              double bossAngle = MathF.Sin(stats.HoverTimer * bossFx.BossHoverAngleSpeed) * bossFx.BossHoverAngleAmp;
                              GameEngine.DrawRectRotaGraph(
                                  bx,
                                  by,
                                  sprite.SrcRect.X1,
                                  sprite.SrcRect.Y1,
                                  sprite.SrcRect.Width,
                                  sprite.SrcRect.Height,
                                  bossFx.BossSpriteScale,
                                  bossAngle,
                                  sprite.GraphHandle,
                                  true,
                                  false);
                          }

                          if (stats.HitFlashTimer > 0.0f)
                          {
                              GameEngine.DrawCircle(bx, by, (int)bossFx.HitFlashRadius, bossFx.HitFlashColor, true);
                          }
                      });

                 // 3. プレイヤー自機描画
                 world.QueryBuilder<Transform, Health, PlayerState, Sprite>()
                      .With<PlayerTag>()
                      .Build()
                      .Each((ref Transform trans, ref Health health, ref PlayerState playerState, ref Sprite sprite) =>
                      {
                          if (!health.IsAlive || !sprite.IsVisible || sprite.GraphHandle < 0)
                          {
                              return;
                          }

                          // 無敵時間点滅演出（i-frames flickering）
                          bool shouldDrawSprite = !health.IsInvincible || ((int)(health.InvincibleTimer * 30.0f) % 2 == 0);

                          if (shouldDrawSprite)
                          {
                              GameEngine.DrawRectRotaGraph(
                                  (int)trans.X,
                                  (int)trans.Y,
                                  sprite.SrcRect.X1,
                                  sprite.SrcRect.Y1,
                                  sprite.SrcRect.Width,
                                  sprite.SrcRect.Height,
                                  trans.ScaleX,
                                  trans.Rotation,
                                  sprite.GraphHandle,
                                  true,
                                  sprite.FlipX);
                          }

                          if (health.IsInvincible)
                          {
                              GameEngine.DrawCircle((int)trans.X, (int)trans.Y, 24, ColorPalette.InvincibleShield, false);
                          }
                      });

                 // 4. プレイヤー銃弾描画（8 方向対応グロートレイル）
                 world.QueryBuilder<Transform, Velocity>()
                      .With<PlayerBulletTag>()
                      .Build()
                      .Each((ref Transform trans, ref Velocity vel) =>
                      {
                          int px = (int)trans.X;
                          int py = (int)trans.Y;

                          float vLen = MathF.Sqrt(vel.Vx * vel.Vx + vel.Vy * vel.Vy);
                          if (vLen > 0.001f)
                          {
                              float normVx = vel.Vx / vLen;
                              float normVy = vel.Vy / vLen;
                              int trailX = (int)(px - normVx * bulletCfg.PlayerBulletTrailHalfWidth);
                              int trailY = (int)(py - normVy * bulletCfg.PlayerBulletTrailHalfWidth);
                              GameEngine.DrawCircle(trailX, trailY, (int)(bulletCfg.PlayerBulletOuterRadius * 0.7f), bulletCfg.PlayerBulletTrailColor, true);
                          }

                          GameEngine.DrawCircle(px, py, (int)bulletCfg.PlayerBulletOuterRadius, bulletCfg.PlayerBulletOuterColor, true);
                          GameEngine.DrawCircle(px, py, (int)bulletCfg.PlayerBulletInnerRadius, bulletCfg.PlayerBulletInnerColor, true);
                      });

                 // 5. Boss 弾幕描画
                 world.QueryBuilder<Transform, Bullet>()
                      .With<BossBulletTag>()
                      .Build()
                      .Each((ref Transform trans, ref Bullet bullet) =>
                      {
                          int bx = (int)trans.X;
                          int by = (int)trans.Y;
                          int radius = (int)bullet.Radius;

                          GameEngine.DrawCircle(bx, by, radius + bulletCfg.BossBulletOutlinePadding, bullet.OutlineColor, true);
                          GameEngine.DrawCircle(bx, by, radius, bullet.FillColor, true);
                          int coreRadius = Math.Max(bulletCfg.BossBulletCoreMinRadius, (int)(radius * bulletCfg.BossBulletCoreRadiusRatio));
                          GameEngine.DrawCircle(bx, by, coreRadius, bulletCfg.BossBulletCoreColor, true);
                      });
             });
    }
}
