using System;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// 弾、Boss（FSM 預警光環・無敵盾）、プレイヤー、プラットフォーム、および HUD・状態機バナーを
/// Flecs.NET ECS 常駐 Routine (System) として描画登録・管理する静的クラス。
/// 各種描画色は ConfigManager.Render (render.toml) から動的取得されます。
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

                 // 1. プラットフォーム・足場の描画
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
                 world.QueryBuilder<Transform, BossStats, BossStateMachine, Sprite>()
                      .With<BossTag>()
                      .Build()
                      .Each((ref Transform trans, ref BossStats stats, ref BossStateMachine fsm, ref Sprite sprite) =>
                      {
                          uint auraColor = stats.Phase switch
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

                          if (sprite.IsVisible && sprite.GraphHandle >= 0)
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
                 world.QueryBuilder<Transform, PlayerState, Sprite>()
                      .With<PlayerTag>()
                      .Build()
                      .Each((ref Transform trans, ref PlayerState playerState, ref Sprite sprite) =>
                      {
                          if (!playerState.IsAlive || !sprite.IsVisible || sprite.GraphHandle < 0)
                          {
                              return;
                          }

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

                          GameEngine.DrawCircle((int)trans.X, (int)trans.Y, (int)playerRenderCfg.HitboxCoreRadius, playerRenderCfg.HitboxCoreColor, true);
                          GameEngine.DrawCircle((int)trans.X, (int)trans.Y, (int)playerRenderCfg.HitboxBorderRadius, playerRenderCfg.HitboxBorderColor, false);
                      });

                 // 4. プレイヤー銃弾描画
                 world.QueryBuilder<Transform>()
                      .With<PlayerBulletTag>()
                      .Build()
                      .Each((ref Transform trans) =>
                      {
                          int px = (int)trans.X;
                          int py = (int)trans.Y;
                          GameEngine.DrawCircle(px, py, (int)bulletCfg.PlayerBulletOuterRadius, bulletCfg.PlayerBulletOuterColor, true);
                          GameEngine.DrawCircle(px, py, (int)bulletCfg.PlayerBulletInnerRadius, bulletCfg.PlayerBulletInnerColor, true);
                          GameEngine.DrawBox(
                              px - bulletCfg.PlayerBulletTrailHalfWidth,
                              py - bulletCfg.PlayerBulletTrailHalfHeight,
                              px + bulletCfg.PlayerBulletTrailHalfWidth,
                              py + bulletCfg.PlayerBulletTrailHalfHeight,
                              bulletCfg.PlayerBulletTrailColor,
                              true);
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
                 world.QueryBuilder<PlayerState>()
                      .With<PlayerTag>()
                      .Build()
                      .Each((ref PlayerState playerState) =>
                      {
                          string jumpSymbol = playerState.JumpCount switch
                          {
                              2 => "[ ●  ● ]",
                              1 => "[ ●  ○ ]",
                              _ => "[ ○  ○ ]"
                          };
                          uint jumpColor = playerState.JumpCount > 0 ? hudCfg.DoubleJumpActiveColor : hudCfg.DoubleJumpInactiveColor;
                          GameEngine.DrawString(hudCfg.TitleX, hudCfg.DoubleJumpY, $"DOUBLE JUMP: {jumpSymbol}", jumpColor);
                      });

                 // 3. 画面下部 Boss 大 HP バーおよび状態表示
                 world.QueryBuilder<BossStats, BossStateMachine>()
                      .With<BossTag>()
                      .Build()
                      .Each((ref BossStats stats, ref BossStateMachine fsm) =>
                      {
                          int barX1 = hpCfg.BarX1;
                          int barY1 = hpCfg.BarY1;
                          int barX2 = hpCfg.BarX2;
                          int barY2 = hpCfg.BarY2;
                          int totalWidth = barX2 - barX1;

                          // 3.1 Boss 情報ヘッダー (HP バー上部)
                          string bossTitle = $"BOSS: ANCIENT CAT OVERLORD [PHASE {stats.Phase}/3]";
                          string hpText = $"HP: {stats.CurrentHp:F0} / {stats.MaxHp:F0}";
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

                          uint stateBadgeColor = fsm.CurrentState switch
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
                          uint hpFillColor = stats.Phase switch
                          {
                              1 => renderCfg.BossEffects.Phase1AuraColor,
                              2 => renderCfg.BossEffects.Phase2AuraColor,
                              _ => renderCfg.BossEffects.Phase3AuraColor
                          };

                          float hpRatio = Math.Clamp(stats.CurrentHp / stats.MaxHp, 0.0f, 1.0f);
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
                 world.QueryBuilder<PlayerState>()
                      .With<PlayerTag>()
                      .Build()
                      .Each((ref PlayerState playerState) =>
                      {
                          if (!playerState.IsAlive)
                          {
                              GameEngine.DrawBox(modalCfg.GameOverX1, modalCfg.GameOverY1, modalCfg.GameOverX2, modalCfg.GameOverY2, modalCfg.GameOverBgColor, true);
                              GameEngine.DrawBox(modalCfg.GameOverX1, modalCfg.GameOverY1, modalCfg.GameOverX2, modalCfg.GameOverY2, modalCfg.GameOverOuterBorderColor, false);
                              GameEngine.DrawBox(modalCfg.GameOverX1 + 4, modalCfg.GameOverY1 + 4, modalCfg.GameOverX2 - 4, modalCfg.GameOverY2 - 4, modalCfg.GameOverInnerBorderColor, false);

                              GameEngine.DrawString(modalCfg.GameOverTitleX, modalCfg.GameOverTitleY, modalCfg.GameOverTitleText, modalCfg.GameOverTitleColor);
                              GameEngine.DrawString(modalCfg.GameOverRetryX, modalCfg.GameOverRetryY, modalCfg.GameOverRetryText, modalCfg.GameOverRetryColor);
                          }
                      });

                 world.QueryBuilder<BossStats>()
                      .With<BossTag>()
                      .Build()
                      .Each((ref BossStats stats) =>
                      {
                          if (stats.CurrentHp <= 0.0f)
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
