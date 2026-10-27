namespace AnimalBattle.Skills;

/// <summary>
/// 予兆演出設定から描画デリゲートを生成するビルダークラス。
/// </summary>
public static class SkillTelegraphBuilder
{
    /// <summary>
    /// 予兆設定に対応する描画処理デリゲートを生成します。
    /// </summary>
    public static Action<AbilityTelegraphContext>? Build(TelegraphConfig cfg)
    {
        if (cfg.Kind == TelegraphKind.None) return null;

        return cfg.Kind switch
        {
            TelegraphKind.AimLaser => ctx => TelegraphRenderHelper.DrawAimingLasers(
                ctx.SourcePos, ctx.TargetPos, cfg.SpreadAngle, Math.Max(1, cfg.Count), ctx.Progress, cfg.Color, ctx.Effects, cfg.IsSniper),

            TelegraphKind.RadialSpikes => ctx =>
            {
                var spinAngle = cfg.SpinSpeed != 0.0f ? ctx.StateTimer * cfg.SpinSpeed : 0.0f;
                TelegraphRenderHelper.DrawRadialThreatSpikes(
                    ctx.SourcePos, Math.Max(1, cfg.Count), spinAngle, cfg.Radius > 0.0f ? cfg.Radius : 75.0f, ctx.Progress, cfg.Color, ctx.Effects);
            },

            TelegraphKind.SkyfallColumns => ctx => TelegraphRenderHelper.DrawSkyfallColumns(
                cfg.MinX, cfg.MaxX, Math.Max(1, cfg.Count), cfg.Height > 0.0f ? cfg.Height : 650.0f, ctx.Progress, cfg.Color, cfg.WarningText, ctx.Effects),

            TelegraphKind.DiveTrajectory => ctx => TelegraphRenderHelper.DrawDiveRamTelegraph(
                ctx.SourcePos, ctx.TargetPos, cfg.Radius > 0.0f ? cfg.Radius : 40.0f, ctx.Progress, cfg.Color, ctx.Effects),

            TelegraphKind.MeleeDangerBox => ctx => TelegraphRenderHelper.DrawMeleeDangerBox(
                ctx.SourcePos, cfg.Size, ctx.FacingRight, ctx.Progress, cfg.Color, ctx.Effects),

            TelegraphKind.ChargeTrack => ctx => TelegraphRenderHelper.DrawChargeDangerTrack(
                ctx.SourcePos, cfg.Height > 0.0f ? cfg.Height : (ctx.SourceHeight > 0.0f ? ctx.SourceHeight : 80.0f), ctx.FacingRight, ctx.Progress, cfg.Color, ctx.Effects, ctx.Arena),

            TelegraphKind.GroundShockwave => ctx =>
            {
                var groundY = ctx.SourcePos.Y + (ctx.SourceHeight > 0.0f ? ctx.SourceHeight : 80.0f) / 2.0f;
                TelegraphRenderHelper.DrawGroundShockwaveRails(ctx.SourcePos, groundY, ctx.Progress, cfg.Color, ctx.Effects);
            },

            TelegraphKind.TargetLock => ctx => TelegraphRenderHelper.DrawLeapSmashTelegraph(
                ctx.TargetPos, ctx.Progress, cfg.Color, ctx.Effects),

            _ => null
        };
    }
}
