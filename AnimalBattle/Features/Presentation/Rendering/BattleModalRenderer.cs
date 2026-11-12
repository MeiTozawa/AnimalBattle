namespace AnimalBattle.Presentation.Rendering;

/// <summary>
///     ゲームオーバー・勝利モーダルおよびトレーニングモードバナーの描画を担当するレンダラー。
/// </summary>
internal static class BattleModalRenderer
{
    /// <summary>
    ///     トレーニングモード（ボス不在時）の簡易案内バナーを描画します。
    /// </summary>
    public static void DrawTrainingModeBanner(HudRenderConfig hudCfg)
    {
        if (!string.IsNullOrEmpty(hudCfg.TrainingModeTitleText))
        {
            GameEngine.DrawLeftString(hudCfg.TrainingModeTitlePos, hudCfg.TrainingModeTitleText, hudCfg.TrainingModeTitleColor);
        }
        if (!string.IsNullOrEmpty(hudCfg.TrainingModeGuideText))
        {
            GameEngine.DrawLeftString(hudCfg.TrainingModeGuidePos, hudCfg.TrainingModeGuideText, hudCfg.TrainingModeGuideColor);
        }
    }

    /// <summary>
    ///     自機死亡時のゲームオーバーモーダルを描画します。
    /// </summary>
    public static void DrawGameOverModal(in HudRenderSystemContext.PlayerHudData? player, ModalRenderConfig modalCfg)
    {
        if (player == null || player.Value.IsAlive) return;
        GameEngine.DrawBox(modalCfg.GameOverPos1, modalCfg.GameOverPos2, modalCfg.GameOverBgColor, true);
        GameEngine.DrawBox(modalCfg.GameOverPos1, modalCfg.GameOverPos2, modalCfg.GameOverOuterBorderColor, false);
        var pad = new Vec2i(modalCfg.InnerBorderPadding, modalCfg.InnerBorderPadding);
        GameEngine.DrawBox(modalCfg.GameOverPos1 + pad, modalCfg.GameOverPos2 - pad, modalCfg.GameOverInnerBorderColor, false);
        var modalCenterX = (modalCfg.GameOverPos1.X + modalCfg.GameOverPos2.X) / 2;
        GameEngine.DrawCenterString(new Vec2i(modalCenterX, modalCfg.GameOverTitlePos.Y), modalCfg.GameOverTitleText, modalCfg.GameOverTitleColor);
        GameEngine.DrawCenterString(new Vec2i(modalCenterX, modalCfg.GameOverRetryPos.Y), modalCfg.GameOverRetryText, modalCfg.GameOverRetryColor);
    }

    /// <summary>
    ///     Boss 撃破時の勝利モーダルを描画します。
    /// </summary>
    public static void DrawVictoryModal(ModalRenderConfig modalCfg)
    {
        GameEngine.DrawBox(modalCfg.VictoryPos1, modalCfg.VictoryPos2, modalCfg.VictoryBgColor, true);
        GameEngine.DrawBox(modalCfg.VictoryPos1, modalCfg.VictoryPos2, modalCfg.VictoryOuterBorderColor, false);
        var vicPad = new Vec2i(modalCfg.InnerBorderPadding, modalCfg.InnerBorderPadding);
        GameEngine.DrawBox(modalCfg.VictoryPos1 + vicPad, modalCfg.VictoryPos2 - vicPad, modalCfg.VictoryInnerBorderColor, false);
        var vicCenterX = (modalCfg.VictoryPos1.X + modalCfg.VictoryPos2.X) / 2;
        GameEngine.DrawCenterString(new Vec2i(vicCenterX, modalCfg.VictoryTitlePos.Y), modalCfg.VictoryTitleText, modalCfg.VictoryTitleColor);
        GameEngine.DrawCenterString(new Vec2i(vicCenterX, modalCfg.VictoryRetryPos.Y), modalCfg.VictoryRetryText, modalCfg.VictoryRetryColor);
    }
}
