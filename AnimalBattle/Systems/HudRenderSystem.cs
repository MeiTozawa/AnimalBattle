using System;
using System.Drawing;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// 画面上部操作ヘルプ、自機 HP・二段ジャンプ残数、および画面下部 Boss 大 HP バー・行動状態バッジを描画する ECS System。
/// Boss 1 (Ancient Cat) と Boss 2 (Ground Crusher) の両方を自動検知して動的に描画を切り替えます。
/// </summary>
public static class HudRenderSystem
{
    /// <summary>
    /// HUD 描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World"/></returns>
    public static World AddHudRenderSystem(this World world)
    {
        Query<DoubleJump> doubleJumpQuery =
            world.QueryBuilder<DoubleJump>()
                 .With<PlayerTag>()
                 .Build();

        Query<Health> playerHealthQuery =
            world.QueryBuilder<Health>()
                 .With<PlayerTag>()
                 .Build();

        Query<Health, Boss1Stats, Boss1StateMachine> boss1StatusQuery =
            world.QueryBuilder<Health, Boss1Stats, Boss1StateMachine>()
                 .With<BossTag>()
                 .With<Boss1Tag>()
                 .Build();

        Query<Health, Boss2Stats, Boss2StateMachine> boss2StatusQuery =
            world.QueryBuilder<Health, Boss2Stats, Boss2StateMachine>()
                 .With<BossTag>()
                 .With<Boss2Tag>()
                 .Build();

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

                 int lineIndex = 1;
                 foreach (HudTextItem item in hudCfg.Text)
                 {
                     int drawY = hudCfg.TitleY + hudCfg.LineSpacing * lineIndex;

                     if (item.Text.Contains("F1", StringComparison.OrdinalIgnoreCase) ||
                         item.Text.Contains("Hitbox", StringComparison.OrdinalIgnoreCase))
                     {
                         string dbgStatus = dbgCfg.Enabled ? " [ON - H / F1 to toggle]" : " [OFF - H / F1 to toggle]";
                         Color statusColor = dbgCfg.Enabled ? dbgCfg.IndicatorColor : item.Color;
                         GameEngine.DrawString(hudCfg.TitleX, drawY, $"{item.Text}{dbgStatus}", statusColor);
                     }
                     else
                     {
                         GameEngine.DrawString(hudCfg.TitleX, drawY, item.Text, item.Color);
                     }

                     lineIndex++;
                 }

                 // Boss 選択ホットキー表示
                 int switchGuideY = hudCfg.TitleY + hudCfg.LineSpacing * lineIndex;
                 GameEngine.DrawString(hudCfg.TitleX, switchGuideY, "[1] Boss 1 (Aerial Cat)  [2] Boss 2 (Ground Crusher)", ColorPalette.GoldLight);

                 // 2. プレイヤージャンプ残数表示
                 doubleJumpQuery.Each((ref DoubleJump doubleJump) =>
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
                 playerHealthQuery.Each((ref Health health) =>
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

                 // 4.1 Boss 1 大 HP バーおよび状態表示
                 boss1StatusQuery.Each((ref Health health, ref Boss1Stats stats, ref Boss1StateMachine fsm) =>
                 {
                     string bossTitle = $"BOSS: {ConfigManager.Boss1.BossName} [PHASE {stats.Phase}/3]";
                     string stateName = fsm.CurrentState switch
                     {
                         Boss1StateKind.Hovering => "[STATE: HOVERING (IDLE)]",
                         Boss1StateKind.Telegraphing => $"[STATE: TELEGRAPHING (WIND-UP: {fsm.CurrentSkillName})]",
                         Boss1StateKind.Casting => $"[STATE: CASTING ({fsm.CurrentSkillName})]",
                         Boss1StateKind.Recovering => "[STATE: RECOVERING (VULNERABLE)]",
                         Boss1StateKind.PhaseTransition => "[STATE: PHASE TRANSITION (INVULNERABLE)]",
                         _ => "[STATE: UNKNOWN]"
                     };

                     Color stateBadgeColor = fsm.CurrentState switch
                     {
                         Boss1StateKind.Hovering => hpCfg.StateDefaultColor,
                         Boss1StateKind.Telegraphing => hpCfg.StateTelegraphingColor,
                         Boss1StateKind.Casting => hpCfg.StateCastingColor,
                         Boss1StateKind.Recovering => hpCfg.StateRecoveringColor,
                         Boss1StateKind.PhaseTransition => hpCfg.StatePhaseTransitionColor,
                         _ => hpCfg.StateDefaultColor
                     };

                     DrawBossHpBar(hpCfg, modalCfg, bossTitle, stateName, stateBadgeColor, health, stats.Phase, fsm.StateTimer, fsm.StateDuration);
                 });

                 // 4.2 Boss 2 大 HP バーおよび状態表示
                 boss2StatusQuery.Each((ref Health health, ref Boss2Stats stats, ref Boss2StateMachine fsm) =>
                 {
                     string bossTitle = $"BOSS: {ConfigManager.Boss2.BossName} [PHASE {stats.Phase}/3]";
                     string stateName = fsm.CurrentState switch
                     {
                         Boss2StateKind.PatrolChase => string.IsNullOrEmpty(fsm.CurrentSkillName)
                             ? "[STATE: PATROL & CHASE]"
                             : $"[STATE: PATROL & CHASE (NEXT: {fsm.CurrentSkillName})]",
                         Boss2StateKind.Telegraphing => $"[STATE: TELEGRAPHING (WIND-UP: {fsm.CurrentSkillName})]",
                         Boss2StateKind.Casting => $"[STATE: CASTING ({fsm.CurrentSkillName})]",
                         Boss2StateKind.LeapJumping => $"[STATE: LEAP SMASH (AIRBORNE)]",
                         Boss2StateKind.Recovering => string.IsNullOrEmpty(fsm.CurrentSkillName)
                             ? "[STATE: RECOVERING (VULNERABLE)]"
                             : $"[STATE: RECOVERING (NEXT: {fsm.CurrentSkillName})]",
                         Boss2StateKind.PhaseTransition => "[STATE: ROAR TRANSITION (INVULNERABLE)]",
                         _ => "[STATE: UNKNOWN]"
                     };

                     Color stateBadgeColor = fsm.CurrentState switch
                     {
                         Boss2StateKind.PatrolChase => hpCfg.StateDefaultColor,
                         Boss2StateKind.Telegraphing => hpCfg.StateTelegraphingColor,
                         Boss2StateKind.Casting => hpCfg.StateCastingColor,
                         Boss2StateKind.LeapJumping => ColorPalette.OrangeBright,
                         Boss2StateKind.Recovering => hpCfg.StateRecoveringColor,
                         Boss2StateKind.PhaseTransition => hpCfg.StatePhaseTransitionColor,
                         _ => hpCfg.StateDefaultColor
                     };

                     DrawBossHpBar(hpCfg, modalCfg, bossTitle, stateName, stateBadgeColor, health, stats.Phase, fsm.StateTimer, fsm.StateDuration);
                 });

                 // 5. プレイヤー死亡時 GAME OVER バナー
                 playerHealthQuery.Each((ref Health pHealth) =>
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

        return world;
    }

    private static void DrawBossHpBar(BossHpBarRenderConfig hpCfg, ModalRenderConfig modalCfg, string bossTitle, string stateName, Color stateBadgeColor, Health health, int phase, float stateTimer, float stateDuration)
    {
        int barX1 = hpCfg.BarX1;
        int barY1 = hpCfg.BarY1;
        int barX2 = hpCfg.BarX2;
        int barY2 = hpCfg.BarY2;
        int totalWidth = barX2 - barX1;

        // 4.1 Boss 情報ヘッダー (HP バー上部)
        string hpText = $"HP: {health.Current:F0} / {health.Max:F0}";
        GameEngine.DrawString(barX1, barY1 - hpCfg.HeaderOffsetY, bossTitle, hpCfg.TitleColor);
        GameEngine.DrawString(barX2 - 170, barY1 - hpCfg.HeaderOffsetY, hpText, hpCfg.HpTextColor);

        GameEngine.DrawString(barX1 + hpCfg.StateBadgeOffsetX, barY1 - hpCfg.HeaderOffsetY, $"{stateName} ({stateTimer:F1}s/{stateDuration:F1}s)", stateBadgeColor);

        // 4.2 Boss HP バー背景および枠線
        GameEngine.DrawBox(barX1 - hpCfg.BgPadding, barY1 - hpCfg.BgPadding, barX2 + hpCfg.BgPadding, barY2 + hpCfg.BgPadding, hpCfg.BgColor, true);
        GameEngine.DrawBox(barX1 - hpCfg.BgPadding, barY1 - hpCfg.BgPadding, barX2 + hpCfg.BgPadding, barY2 + hpCfg.BgPadding, hpCfg.BorderColor, false);

        // 4.3 Boss HP ゲージ塗りつぶし
        Color hpFillColor = phase switch
        {
            1 => hpCfg.Phase1Color,
            2 => hpCfg.Phase2Color,
            3 => hpCfg.Phase3Color,
            _ => hpCfg.Phase1Color
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
    }
}
