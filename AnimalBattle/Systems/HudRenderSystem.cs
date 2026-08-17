using System;
using System.Drawing;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// 画面上部操作ヘルプ、自機 HP・二段ジャンプ残数、および画面下部 Boss 大 HP バー・行動状態バッジを描画する ECS System。
/// 全ての座標・マージン・テキスト・色パラメータは外部 TOML 設定ファイルから読み出され、未定義状態の診断警告を備えています。
/// </summary>
public static class HudRenderSystem
{
    /// <summary>
    /// HUD 描画システムを Flecs.NET ワールドへ登録します。
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

                 // 1. 操作説明テキスト
                 GameEngine.DrawString(hudCfg.TitleX, hudCfg.TitleY, hudCfg.TitleText, hudCfg.TitleColor);
                 GameEngine.DrawString(hudCfg.TitleX, hudCfg.TitleY + hudCfg.LineSpacing * 1, hudCfg.MoveHelpText, hudCfg.HelpTextColor);
                 GameEngine.DrawString(hudCfg.TitleX, hudCfg.TitleY + hudCfg.LineSpacing * 2, hudCfg.JumpHelpText, hudCfg.HelpTextColor);
                 GameEngine.DrawString(hudCfg.TitleX, hudCfg.TitleY + hudCfg.LineSpacing * 3, hudCfg.ShootHelpText, hudCfg.HelpTextColor);
                 GameEngine.DrawString(hudCfg.TitleX, hudCfg.TitleY + hudCfg.LineSpacing * 4, hudCfg.RestartHelpText, hudCfg.HelpTextColor);

                 string dbgStatus = dbgCfg.Enabled ? " [ON - H / F1 to toggle]" : " [OFF - H / F1 to toggle]";
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
                              >= 2 => "[ ●  ● ]",
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
                          int currentHp = (int)health.Current;
                          string hpSymbol = currentHp switch
                          {
                              >= 3 => "[ ●  ●  ● ]",
                              2 => "[ ●  ●  ○ ]",
                              1 => "[ ●  ○  ○ ]",
                              _ => "[ ○  ○  ○ ]"
                          };
                          Color hpColor = currentHp switch
                          {
                              >= 3 => ColorPalette.Green,
                              2 => ColorPalette.Gold,
                              1 => ColorPalette.Red,
                              _ => ColorPalette.Gray
                          };
                          string invBadge = health.IsInvincible ? $" [INVINCIBLE {health.InvincibleTimer:F1}s]" : "";
                          GameEngine.DrawString(hudCfg.TitleX, hudCfg.DoubleJumpY + 20, $"PLAYER HP:   {hpSymbol} ({currentHp}/{(int)health.Max}){invBadge}", hpColor);
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
                              _ => GetFallbackStateName(fsm.CurrentState)
                          };

                          Color stateBadgeColor = fsm.CurrentState switch
                          {
                              BossStateKind.Hovering => hpCfg.StateDefaultColor,
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
                              3 => renderCfg.BossEffects.Phase3AuraColor,
                              _ => GetFallbackPhaseColor(stats.Phase, renderCfg.BossEffects.Phase1AuraColor)
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

                          // 4.5 勝利バナー
                          if (health.Current <= 0.0f)
                          {
                              GameEngine.DrawBox(modalCfg.VictoryX1, modalCfg.VictoryY1, modalCfg.VictoryX2, modalCfg.VictoryY2, modalCfg.VictoryBgColor, true);
                              GameEngine.DrawBox(modalCfg.VictoryX1, modalCfg.VictoryY1, modalCfg.VictoryX2, modalCfg.VictoryY2, modalCfg.VictoryOuterBorderColor, false);
                              GameEngine.DrawBox(modalCfg.VictoryX1 + 2, modalCfg.VictoryY1 + 2, modalCfg.VictoryX2 - 2, modalCfg.VictoryY2 - 2, modalCfg.VictoryInnerBorderColor, false);
                              GameEngine.DrawString(modalCfg.VictoryTitleX, modalCfg.VictoryTitleY, modalCfg.VictoryTitleText, modalCfg.VictoryTitleColor);
                              GameEngine.DrawString(modalCfg.VictoryRetryX, modalCfg.VictoryRetryY, modalCfg.VictoryRetryText, modalCfg.VictoryRetryColor);
                          }
                      });

                 // 5. プレイヤー死亡時 GAME OVER バナー
                 world.QueryBuilder<Health>()
                      .With<PlayerTag>()
                      .Build()
                      .Each((ref Health pHealth) =>
                      {
                          if (pHealth.Current <= 0.0f)
                          {
                              GameEngine.DrawBox(modalCfg.GameOverX1, modalCfg.GameOverY1, modalCfg.GameOverX2, modalCfg.GameOverY2, modalCfg.GameOverBgColor, true);
                              GameEngine.DrawBox(modalCfg.GameOverX1, modalCfg.GameOverY1, modalCfg.GameOverX2, modalCfg.GameOverY2, modalCfg.GameOverOuterBorderColor, false);
                              GameEngine.DrawBox(modalCfg.GameOverX1 + 2, modalCfg.GameOverY1 + 2, modalCfg.GameOverX2 - 2, modalCfg.GameOverY2 - 2, modalCfg.GameOverInnerBorderColor, false);
                              GameEngine.DrawString(modalCfg.GameOverTitleX, modalCfg.GameOverTitleY, modalCfg.GameOverTitleText, modalCfg.GameOverTitleColor);
                              GameEngine.DrawString(modalCfg.GameOverRetryX, modalCfg.GameOverRetryY, modalCfg.GameOverRetryText, modalCfg.GameOverRetryColor);
                          }
                      });
             });
    }

    private static string GetFallbackStateName(BossStateKind state)
    {
        Console.WriteLine($"[HUD 警告] 未知の BossStateKind です: {state}");
        return "[STATE: UNKNOWN]";
    }

    private static Color GetFallbackPhaseColor(int phase, Color defaultColor)
    {
        Console.WriteLine($"[HUD 警告] 未知の Boss Phase 番号です: {phase} (有効値: 1, 2, 3)");
        return defaultColor;
    }
}
