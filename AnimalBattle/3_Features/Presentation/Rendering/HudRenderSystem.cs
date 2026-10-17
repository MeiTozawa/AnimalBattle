using System.Diagnostics;
using System.Drawing;
using AnimalBattle.Abilities;
using AnimalBattle.Ai;
using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Boss2;
using AnimalBattle.Characters;
using AnimalBattle.Characters.Boss;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using Flecs.NET.Core;

namespace AnimalBattle.Systems.Render;

/// <summary>
///     自機ステータス（HP、二段ジャンプ残数）、Boss 大 HP バー、および操作ガイドを描画する ECS システム。
///     戦闘状態（GameState.Battle）時にスクリーン空間へ直接描画します。
/// </summary>
public static class HudRenderSystem {
    /// <summary>
    ///     HUD 描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddHudRenderSystem(this World world)
    {
        var playerQuery = world.QueryBuilder<PlayerPawn>().Build();
        var bossQuery = world.QueryBuilder<BaseBossAgent>().Build();

        world.System("HudRenderSystem")
            .Kind(Ecs.OnStore)
            .Iter((Iter it) =>
            {
                var w = it.World();
                ref readonly var flow = ref w.Get<GameFlow>();
                if (flow.State != GameState.Battle) return;

                PlayerPawn? player = null;
                playerQuery.Each((ref PlayerPawn p) => player = p);

                BaseBossAgent? boss = null;
                bossQuery.Each((ref BaseBossAgent b) => boss = b);

                var remainingJumps = player?.RemainingJumps ?? 0;

                Render(player, boss, remainingJumps);
            });

        return world;
    }

    /// <summary>
    ///     HUD および各種ステータス UI をスクリーン空間に描画します。
    /// </summary>
    /// <param name="player">現在アクティブな自機 Pawn。</param>
    /// <param name="boss">現在アクティブな首領 Agent。</param>
    /// <param name="remainingJumps">プレイヤーの二段ジャンプ残数。</param>
    private static void Render(Pawn? player, BaseBossAgent? boss, int remainingJumps = 0)
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

        if (!string.IsNullOrEmpty(hudCfg.CharacterSwitchGuideText))
        {
            lineIndex++;
            var charGuideY = hudCfg.TitleY + hudCfg.LineSpacing * lineIndex;
            GameEngine.DrawString(hudCfg.TitleX, charGuideY, hudCfg.CharacterSwitchGuideText, hudCfg.CharacterSwitchGuideColor);
        }
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

        var maxHp = (int)Math.Max(1.0f, player.MaxHp);
        var currentHp = Math.Clamp((int)player.CurrentHp, 0, maxHp);

        var sb = new System.Text.StringBuilder("[ ");
        for (var i = 0; i < maxHp; i++)
        {
            sb.Append(i < currentHp ? "●  " : "○  ");
        }
        sb.Append(']');
        var symbols = sb.ToString();

        var hpRatio = (float)currentHp / maxHp;
        var hpColor = hpRatio switch {
            var r when r >= hudCfg.PlayerHpHighThreshold => hudCfg.PlayerHpHighColor,
            var r when r >= hudCfg.PlayerHpMidThreshold => hudCfg.PlayerHpMidColor,
            > 0.0f => hudCfg.PlayerHpLowColor,
            _ => hudCfg.PlayerHpDeadColor
        };

        var charName = player is PlayerPawn pp ? pp.CurrentCharacter.ToString() : "Cat";
        var hpText = player.IsInvincible
            ? $"PLAYER [{charName}] HP: {symbols} ({currentHp}/{maxHp}) [INVINCIBLE]"
            : $"PLAYER [{charName}] HP: {symbols} ({currentHp}/{maxHp})";
        GameEngine.DrawString(hudCfg.PlayerHpX, hudCfg.PlayerHpY, hpText, hpColor);
    }

    private static void DrawBossStatus(BaseBossAgent? boss, BossHpBarRenderConfig hpCfg, ModalRenderConfig modalCfg)
    {
        if (boss == null) return;

        var phase = ToLegacyPhase(boss.Phase);
        var bossTitle = GetBossTitle(boss.BossType, phase);
        GetBossStateInfo(boss, hpCfg, out var stateName, out var stateBadgeColor);
        string timerText;
        if (boss.AbilitySystem.IsCasting)
        {
            var pElapsed = boss.AbilitySystem.PhaseElapsed;
            var pDuration = boss.AbilitySystem.PhaseDuration;
            timerText = pDuration > 0.0f
                ? $"{pElapsed:F1}s/{pDuration:F1}s"
                : $"{pElapsed:F1}s";
        }
        else
        {
            timerText = $"{boss.StateTimer:F1}s";
        }

        DrawBossHpBar(hpCfg, modalCfg, bossTitle, stateName, stateBadgeColor, boss.CurrentHp, boss.MaxHp, phase,
            timerText, boss.PhaseThresholds);
    }

    /// <summary>
    ///     Boss 称号および Phase を取得します（模式匹配で魔数配列を排除し 0-GC 描画）。
    /// </summary>
    private static string GetBossTitle(BossType bossType, int phase)
    {
        var p = Math.Clamp(phase, 1, 3);
        return (bossType, p) switch {
            (BossType.Boss1, 1) => "BOSS: Boss1 [PHASE 1/3]",
            (BossType.Boss1, 2) => "BOSS: Boss1 [PHASE 2/3]",
            (BossType.Boss1, 3) => "BOSS: Boss1 [PHASE 3/3]",
            (BossType.Boss2, 1) => "BOSS: Boss2 [PHASE 1/3]",
            (BossType.Boss2, 2) => "BOSS: Boss2 [PHASE 2/3]",
            (BossType.Boss2, 3) => "BOSS: Boss2 [PHASE 3/3]",
            _ => $"BOSS: {bossType} [PHASE {p}/3]"
        };
    }

    private static void DrawGameOverModal(Pawn? player, ModalRenderConfig modalCfg)
    {
        if (player is not { IsAlive: false }) return;
        GameEngine.DrawBox(modalCfg.GameOverX1, modalCfg.GameOverY1, modalCfg.GameOverX2, modalCfg.GameOverY2,
            modalCfg.GameOverBgColor, true);
        GameEngine.DrawBox(modalCfg.GameOverX1, modalCfg.GameOverY1, modalCfg.GameOverX2, modalCfg.GameOverY2,
            modalCfg.GameOverOuterBorderColor, false);
        GameEngine.DrawBox(modalCfg.GameOverX1 + modalCfg.InnerBorderPadding, modalCfg.GameOverY1 + modalCfg.InnerBorderPadding, modalCfg.GameOverX2 - modalCfg.InnerBorderPadding,
            modalCfg.GameOverY2 - modalCfg.InnerBorderPadding, modalCfg.GameOverInnerBorderColor, false);
        GameEngine.DrawString(modalCfg.GameOverTitleX, modalCfg.GameOverTitleY, modalCfg.GameOverTitleText,
            modalCfg.GameOverTitleColor);
        GameEngine.DrawString(modalCfg.GameOverRetryX, modalCfg.GameOverRetryY, modalCfg.GameOverRetryText,
            modalCfg.GameOverRetryColor);
    }

    private static int ToLegacyPhase(Phase phase) => phase switch {
        Phase.Phase1 => 1,
        Phase.Phase2 => 2,
        Phase.Phase3 => 3,
        _ => AssertHelper.FailWithDefault("[HUD 警告] 未知のフェーズ", 1)
    };

    private static void GetBossStateInfo(BaseBossAgent boss, BossHpBarRenderConfig hpCfg,
        out string stateName, out Color badgeColor)
    {
        var state = boss.CurrentState;
        var currentAbility = boss.AbilitySystem.CurrentAbility;
        var abilityName = currentAbility?.DisplayName;
        if (string.IsNullOrEmpty(abilityName))
        {
            abilityName = !boss.CurrentSkillTag.IsEmpty ? boss.CurrentSkillTag.ToString() : "None";
        }

        (stateName, badgeColor) = state switch {
            { IsDead: true } => ("[STATE: DEFEATED]", ColorPalette.Gray),
            { IsPhaseTransition: true } => ("[STATE: PHASE TRANSITION (INVINCIBLE)]", hpCfg.StatePhaseTransitionColor),
            { IsRecovery: true } => ("[STATE: RECOVERING]", hpCfg.StateRecoveringColor),
            _ when state.BossType == BossType.Boss2 && state.AsBoss2() == Boss2State.WallCrashStun =>
                ("[STATE: STUNNED (VULNERABLE)]", hpCfg.StateStunnedColor),
            { IsTelegraph: true } => ($"[STATE: TELEGRAPHING (WIND-UP: {abilityName})]", hpCfg.StateTelegraphingColor),
            { IsExecute: true } => ($"[STATE: EXECUTING ({abilityName})]", hpCfg.StateCastingColor),
            _ => state.BossType switch {
                BossType.Boss1 => state.AsBoss1() switch {
                    Boss1State.Hover => ("[STATE: HOVERING (IDLE)]", hpCfg.StateDefaultColor),
                    Boss1State.AttackSelection => ("[STATE: ATTACK SELECTION]", hpCfg.StateDefaultColor),
                    _ => (AssertHelper.FailWithDefault("[HUD 警告] 未知のステート", "[STATE: UNKNOWN]"), hpCfg.StateDefaultColor)
                },
                BossType.Boss2 => state.AsBoss2() switch {
                    Boss2State.Idle => ("[STATE: IDLE / ALERT]", hpCfg.StateDefaultColor),
                    Boss2State.PatrolChase => ($"[STATE: CHASE (NEXT: {abilityName})]", hpCfg.StateDefaultColor),
                    Boss2State.MoveToTakeoff => ("[STATE: NAVIGATING TO TAKEOFF]", hpCfg.StateDefaultColor),
                    Boss2State.PlatformTraverse => ("[STATE: PLATFORM TRAVERSE (AIRBORNE)]", hpCfg.StateDefaultColor),
                    _ => (AssertHelper.FailWithDefault("[HUD 警告] 未知のステート", "[STATE: UNKNOWN]"), hpCfg.StateDefaultColor)
                },
                _ => (AssertHelper.FailWithDefault("[HUD 警告] 未知のステート", "[STATE: UNKNOWN]"), hpCfg.StateDefaultColor)
            }
        };
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
        string timerText,
        (float Phase2Threshold, float Phase3Threshold) phaseThresholds)
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

        var badgeX = barX1 + hpCfg.StateBadgeOffsetX;
        var badgeY = barY1 - (hpCfg.StateBadgeOffsetY != 0 ? hpCfg.StateBadgeOffsetY : hpCfg.HeaderOffsetY);
        GameEngine.DrawString(badgeX, badgeY, $"{stateName} ({timerText})", stateBadgeColor);

        // Boss HP バー背景および枠線
        GameEngine.DrawBox(barX1 - hpCfg.BgPadding, barY1 - hpCfg.BgPadding, barX2 + hpCfg.BgPadding,
            barY2 + hpCfg.BgPadding, hpCfg.BgColor, true);
        GameEngine.DrawBox(barX1 - hpCfg.BgPadding, barY1 - hpCfg.BgPadding, barX2 + hpCfg.BgPadding,
            barY2 + hpCfg.BgPadding, hpCfg.BorderColor, false);

        // Boss HP ゲージ塗りつぶし
        Color hpFillColor;
        switch (phase)
        {
            case 1:
                hpFillColor = hpCfg.Phase1Color;
            break;
            case 2:
                hpFillColor = hpCfg.Phase2Color;
            break;
            case 3:
                hpFillColor = hpCfg.Phase3Color;
            break;
            default:
                Debug.Fail("[HUD 警告] 未知のフェーズ");
                hpFillColor = hpCfg.Phase1Color;
            break;
        }

        var hpRatio = maxHp > 0.0f ? Math.Clamp(currentHp / maxHp, 0.0f, 1.0f) : 0.0f;
        var fillWidth = (int)(totalWidth * hpRatio);
        if (fillWidth > 0)
        {
            GameEngine.DrawBox(barX1, barY1, barX1 + fillWidth, barY2, hpFillColor, true);
            GameEngine.DrawBox(barX1, barY1, barX1 + fillWidth, barY1 + hpCfg.TopHighlightThickness,
                hpCfg.TopHighlightColor, true);
        }

        // Phase 分割マーカー（設定ファイル駆動の HpThreshold に基づく動的描画）
        var p2Ratio = Math.Clamp(phaseThresholds.Phase2Threshold, 0.0f, 1.0f);
        var p3Ratio = Math.Clamp(phaseThresholds.Phase3Threshold, 0.0f, 1.0f);

        var phase2MarkerX = barX1 + (int)(totalWidth * p2Ratio);
        var phase3MarkerX = barX1 + (int)(totalWidth * p3Ratio);

        GameEngine.DrawBox(phase2MarkerX - hpCfg.PhaseSplitWidth, barY1, phase2MarkerX + hpCfg.PhaseSplitWidth, barY2, hpCfg.BorderColor, true);
        GameEngine.DrawBox(phase3MarkerX - hpCfg.PhaseSplitWidth, barY1, phase3MarkerX + hpCfg.PhaseSplitWidth, barY2, hpCfg.BorderColor, true);

        // 勝利バナー
        if (currentHp > 0.0f) return;
        GameEngine.DrawBox(modalCfg.VictoryX1, modalCfg.VictoryY1, modalCfg.VictoryX2, modalCfg.VictoryY2,
            modalCfg.VictoryBgColor, true);
        GameEngine.DrawBox(modalCfg.VictoryX1, modalCfg.VictoryY1, modalCfg.VictoryX2, modalCfg.VictoryY2,
            modalCfg.VictoryOuterBorderColor, false);
        GameEngine.DrawBox(modalCfg.VictoryX1 + modalCfg.InnerBorderPadding, modalCfg.VictoryY1 + modalCfg.InnerBorderPadding, modalCfg.VictoryX2 - modalCfg.InnerBorderPadding,
            modalCfg.VictoryY2 - modalCfg.InnerBorderPadding, modalCfg.VictoryInnerBorderColor, false);
        GameEngine.DrawString(modalCfg.VictoryTitleX, modalCfg.VictoryTitleY, modalCfg.VictoryTitleText,
            modalCfg.VictoryTitleColor);
        GameEngine.DrawString(modalCfg.VictoryRetryX, modalCfg.VictoryRetryY, modalCfg.VictoryRetryText,
            modalCfg.VictoryRetryColor);
    }
}
