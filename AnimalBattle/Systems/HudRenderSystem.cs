using System;
using System.Drawing;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// 左上 HUD 操作案内、二段ジャンプ表示、プレイヤー HP ハート表示、画面下部 Boss HP バー（各フェーズ分割線・状態バッジ）、
/// および Game Over / Victory モーダルダイアログを描画する ECS 描画 System。
/// </summary>
public static class HudRenderSystem
{
    /// <summary>
    /// HUD および UI 描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    public static void Register(World world)
    {
        world.System("HudRenderSystem")
             .Kind(Ecs.OnStore)
             .Run((Iter it) =>
             {
                 RenderConfig renderCfg = ConfigManager.Render;
                 HudRenderConfig hudCfg = renderCfg.Hud;
                 BossHpBarRenderConfig hpCfg = renderCfg.BossHpBar;
                 ModalRenderConfig modalCfg = renderCfg.Modals;
                 DebugRenderConfig dbgCfg = renderCfg.Debug;

                 // 1. 操作説明とタイトル
                 GameEngine.DrawString(hudCfg.TitleX, hudCfg.TitleY, hudCfg.TitleText, hudCfg.TitleColor);
                 GameEngine.DrawString(hudCfg.TitleX, hudCfg.TitleY + hudCfg.LineSpacing, hudCfg.MoveHelpText, hudCfg.HelpTextColor);
                 GameEngine.DrawString(hudCfg.TitleX, hudCfg.TitleY + hudCfg.LineSpacing * 2, hudCfg.JumpHelpText, hudCfg.HelpTextColor);
                 GameEngine.DrawString(hudCfg.TitleX, hudCfg.TitleY + hudCfg.LineSpacing * 3, hudCfg.ShootHelpText, hudCfg.HelpTextColor);
                 GameEngine.DrawString(hudCfg.TitleX, hudCfg.TitleY + hudCfg.LineSpacing * 4, hudCfg.RestartHelpText, hudCfg.RestartHelpColor);

                 string dbgStatus = dbgCfg.Enabled ? " [HITBOX: ON]" : "";
                 Color dbgColor = dbgCfg.Enabled ? dbgCfg.IndicatorColor : hudCfg.HelpTextColor;
                 GameEngine.DrawString(hudCfg.TitleX, hudCfg.TitleY + hudCfg.LineSpacing * 5, $"{hudCfg.DebugHelpText}{dbgStatus}", dbgColor);

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
                              3 => ColorPalette.Green,
                              2 => ColorPalette.Gold,
                              1 => ColorPalette.Red,
                              _ => ColorPalette.Gray
                          };
                          string invBadge = health.IsInvincible ? $" [INVINCIBLE {health.InvincibleTimer:F1}s]" : "";
                          GameEngine.DrawString(hudCfg.TitleX, hudCfg.DoubleJumpY + 20, $"PLAYER HP:   {hpSymbol} ({(int)health.Current}/{(int)health.Max}){invBadge}", hpColor);
                      });

                 // 4. 画面下部 Boss 大 HP バーおよび状態表示
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

                          // 4.1 Boss 情報ヘッダー (HP バー上部)
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

                          // 4.2 Boss HP バー背景および枠線
                          GameEngine.DrawBox(barX1 - hpCfg.BgPadding, barY1 - hpCfg.BgPadding, barX2 + hpCfg.BgPadding, barY2 + hpCfg.BgPadding, hpCfg.BgColor, true);
                          GameEngine.DrawBox(barX1 - hpCfg.BgPadding, barY1 - hpCfg.BgPadding, barX2 + hpCfg.BgPadding, barY2 + hpCfg.BgPadding, hpCfg.BorderColor, false);

                          // 4.3 Boss HP ゲージ塗りつぶし
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

                          // 4.4 Phase 分割マーカー (1/3, 2/3)
                          int p1Split = barX1 + totalWidth / 3;
                          int p2Split = barX1 + (totalWidth * 2) / 3;
                          GameEngine.DrawBox(p1Split - 1, barY1, p1Split + 1, barY2, hpCfg.BorderColor, true);
                          GameEngine.DrawBox(p2Split - 1, barY1, p2Split + 1, barY2, hpCfg.BorderColor, true);
                      });

                  // 5. ゲームオーバーおよび勝利モーダル
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
