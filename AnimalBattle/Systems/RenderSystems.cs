using System;
using System.Drawing;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// アリーナ足場、Boss (エフェクト・スプライト・弾幕)、自機 (点滅・光環・銃弾)、
/// 画面下部大 HP バー、およびモーダル UI の描画を担当する ECS 描画 Routine (System)。
/// 体力情報は独立した <see cref="Health"/> コンポーネントから取得し、色や表示テキストは <see cref="RenderConfig"/> から動的取得します。
/// 全ての描画色に <see cref="Color"/> を採用しています。
/// </summary>
public static class RenderSystems
{
    /// <summary>
    /// 全描画関連の ECS 常駐 Routine (System) を Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    public static void RegisterSystems(World world)
    {
        RegisterWorldRenderSystem(world);
        RegisterHudRenderSystem(world);
    }

    private static void RegisterWorldRenderSystem(World world)
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

                 // 1. アリーナプラットフォーム（足場）描画
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

                 // 2. Boss 視覚エフェクトおよびスプライト描画
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
                              GameEngine.DrawCircle((int)trans.X, (int)trans.Y, 24, Color.FromArgb(0x88, 0x00, 0xFF, 0xFF), false);
                          }

                          GameEngine.DrawCircle((int)trans.X, (int)trans.Y, (int)playerRenderCfg.HitboxCoreRadius, playerRenderCfg.HitboxCoreColor, true);
                          GameEngine.DrawCircle((int)trans.X, (int)trans.Y, (int)playerRenderCfg.HitboxBorderRadius, playerRenderCfg.HitboxBorderColor, false);
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

    private static void RegisterHudRenderSystem(World world)
    {
        world.System("HudRenderSystem")
             .Kind(Ecs.OnStore)
             .Run((Iter it) =>
             {
                 RenderConfig renderCfg = ConfigManager.Render;
                 HudRenderConfig hudCfg = renderCfg.Hud;
                 BossHpBarRenderConfig hpCfg = renderCfg.BossHpBar;
                 ModalRenderConfig modalCfg = renderCfg.Modals;

                 // 1. 操作説明とタイトル
                 GameEngine.DrawString(hudCfg.TitleX, hudCfg.TitleY, hudCfg.TitleText, hudCfg.TitleColor);
                 GameEngine.DrawString(hudCfg.TitleX, hudCfg.TitleY + hudCfg.LineSpacing, hudCfg.MoveHelpText, hudCfg.HelpTextColor);
                 GameEngine.DrawString(hudCfg.TitleX, hudCfg.TitleY + hudCfg.LineSpacing * 2, hudCfg.JumpHelpText, hudCfg.HelpTextColor);
                 GameEngine.DrawString(hudCfg.TitleX, hudCfg.TitleY + hudCfg.LineSpacing * 3, hudCfg.ShootHelpText, hudCfg.HelpTextColor);
                 GameEngine.DrawString(hudCfg.TitleX, hudCfg.TitleY + hudCfg.LineSpacing * 4, hudCfg.RestartHelpText, hudCfg.RestartHelpColor);

                 // 2. プレイヤージャンプ残数表示
                 world.QueryBuilder<DoubleJump>()
                      .With<PlayerTag>()
                      .Build()
                      .Each((ref DoubleJump doubleJump) =>
                      {
                          string jumpSymbol = doubleJump.RemainingJumps switch
                          {
                              2 => "[ ●  ● ]",
                              1 => "[ ●  ○ ]",
                              _ => "[ ○  ○ ]"
                          };
                          Color jumpColor = doubleJump.RemainingJumps > 0 ? hudCfg.DoubleJumpActiveColor : hudCfg.DoubleJumpInactiveColor;
                          GameEngine.DrawString(hudCfg.TitleX, hudCfg.DoubleJumpY, $"DOUBLE JUMP: {jumpSymbol}", jumpColor);
                      });

                 // 3. プレイヤー HP 表示
                 world.QueryBuilder<Health>()
                      .With<PlayerTag>()
                      .Build()
                      .Each((ref Health health) =>
                      {
                          string hpSymbol = (int)health.Current switch
                          {
                              3 => "[ ●  ●  ● ]",
                              2 => "[ ●  ●  ○ ]",
                              1 => "[ ●  ○  ○ ]",
                              _ => "[ ○  ○  ○ ]"
                          };
                          Color hpColor = (int)health.Current switch
                          {
                              3 => Color.FromArgb(0xFF, 0x00, 0xFF, 0x88),
                              2 => Color.FromArgb(0xFF, 0xFF, 0xD7, 0x00),
                              1 => Color.FromArgb(0xFF, 0xFF, 0x33, 0x33),
                              _ => Color.FromArgb(0xFF, 0x88, 0x88, 0x88)
                          };
                          string invBadge = health.IsInvincible ? $" [INVINCIBLE {health.InvincibleTimer:F1}s]" : "";
                          GameEngine.DrawString(hudCfg.TitleX, hudCfg.DoubleJumpY + 20, $"PLAYER HP:   {hpSymbol} ({(int)health.Current}/{(int)health.Max}){invBadge}", hpColor);
                      });

                 // 3. 画面下部 Boss 大 HP バーおよび状態表示
                 world.QueryBuilder<Health, BossStats, BossStateMachine>()
                      .With<BossTag>()
                      .Build()
                      .Each((ref Health health, ref BossStats stats, ref BossStateMachine fsm) =>
                      {
                          int barX1 = hpCfg.BarX1;
                          int barY1 = hpCfg.BarY1;
                          int barX2 = hpCfg.BarX2;
                          int barY2 = hpCfg.BarY2;
                          int totalWidth = barX2 - barX1;

                          // 3.1 Boss 情報ヘッダー (HP バー上部)
                          string bossTitle = $"BOSS: ANCIENT CAT OVERLORD [PHASE {stats.Phase}/3]";
                          string hpText = $"HP: {health.Current:F0} / {health.Max:F0}";
                          GameEngine.DrawString(barX1, barY1 - hpCfg.HeaderOffsetY, bossTitle, hpCfg.TitleColor);
                          GameEngine.DrawString(barX2 - 170, barY1 - hpCfg.HeaderOffsetY, hpText, hpCfg.HpTextColor);

                          string stateName = fsm.CurrentState switch
                          {
                              BossStateKind.Hovering => "[STATE: HOVERING (IDLE)]",
                              BossStateKind.Telegraphing => $"[STATE: TELEGRAPHING (WIND-UP: {fsm.CurrentSkillName})]",
                              BossStateKind.Casting => $"[STATE: CASTING ({fsm.CurrentSkillName})]",
                              BossStateKind.Recovering => "[STATE: RECOVERING (VULNERABLE)]",
                              BossStateKind.PhaseTransition => "[STATE: PHASE TRANSITION (INVULNERABLE)]",
                              _ => "[STATE: UNKNOWN]"
                          };

                          Color stateBadgeColor = fsm.CurrentState switch
                          {
                              BossStateKind.Telegraphing => hpCfg.StateTelegraphingColor,
                              BossStateKind.Casting => hpCfg.StateCastingColor,
                              BossStateKind.Recovering => hpCfg.StateRecoveringColor,
                              BossStateKind.PhaseTransition => hpCfg.StatePhaseTransitionColor,
                              _ => hpCfg.StateDefaultColor
                          };

                          GameEngine.DrawString(barX1 + hpCfg.StateBadgeOffsetX, barY1 - hpCfg.HeaderOffsetY, $"{stateName} ({fsm.StateTimer:F1}s/{fsm.StateDuration:F1}s)", stateBadgeColor);

                          // 3.2 Boss HP バー背景および枠線
                          GameEngine.DrawBox(barX1 - hpCfg.BgPadding, barY1 - hpCfg.BgPadding, barX2 + hpCfg.BgPadding, barY2 + hpCfg.BgPadding, hpCfg.BgColor, true);
                          GameEngine.DrawBox(barX1 - hpCfg.BgPadding, barY1 - hpCfg.BgPadding, barX2 + hpCfg.BgPadding, barY2 + hpCfg.BgPadding, hpCfg.BorderColor, false);

                          // 3.3 Boss HP ゲージ塗りつぶし
                          Color hpFillColor = stats.Phase switch
                          {
                              1 => renderCfg.BossEffects.Phase1AuraColor,
                              2 => renderCfg.BossEffects.Phase2AuraColor,
                              _ => renderCfg.BossEffects.Phase3AuraColor
                          };

                          float hpRatio = health.Ratio;
                          int fillWidth = (int)(totalWidth * hpRatio);
                          if (fillWidth > 0)
                          {
                              GameEngine.DrawBox(barX1, barY1, barX1 + fillWidth, barY2, hpFillColor, true);
                              GameEngine.DrawBox(barX1, barY1, barX1 + fillWidth, barY1 + hpCfg.TopHighlightThickness, hpCfg.TopHighlightColor, true);
                          }

                          // 3.4 Phase 分割マーカー (1/3, 2/3)
                          int p1Split = barX1 + totalWidth / 3;
                          int p2Split = barX1 + (totalWidth * 2) / 3;
                          GameEngine.DrawBox(p1Split - 1, barY1, p1Split + 1, barY2, hpCfg.BorderColor, true);
                          GameEngine.DrawBox(p2Split - 1, barY1, p2Split + 1, barY2, hpCfg.BorderColor, true);
                      });

                  // 4. ゲームオーバーおよび勝利モーダル
                  world.QueryBuilder<Health>()
                       .With<PlayerTag>()
                       .Build()
                       .Each((ref Health health) =>
                       {
                           if (!health.IsAlive)
                           {
                               GameEngine.DrawBox(modalCfg.GameOverX1, modalCfg.GameOverY1, modalCfg.GameOverX2, modalCfg.GameOverY2, modalCfg.GameOverBgColor, true);
                               GameEngine.DrawBox(modalCfg.GameOverX1, modalCfg.GameOverY1, modalCfg.GameOverX2, modalCfg.GameOverY2, modalCfg.GameOverOuterBorderColor, false);
                               GameEngine.DrawBox(modalCfg.GameOverX1 + 4, modalCfg.GameOverY1 + 4, modalCfg.GameOverX2 - 4, modalCfg.GameOverY2 - 4, modalCfg.GameOverInnerBorderColor, false);

                               GameEngine.DrawString(modalCfg.GameOverTitleX, modalCfg.GameOverTitleY, modalCfg.GameOverTitleText, modalCfg.GameOverTitleColor);
                               GameEngine.DrawString(modalCfg.GameOverRetryX, modalCfg.GameOverRetryY, modalCfg.GameOverRetryText, modalCfg.GameOverRetryColor);
                           }
                       });

                  world.QueryBuilder<Health>()
                       .With<BossTag>()
                       .Build()
                       .Each((ref Health health) =>
                       {
                           if (!health.IsAlive)
                           {
                               GameEngine.DrawBox(modalCfg.VictoryX1, modalCfg.VictoryY1, modalCfg.VictoryX2, modalCfg.VictoryY2, modalCfg.VictoryBgColor, true);
                               GameEngine.DrawBox(modalCfg.VictoryX1, modalCfg.VictoryY1, modalCfg.VictoryX2, modalCfg.VictoryY2, modalCfg.VictoryOuterBorderColor, false);
                               GameEngine.DrawBox(modalCfg.VictoryX1 + 4, modalCfg.VictoryY1 + 4, modalCfg.VictoryX2 - 4, modalCfg.VictoryY2 - 4, modalCfg.VictoryInnerBorderColor, false);

                               GameEngine.DrawString(modalCfg.VictoryTitleX, modalCfg.VictoryTitleY, modalCfg.VictoryTitleText, modalCfg.VictoryTitleColor);
                               GameEngine.DrawString(modalCfg.VictoryRetryX, modalCfg.VictoryRetryY, modalCfg.VictoryRetryText, modalCfg.VictoryRetryColor);
                           }
                       });
              });
    }
}
