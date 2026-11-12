using System.Drawing;
using AnimalBattle.Ai;

namespace AnimalBattle.Presentation.Rendering;

/// <summary>
///     Boss ステータスおよび HP バーの描画を担当するレンダラー。
/// </summary>
internal static class BossHudRenderer
{
    public static void DrawBossStatus(in HudRenderSystemContext.BossHudData? boss, BossHpBarRenderConfig hpCfg, ModalRenderConfig modalCfg)
    {
        if (boss == null) return;
        var b = boss.Value;

        var phase = ToLegacyPhase(b.CombatState.Phase);
        var bossTitle = GetBossTitle(b.CombatState.BossType, phase);
        var bossHp = b.Health;
        DrawBossHpBar(hpCfg, modalCfg, bossTitle, bossHp.Current, bossHp.Max, phase, b.PhaseThresholds);
    }

    /// <summary>
    ///     Boss の称号およびフェーズ表記を取得します：パターンマッチにより固定文字列を返し 0-GC で描画。
    /// </summary>
    public static string GetBossTitle(BossType bossType, int phase)
    {
        var p = Math.Clamp(phase, 1, 3);
        return (bossType, p) switch {
            (BossType.BossSnake, 1) => "BOSS: BossSnake [PHASE 1/3]",
            (BossType.BossSnake, 2) => "BOSS: BossSnake [PHASE 2/3]",
            (BossType.BossSnake, 3) => "BOSS: BossSnake [PHASE 3/3]",
            (BossType.BossLion, 1) => "BOSS: BossLion [PHASE 1/3]",
            (BossType.BossLion, 2) => "BOSS: BossLion [PHASE 2/3]",
            (BossType.BossLion, 3) => "BOSS: BossLion [PHASE 3/3]",
            (BossType.Empty, _) => "TRAINING DUMMY",
            _ => $"BOSS: {bossType} [PHASE {p}/3]"
        };
    }

    public static int ToLegacyPhase(Phase phase) => phase switch {
        Phase.Phase1 => 1,
        Phase.Phase2 => 2,
        Phase.Phase3 => 3,
        _ => AssertHelper.FailWithDefault("[HUD 警告] 未知のフェーズ", 1)
    };

    public static void DrawBossHpBar(
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

        // ボス情報ヘッダー：HPバー上部
        Span<char> hpBuf = stackalloc char[64];
        hpBuf.TryWrite($"HP: {currentHp:F0} / {maxHp:F0}", out var hpCharsWritten);

        var titlePos = barP1 + (hpCfg.TitleOffset != Vec2i.Zero
            ? hpCfg.TitleOffset
            : new Vec2i(0, -hpCfg.HeaderOffsetY));
        GameEngine.DrawLeftString(titlePos, bossTitle, hpCfg.TitleColor);

        var hpY = barP1.Y - (hpCfg.HpTextOffsetY != 0 ? hpCfg.HpTextOffsetY : hpCfg.HeaderOffsetY);
        GameEngine.DrawRightString(new Vec2i(barP2.X, hpY), hpBuf[..hpCharsWritten], hpCfg.HpTextColor);

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

        // Phase 分割マーカー：設定ファイル駆動の HpThreshold に基づく動的描画
        var p2Ratio = Math.Clamp(phaseThresholds.Phase2Threshold, 0.0f, 1.0f);
        var p3Ratio = Math.Clamp(phaseThresholds.Phase3Threshold, 0.0f, 1.0f);

        var phase2MarkerX = barP1.X + (int)(totalWidth * p2Ratio);
        var phase3MarkerX = barP1.X + (int)(totalWidth * p3Ratio);

        GameEngine.DrawBox(new Vec2i(phase2MarkerX - hpCfg.PhaseSplitWidth, barP1.Y), new Vec2i(phase2MarkerX + hpCfg.PhaseSplitWidth, barP2.Y), hpCfg.BorderColor, true);
        GameEngine.DrawBox(new Vec2i(phase3MarkerX - hpCfg.PhaseSplitWidth, barP1.Y), new Vec2i(phase3MarkerX + hpCfg.PhaseSplitWidth, barP2.Y), hpCfg.BorderColor, true);

        // 勝利バナー
        if (currentHp > 0.0f) return;
        BattleModalRenderer.DrawVictoryModal(modalCfg);
    }
}
