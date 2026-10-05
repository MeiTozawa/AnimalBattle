using System.Drawing;
using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Boss2;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Characters;
using AnimalBattle.Characters.Boss;
using AnimalBattle.Configs;

namespace AnimalBattle.Systems;

/// <summary>
///     画面上部操作ヘルプ、自機 HP・二段ジャンプ残数、および画面下部 Boss 大 HP バー・行動状態バッジを描画する HUD 描画システム。
///     領域モデル（<see cref="Player" /> / <see cref="BossCharacter" />）から直接状態を読み取って描画します。
/// </summary>
public static class HudRenderSystem {
    private static readonly string[] BossTitles = [
        "BOSS: Unknown [PHASE 1/3]",
        "BOSS: Ancient Cat Overlord [PHASE 1/3]",
        "BOSS: Ancient Cat Overlord [PHASE 2/3]",
        "BOSS: Ancient Cat Overlord [PHASE 3/3]",
        "BOSS: Ground Crusher [PHASE 1/3]",
        "BOSS: Ground Crusher [PHASE 2/3]",
        "BOSS: Ground Crusher [PHASE 3/3]"
    ];

    private static readonly string[] PlayerHpSymbols = [
        "[ ○  ○  ○ ]",
        "[ ●  ○  ○ ]",
        "[ ●  ●  ○ ]",
        "[ ●  ●  ● ]"
    ];

    private static readonly string[] PlayerHpLut = [
        "HP: [ ○  ○  ○ ] (0/3)",
        "HP: [ ●  ○  ○ ] (1/3)",
        "HP: [ ●  ●  ○ ] (2/3)",
        "HP: [ ●  ●  ● ] (3/3)"
    ];

    /// <summary>
    ///     HUD および各種ステータス UI を描画します。
    /// </summary>
    /// <param name="player">現在アクティブな自機 Pawn。</param>
    /// <param name="boss">現在アクティブな首領 Agent。</param>
    /// <param name="remainingJumps">プレイヤーの二段ジャンプ残数。</param>
    public static void Render(Pawn? player, BaseBossAgent? boss, int remainingJumps = 0)
    {
        var renderCfg = ConfigManager.Render;
        var hudCfg = renderCfg.Hud;
        var hpCfg = renderCfg.BossHpBar;
        var modalCfg = renderCfg.Modals;
        var dbgCfg = renderCfg.Debug;

        DrawControlHelp(hudCfg, dbgCfg);
        DrawPlayerStatus(player, remainingJumps, hudCfg);
        DrawBossStatus(boss, hpCfg, modalCfg);
        DrawGameOverModal(player, modalCfg);
    }

    private static void DrawControlHelp(HudRenderConfig hudCfg, DebugRenderConfig dbgCfg)
    {
        GameEngine.DrawString(hudCfg.TitleX, hudCfg.TitleY, hudCfg.TitleText, hudCfg.TitleColor);

        var lineIndex = 1;
        foreach (var item in hudCfg.Text)
        {
            var drawY = hudCfg.TitleY + hudCfg.LineSpacing * lineIndex;

            if (item.Text.Contains("F1", StringComparison.OrdinalIgnoreCase) ||
                item.Text.Contains("Hitbox", StringComparison.OrdinalIgnoreCase))
            {
                var dbgStatus = dbgCfg.Enabled ? " [ON - H / F1 to toggle]" : " [OFF - H / F1 to toggle]";
                var statusColor = dbgCfg.Enabled ? dbgCfg.IndicatorColor : item.Color;
                GameEngine.DrawString(hudCfg.TitleX, drawY, $"{item.Text}{dbgStatus}", statusColor);
            }
            else
            {
                GameEngine.DrawString(hudCfg.TitleX, drawY, item.Text, item.Color);
            }

            lineIndex++;
        }

        var switchGuideY = hudCfg.TitleY + hudCfg.LineSpacing * lineIndex;
        GameEngine.DrawString(hudCfg.TitleX, switchGuideY, hudCfg.BossSwitchGuideText, hudCfg.BossSwitchGuideColor);
    }

    private static void DrawPlayerStatus(Pawn? player, int remainingJumps, HudRenderConfig hudCfg)
    {
        if (player == null) return;

        var jumpText = remainingJumps switch {
            >= 2 => "Double Jump: [ ●  ● ]",
            1 => "Double Jump: [ ●  ○ ]",
            _ => "Double Jump: [ ○  ○ ]"
        };
        var jumpColor = remainingJumps > 0 ? hudCfg.DoubleJumpActiveColor : hudCfg.DoubleJumpInactiveColor;
        GameEngine.DrawString(hudCfg.DoubleJumpX, hudCfg.DoubleJumpY, jumpText, jumpColor);

        var currentHp = Math.Clamp((int)player.CurrentHp, 0, 3);
        var hpColor = currentHp switch {
            >= 3 => hudCfg.PlayerHpHighColor,
            2 => hudCfg.PlayerHpMidColor,
            1 => hudCfg.PlayerHpLowColor,
            _ => hudCfg.PlayerHpDeadColor
        };
        var hpText = player.IsInvincible
            ? $"HP: {PlayerHpSymbols[currentHp]} ({currentHp}/3) [INVINCIBLE]"
            : PlayerHpLut[currentHp];
        GameEngine.DrawString(hudCfg.PlayerHpX, hudCfg.PlayerHpY, hpText, hpColor);
    }

    private static void DrawBossStatus(BaseBossAgent? boss, BossHpBarRenderConfig hpCfg, ModalRenderConfig modalCfg)
    {
        if (boss == null) return;

        var phase = ToLegacyPhase(boss.Phase);
        var bossTitle = GetBossTitle(boss.BossType, phase);
        GetBossStateInfo(boss.CurrentState, boss.CurrentSkill, hpCfg, out var stateName, out var stateBadgeColor);
        DrawBossHpBar(hpCfg, modalCfg, bossTitle, stateName, stateBadgeColor, boss.CurrentHp, boss.MaxHp, phase,
            boss.StateTimer, boss.StateDuration);
    }

    private static string GetBossTitle(BossType bossType, int phase)
    {
        var p = Math.Clamp(phase, 1, 3);
        if (bossType == BossType.Boss1) return BossTitles[p];
        if (bossType == BossType.Boss2) return BossTitles[3 + p];
        return BossTitles[0];
    }

    private static void DrawGameOverModal(Pawn? player, ModalRenderConfig modalCfg)
    {
        if (player != null && !player.IsAlive)
        {
            GameEngine.DrawBox(modalCfg.GameOverX1, modalCfg.GameOverY1, modalCfg.GameOverX2, modalCfg.GameOverY2,
                modalCfg.GameOverBgColor, true);
            GameEngine.DrawBox(modalCfg.GameOverX1, modalCfg.GameOverY1, modalCfg.GameOverX2, modalCfg.GameOverY2,
                modalCfg.GameOverOuterBorderColor, false);
            GameEngine.DrawBox(modalCfg.GameOverX1 + 2, modalCfg.GameOverY1 + 2, modalCfg.GameOverX2 - 2,
                modalCfg.GameOverY2 - 2, modalCfg.GameOverInnerBorderColor, false);
            GameEngine.DrawString(modalCfg.GameOverTitleX, modalCfg.GameOverTitleY, modalCfg.GameOverTitleText,
                modalCfg.GameOverTitleColor);
            GameEngine.DrawString(modalCfg.GameOverRetryX, modalCfg.GameOverRetryY, modalCfg.GameOverRetryText,
                modalCfg.GameOverRetryColor);
        }
    }

    private static int ToLegacyPhase(Phase phase)
    {
        return phase switch {
            Phase.Phase2 => 2,
            Phase.Phase3 => 3,
            _ => 1
        };
    }

    private static void GetBossStateInfo(BossState state, BossSkill skill, BossHpBarRenderConfig hpCfg,
        out string stateName, out Color badgeColor)
    {
        stateName = state.BossType switch {
            BossType.Boss1 => state.AsBoss1() switch {
                Boss1State.Hover => "[STATE: HOVERING (IDLE)]",
                Boss1State.AttackSelection => "[STATE: ATTACK SELECTION]",
                Boss1State.Telegraph => EnumStringLut.GetBoss1TelegraphBadge(skill),
                Boss1State.Execute => EnumStringLut.GetBoss1ExecuteBadge(skill),
                Boss1State.Recovery => "[STATE: RECOVERING (VULNERABLE)]",
                Boss1State.PhaseTransition => "[STATE: PHASE TRANSITION (INVULNERABLE)]",
                Boss1State.Dead => "[STATE: DEFEATED]",
                _ => "[STATE: UNKNOWN]"
            },
            BossType.Boss2 => state.AsBoss2() switch {
                Boss2State.Idle => "[STATE: IDLE / ALERT]",
                Boss2State.PatrolChase => EnumStringLut.GetBoss2ChaseBadge(skill),
                Boss2State.MoveToTakeoff => "[STATE: NAVIGATING TO TAKEOFF]",
                Boss2State.PlatformTraverse => "[STATE: PLATFORM TRAVERSE (AIRBORNE)]",
                Boss2State.Telegraph => EnumStringLut.GetBoss2TelegraphBadge(skill),
                Boss2State.Execute => EnumStringLut.GetBoss2ExecuteBadge(skill),
                Boss2State.WallCrashStun => "[STATE: WALL CRASH STUNNED (VULNERABLE)]",
                Boss2State.Recovery => "[STATE: RECOVERING (VULNERABLE)]",
                Boss2State.PhaseTransition => "[STATE: PHASE TRANSITION (INVULNERABLE)]",
                Boss2State.Dead => "[STATE: DEFEATED]",
                _ => "[STATE: UNKNOWN]"
            },
            _ => "[STATE: UNKNOWN]"
        };

        if (state.IsDead)
            badgeColor = ColorPalette.Gray;
        else if (state.IsPhaseTransition)
            badgeColor = hpCfg.StatePhaseTransitionColor;
        else if (state.IsRecovery)
            badgeColor = hpCfg.StateRecoveringColor;
        else if (state.BossType == BossType.Boss2 && state.AsBoss2() == Boss2State.WallCrashStun)
            badgeColor = ColorPalette.Gold;
        else if (state.IsTelegraph)
            badgeColor = hpCfg.StateTelegraphingColor;
        else if (state.IsExecute)
            badgeColor = hpCfg.StateCastingColor;
        else
            badgeColor = hpCfg.StateDefaultColor;
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
        var barX1 = hpCfg.BarX1;
        var barY1 = hpCfg.BarY1;
        var barX2 = hpCfg.BarX2;
        var barY2 = hpCfg.BarY2;
        var totalWidth = barX2 - barX1;

        // Boss 情報ヘッダー (HP バー上部)
        var hpText = $"HP: {currentHp:F0} / {maxHp:F0}";

        var titleX = barX1 + hpCfg.TitleOffsetX;
        var titleY = barY1 - (hpCfg.TitleOffsetY != 0 ? hpCfg.TitleOffsetY : hpCfg.HeaderOffsetY);
        GameEngine.DrawString(titleX, titleY, bossTitle, hpCfg.TitleColor);

        var hpX = barX2 - (hpCfg.HpTextOffsetRight != 0 ? hpCfg.HpTextOffsetRight : 170);
        var hpY = barY1 - (hpCfg.HpTextOffsetY != 0 ? hpCfg.HpTextOffsetY : hpCfg.HeaderOffsetY);
        GameEngine.DrawString(hpX, hpY, hpText, hpCfg.HpTextColor);

        var timerText = stateDuration > 0.0f
            ? $"{stateTimer:F1}s/{stateDuration:F1}s"
            : $"{stateTimer:F1}s";
        var badgeX = barX1 + hpCfg.StateBadgeOffsetX;
        var badgeY = barY1 - (hpCfg.StateBadgeOffsetY != 0 ? hpCfg.StateBadgeOffsetY : hpCfg.HeaderOffsetY);
        GameEngine.DrawString(badgeX, badgeY, $"{stateName} ({timerText})", stateBadgeColor);

        // Boss HP バー背景および枠線
        GameEngine.DrawBox(barX1 - hpCfg.BgPadding, barY1 - hpCfg.BgPadding, barX2 + hpCfg.BgPadding,
            barY2 + hpCfg.BgPadding, hpCfg.BgColor, true);
        GameEngine.DrawBox(barX1 - hpCfg.BgPadding, barY1 - hpCfg.BgPadding, barX2 + hpCfg.BgPadding,
            barY2 + hpCfg.BgPadding, hpCfg.BorderColor, false);

        // Boss HP ゲージ塗りつぶし
        var hpFillColor = phase switch {
            1 => hpCfg.Phase1Color,
            2 => hpCfg.Phase2Color,
            3 => hpCfg.Phase3Color,
            _ => hpCfg.Phase1Color
        };

        var hpRatio = maxHp > 0.0f ? Math.Clamp(currentHp / maxHp, 0.0f, 1.0f) : 0.0f;
        var fillWidth = (int)(totalWidth * hpRatio);
        if (fillWidth > 0)
        {
            GameEngine.DrawBox(barX1, barY1, barX1 + fillWidth, barY2, hpFillColor, true);
            GameEngine.DrawBox(barX1, barY1, barX1 + fillWidth, barY1 + hpCfg.TopHighlightThickness,
                hpCfg.TopHighlightColor, true);
        }

        // Phase 分割マーカー (1/3, 2/3)
        var p1Split = barX1 + totalWidth / 3;
        var p2Split = barX1 + totalWidth * 2 / 3;
        GameEngine.DrawBox(p1Split - 1, barY1, p1Split + 1, barY2, hpCfg.BorderColor, true);
        GameEngine.DrawBox(p2Split - 1, barY1, p2Split + 1, barY2, hpCfg.BorderColor, true);

        // 勝利バナー
        if (currentHp <= 0.0f)
        {
            GameEngine.DrawBox(modalCfg.VictoryX1, modalCfg.VictoryY1, modalCfg.VictoryX2, modalCfg.VictoryY2,
                modalCfg.VictoryBgColor, true);
            GameEngine.DrawBox(modalCfg.VictoryX1, modalCfg.VictoryY1, modalCfg.VictoryX2, modalCfg.VictoryY2,
                modalCfg.VictoryOuterBorderColor, false);
            GameEngine.DrawBox(modalCfg.VictoryX1 + 2, modalCfg.VictoryY1 + 2, modalCfg.VictoryX2 - 2,
                modalCfg.VictoryY2 - 2, modalCfg.VictoryInnerBorderColor, false);
            GameEngine.DrawString(modalCfg.VictoryTitleX, modalCfg.VictoryTitleY, modalCfg.VictoryTitleText,
                modalCfg.VictoryTitleColor);
            GameEngine.DrawString(modalCfg.VictoryRetryX, modalCfg.VictoryRetryY, modalCfg.VictoryRetryText,
                modalCfg.VictoryRetryColor);
        }
    }
}