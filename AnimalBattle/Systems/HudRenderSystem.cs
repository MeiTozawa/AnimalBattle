using System;
using System.Drawing;
using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Boss2;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Characters;
using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// 画面上部操作ヘルプ、自機 HP・二段ジャンプ残数、および画面下部 Boss 大 HP バー・行動状態バッジを描画する HUD 描画システム。
/// 領域モデル（<see cref="Player"/> / <see cref="BossCharacter"/>）から直接状態を読み取って描画します。
/// </summary>
public static class HudRenderSystem
{
    /// <summary>
    /// HUD および各種ステータス UI を描画します。
    /// </summary>
    /// <param name="player">現在アクティブなプレイヤー自機。</param>
    /// <param name="boss">現在アクティブな Boss。</param>
    public static void Render(Player? player, BossCharacter? boss)
    {
        RenderConfig renderCfg = ConfigManager.Render;
        HudRenderConfig hudCfg = renderCfg.Hud;
        BossHpBarRenderConfig hpCfg = renderCfg.BossHpBar;
        ModalRenderConfig modalCfg = renderCfg.Modals;
        DebugRenderConfig dbgCfg = renderCfg.Debug;

        DrawControlHelp(hudCfg, dbgCfg);
        DrawPlayerStatus(player, hudCfg);
        DrawBossStatus(boss, hpCfg, modalCfg);
        DrawGameOverModal(player, modalCfg);
    }

    private static void DrawControlHelp(HudRenderConfig hudCfg, DebugRenderConfig dbgCfg)
    {
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

        int switchGuideY = hudCfg.TitleY + hudCfg.LineSpacing * lineIndex;
        GameEngine.DrawString(hudCfg.TitleX, switchGuideY, "[1] Boss 1 (Aerial Cat)  [2] Boss 2 (Ground Crusher)", ColorPalette.GoldLight);
    }

    private static void DrawPlayerStatus(Player? player, HudRenderConfig hudCfg)
    {
        if (player == null)
        {
            return;
        }

        string jumpSymbol = player.RemainingJumps switch
        {
            >= 2 => "[ ●  ● ]",
            1 => "[ ●  ○ ]",
            _ => "[ ○  ○ ]"
        };
        Color jumpColor = player.RemainingJumps > 0 ? hudCfg.DoubleJumpActiveColor : hudCfg.DoubleJumpInactiveColor;
        GameEngine.DrawString(hudCfg.TitleX, hudCfg.DoubleJumpY, $"DOUBLE JUMP: {jumpSymbol}", jumpColor);

        int currentHp = (int)player.CurrentHp;
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
        string invBadge = player.InvincibleTimer > 0.0f ? $" [INVINCIBLE {player.InvincibleTimer:F1}s]" : "";
        GameEngine.DrawString(hudCfg.TitleX, hudCfg.DoubleJumpY + 20, $"PLAYER HP:   {hpSymbol} ({currentHp}/{(int)player.MaxHp}){invBadge}", hpColor);
    }

    private static void DrawBossStatus(BossCharacter? boss, BossHpBarRenderConfig hpCfg, ModalRenderConfig modalCfg)
    {
        if (boss == null)
        {
            return;
        }

        string bossTitle = $"BOSS: {boss.BossName} [PHASE {ToLegacyPhase(boss.Phase)}/3]";
        GetBossStateInfo(boss.CurrentState, boss.CurrentSkill, hpCfg, out string stateName, out Color stateBadgeColor);
        DrawBossHpBar(hpCfg, modalCfg, bossTitle, stateName, stateBadgeColor, boss.CurrentHp, boss.MaxHp, ToLegacyPhase(boss.Phase), boss.StateTimer, boss.StateDuration);
    }

    private static void DrawGameOverModal(Player? player, ModalRenderConfig modalCfg)
    {
        if (player != null && !player.IsAlive)
        {
            GameEngine.DrawBox(modalCfg.GameOverX1, modalCfg.GameOverY1, modalCfg.GameOverX2, modalCfg.GameOverY2, modalCfg.GameOverBgColor, true);
            GameEngine.DrawBox(modalCfg.GameOverX1, modalCfg.GameOverY1, modalCfg.GameOverX2, modalCfg.GameOverY2, modalCfg.GameOverOuterBorderColor, false);
            GameEngine.DrawBox(modalCfg.GameOverX1 + 2, modalCfg.GameOverY1 + 2, modalCfg.GameOverX2 - 2, modalCfg.GameOverY2 - 2, modalCfg.GameOverInnerBorderColor, false);
            GameEngine.DrawString(modalCfg.GameOverTitleX, modalCfg.GameOverTitleY, modalCfg.GameOverTitleText, modalCfg.GameOverTitleColor);
            GameEngine.DrawString(modalCfg.GameOverRetryX, modalCfg.GameOverRetryY, modalCfg.GameOverRetryText, modalCfg.GameOverRetryColor);
        }
    }

    private static int ToLegacyPhase(Phase phase)
    {
        return phase switch
        {
            Phase.Phase2 => 2,
            Phase.Phase3 => 3,
            _ => 1
        };
    }

    private static void GetBossStateInfo(BossState state, BossSkill skill, BossHpBarRenderConfig hpCfg, out string stateName, out Color badgeColor)
    {
        stateName = state.BossType switch
        {
            BossType.Boss1 => state.AsBoss1() switch
            {
                Boss1State.Hover => "[STATE: HOVERING (IDLE)]",
                Boss1State.AttackSelection => "[STATE: ATTACK SELECTION]",
                Boss1State.Telegraph => $"[STATE: TELEGRAPHING (WIND-UP: {skill})]",
                Boss1State.Execute => $"[STATE: EXECUTING ({skill})]",
                Boss1State.Recovery => "[STATE: RECOVERING (VULNERABLE)]",
                Boss1State.PhaseTransition => "[STATE: PHASE TRANSITION (INVULNERABLE)]",
                Boss1State.Dead => "[STATE: DEFEATED]",
                _ => "[STATE: UNKNOWN]"
            },
            BossType.Boss2 => state.AsBoss2() switch
            {
                Boss2State.Idle => "[STATE: IDLE / ALERT]",
                Boss2State.PatrolChase => $"[STATE: CHASE (NEXT: {skill})]",
                Boss2State.MoveToTakeoff => "[STATE: NAVIGATING TO TAKEOFF]",
                Boss2State.PlatformTraverse => "[STATE: PLATFORM TRAVERSE (AIRBORNE)]",
                Boss2State.Telegraph => $"[STATE: TELEGRAPHING (WIND-UP: {skill})]",
                Boss2State.Execute => $"[STATE: EXECUTING ({skill})]",
                Boss2State.WallCrashStun => "[STATE: WALL CRASH STUNNED (VULNERABLE)]",
                Boss2State.Recovery => "[STATE: RECOVERING (VULNERABLE)]",
                Boss2State.PhaseTransition => "[STATE: PHASE TRANSITION (INVULNERABLE)]",
                Boss2State.Dead => "[STATE: DEFEATED]",
                _ => "[STATE: UNKNOWN]"
            },
            _ => "[STATE: UNKNOWN]"
        };

        if (state.IsDead)
        {
            badgeColor = ColorPalette.Gray;
        }
        else if (state.IsPhaseTransition)
        {
            badgeColor = hpCfg.StatePhaseTransitionColor;
        }
        else if (state.IsRecovery)
        {
            badgeColor = hpCfg.StateRecoveringColor;
        }
        else if (state.BossType == BossType.Boss2 && state.AsBoss2() == Boss2State.WallCrashStun)
        {
            badgeColor = ColorPalette.Gold;
        }
        else if (state.IsTelegraph)
        {
            badgeColor = hpCfg.StateTelegraphingColor;
        }
        else if (state.IsExecute)
        {
            badgeColor = hpCfg.StateCastingColor;
        }
        else
        {
            badgeColor = hpCfg.StateDefaultColor;
        }
    }

    private static void DrawBossHpBar(
        BossHpBarRenderConfig hpCfg,
        ModalRenderConfig modalCfg,
        string bossTitle,
        string stateName,
        Color stateBadgeColor,
        float currentHp,
        float maxHp,
        int phase,
        float stateTimer,
        float stateDuration)
    {
        int barX1 = hpCfg.BarX1;
        int barY1 = hpCfg.BarY1;
        int barX2 = hpCfg.BarX2;
        int barY2 = hpCfg.BarY2;
        int totalWidth = barX2 - barX1;

        // Boss 情報ヘッダー (HP バー上部)
        string hpText = $"HP: {currentHp:F0} / {maxHp:F0}";
        GameEngine.DrawString(barX1, barY1 - hpCfg.HeaderOffsetY, bossTitle, hpCfg.TitleColor);
        GameEngine.DrawString(barX2 - 170, barY1 - hpCfg.HeaderOffsetY, hpText, hpCfg.HpTextColor);

        GameEngine.DrawString(barX1 + hpCfg.StateBadgeOffsetX, barY1 - hpCfg.HeaderOffsetY, $"{stateName} ({stateTimer:F1}s/{stateDuration:F1}s)", stateBadgeColor);

        // Boss HP バー背景および枠線
        GameEngine.DrawBox(barX1 - hpCfg.BgPadding, barY1 - hpCfg.BgPadding, barX2 + hpCfg.BgPadding, barY2 + hpCfg.BgPadding, hpCfg.BgColor, true);
        GameEngine.DrawBox(barX1 - hpCfg.BgPadding, barY1 - hpCfg.BgPadding, barX2 + hpCfg.BgPadding, barY2 + hpCfg.BgPadding, hpCfg.BorderColor, false);

        // Boss HP ゲージ塗りつぶし
        Color hpFillColor = phase switch
        {
            1 => hpCfg.Phase1Color,
            2 => hpCfg.Phase2Color,
            3 => hpCfg.Phase3Color,
            _ => hpCfg.Phase1Color
        };

        float hpRatio = maxHp > 0.0f ? Math.Clamp(currentHp / maxHp, 0.0f, 1.0f) : 0.0f;
        int fillWidth = (int)(totalWidth * hpRatio);
        if (fillWidth > 0)
        {
            GameEngine.DrawBox(barX1, barY1, barX1 + fillWidth, barY2, hpFillColor, true);
            GameEngine.DrawBox(barX1, barY1, barX1 + fillWidth, barY1 + hpCfg.TopHighlightThickness, hpCfg.TopHighlightColor, true);
        }

        // Phase 分割マーカー (1/3, 2/3)
        int p1Split = barX1 + totalWidth / 3;
        int p2Split = barX1 + (totalWidth * 2) / 3;
        GameEngine.DrawBox(p1Split - 1, barY1, p1Split + 1, barY2, hpCfg.BorderColor, true);
        GameEngine.DrawBox(p2Split - 1, barY1, p2Split + 1, barY2, hpCfg.BorderColor, true);

        // 勝利バナー
        if (currentHp <= 0.0f)
        {
            GameEngine.DrawBox(modalCfg.VictoryX1, modalCfg.VictoryY1, modalCfg.VictoryX2, modalCfg.VictoryY2, modalCfg.VictoryBgColor, true);
            GameEngine.DrawBox(modalCfg.VictoryX1, modalCfg.VictoryY1, modalCfg.VictoryX2, modalCfg.VictoryY2, modalCfg.VictoryOuterBorderColor, false);
            GameEngine.DrawBox(modalCfg.VictoryX1 + 2, modalCfg.VictoryY1 + 2, modalCfg.VictoryX2 - 2, modalCfg.VictoryY2 - 2, modalCfg.VictoryInnerBorderColor, false);
            GameEngine.DrawString(modalCfg.VictoryTitleX, modalCfg.VictoryTitleY, modalCfg.VictoryTitleText, modalCfg.VictoryTitleColor);
            GameEngine.DrawString(modalCfg.VictoryRetryX, modalCfg.VictoryRetryY, modalCfg.VictoryRetryText, modalCfg.VictoryRetryColor);
        }
    }
}
