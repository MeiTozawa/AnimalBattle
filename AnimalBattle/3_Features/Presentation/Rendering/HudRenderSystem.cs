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

                ref readonly var renderCfg = ref w.Get<RenderConfig>();

                Render(player, boss, in renderCfg);
            });

        return world;
    }

    /// <summary>
    ///     HUD および各種ステータス UI をスクリーン空間に描画します。
    /// </summary>
    /// <param name="player">現在アクティブな自機 Pawn。</param>
    /// <param name="boss">現在アクティブな首領 Agent。</param>
    /// <param name="renderCfg">描画設定。</param>
    private static void Render(Pawn? player, BaseBossAgent? boss, in RenderConfig renderCfg)
    {
        var hudCfg = renderCfg.Hud;
        var hpCfg = renderCfg.BossHpBar;
        var modalCfg = renderCfg.Modals;

        DrawControlHelp(hudCfg);
        DrawPlayerStatus(player, hudCfg);
        DrawBossStatus(boss, hpCfg, modalCfg);
        DrawGameOverModal(player, modalCfg);
    }

    private static void DrawControlHelp(HudRenderConfig hudCfg)
    {
        GameEngine.DrawString(hudCfg.TitlePos, hudCfg.TitleText, hudCfg.TitleColor);

        var lineIndex = 1;
        foreach (var item in hudCfg.Text)
        {
            var drawPos = new Vec2i(hudCfg.TitlePos.X, hudCfg.TitlePos.Y + hudCfg.LineSpacing * lineIndex);
            GameEngine.DrawString(drawPos, item.Text, item.Color);
            lineIndex++;
        }
    }

    private static void DrawPlayerStatus(Pawn? player, HudRenderConfig hudCfg)
    {
        if (player == null) return;

        ref readonly var playerHp = ref player.Health;
        var maxHp = (int)Math.Max(1.0f, playerHp.Max);
        var currentHp = Math.Clamp((int)playerHp.Current, 0, maxHp);

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
        GameEngine.DrawString(hudCfg.PlayerHpPos, hpText, hpColor);
    }

    private static void DrawBossStatus(BaseBossAgent? boss, BossHpBarRenderConfig hpCfg, ModalRenderConfig modalCfg)
    {
        if (boss == null) return;

        var phase = ToLegacyPhase(boss.Phase);
        var bossTitle = GetBossTitle(boss.BossType, phase);
        ref readonly var bossHp = ref boss.Health;
        DrawBossHpBar(hpCfg, modalCfg, bossTitle, bossHp.Current, bossHp.Max, phase, boss.PhaseThresholds);
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
        GameEngine.DrawBox(modalCfg.GameOverPos1, modalCfg.GameOverPos2, modalCfg.GameOverBgColor, true);
        GameEngine.DrawBox(modalCfg.GameOverPos1, modalCfg.GameOverPos2, modalCfg.GameOverOuterBorderColor, false);
        var pad = new Vec2i(modalCfg.InnerBorderPadding, modalCfg.InnerBorderPadding);
        GameEngine.DrawBox(modalCfg.GameOverPos1 + pad, modalCfg.GameOverPos2 - pad, modalCfg.GameOverInnerBorderColor, false);
        GameEngine.DrawString(modalCfg.GameOverTitlePos, modalCfg.GameOverTitleText, modalCfg.GameOverTitleColor);
        GameEngine.DrawString(modalCfg.GameOverRetryPos, modalCfg.GameOverRetryText, modalCfg.GameOverRetryColor);
    }

    private static int ToLegacyPhase(Phase phase) => phase switch {
        Phase.Phase1 => 1,
        Phase.Phase2 => 2,
        Phase.Phase3 => 3,
        _ => AssertHelper.FailWithDefault("[HUD 警告] 未知のフェーズ", 1)
    };

    internal static void GetBossStateInfo(BaseBossAgent boss, BossHpBarRenderConfig hpCfg,
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
        float currentHp,
        float maxHp,
        int phase,
        (float Phase2Threshold, float Phase3Threshold) phaseThresholds)
    {
        var barP1 = hpCfg.BarPos1;
        var barP2 = hpCfg.BarPos2;
        var totalWidth = barP2.X - barP1.X;

        // Boss 情報ヘッダー (HP バー上部)
        var hpText = $"HP: {currentHp:F0} / {maxHp:F0}";

        var titlePos = barP1 + (hpCfg.TitleOffset != Vec2i.Zero
            ? hpCfg.TitleOffset
            : new Vec2i(0, -hpCfg.HeaderOffsetY));
        GameEngine.DrawString(titlePos, bossTitle, hpCfg.TitleColor);

        var hpX = barP2.X - (hpCfg.HpTextOffsetRight != 0 ? hpCfg.HpTextOffsetRight : 170);
        var hpY = barP1.Y - (hpCfg.HpTextOffsetY != 0 ? hpCfg.HpTextOffsetY : hpCfg.HeaderOffsetY);
        GameEngine.DrawString(new Vec2i(hpX, hpY), hpText, hpCfg.HpTextColor);

        // Boss HP バー背景および枠線
        var bgPad = new Vec2i(hpCfg.BgPadding, hpCfg.BgPadding);
        GameEngine.DrawBox(barP1 - bgPad, barP2 + bgPad, hpCfg.BgColor, true);
        GameEngine.DrawBox(barP1 - bgPad, barP2 + bgPad, hpCfg.BorderColor, false);

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
            GameEngine.DrawBox(barP1, new Vec2i(barP1.X + fillWidth, barP2.Y), hpFillColor, true);
            GameEngine.DrawBox(barP1, new Vec2i(barP1.X + fillWidth, barP1.Y + hpCfg.TopHighlightThickness),
                hpCfg.TopHighlightColor, true);
        }

        // Phase 分割マーカー（設定ファイル駆動の HpThreshold に基づく動的描画）
        var p2Ratio = Math.Clamp(phaseThresholds.Phase2Threshold, 0.0f, 1.0f);
        var p3Ratio = Math.Clamp(phaseThresholds.Phase3Threshold, 0.0f, 1.0f);

        var phase2MarkerX = barP1.X + (int)(totalWidth * p2Ratio);
        var phase3MarkerX = barP1.X + (int)(totalWidth * p3Ratio);

        GameEngine.DrawBox(new Vec2i(phase2MarkerX - hpCfg.PhaseSplitWidth, barP1.Y), new Vec2i(phase2MarkerX + hpCfg.PhaseSplitWidth, barP2.Y), hpCfg.BorderColor, true);
        GameEngine.DrawBox(new Vec2i(phase3MarkerX - hpCfg.PhaseSplitWidth, barP1.Y), new Vec2i(phase3MarkerX + hpCfg.PhaseSplitWidth, barP2.Y), hpCfg.BorderColor, true);

        // 勝利バナー
        if (currentHp > 0.0f) return;
        GameEngine.DrawBox(modalCfg.VictoryPos1, modalCfg.VictoryPos2, modalCfg.VictoryBgColor, true);
        GameEngine.DrawBox(modalCfg.VictoryPos1, modalCfg.VictoryPos2, modalCfg.VictoryOuterBorderColor, false);
        var vicPad = new Vec2i(modalCfg.InnerBorderPadding, modalCfg.InnerBorderPadding);
        GameEngine.DrawBox(modalCfg.VictoryPos1 + vicPad, modalCfg.VictoryPos2 - vicPad, modalCfg.VictoryInnerBorderColor, false);
        GameEngine.DrawString(modalCfg.VictoryTitlePos, modalCfg.VictoryTitleText, modalCfg.VictoryTitleColor);
        GameEngine.DrawString(modalCfg.VictoryRetryPos, modalCfg.VictoryRetryText, modalCfg.VictoryRetryColor);
    }
}
