using System;
using System.Drawing;
using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// Boss の攻撃予兆（Telegraph / Indicator）描画を一括管理する静的ヘルパークラス。
/// 従来の円環を全廃し、現代の弾幕およびアクションゲームに準拠した高視認性・直感的な危険予兆を提供します。
/// </summary>
public static class TelegraphRenderHelper
{
    /// <summary>
    /// 自機狙い・スナイパー用の照準レーザー線および照準クロスヘアを描画します。
    /// </summary>
    /// <param name="startX">Boss 基準 X 座標</param>
    /// <param name="startY">Boss 基準 Y 座標</param>
    /// <param name="targetX">プレイヤー目標 X 座標</param>
    /// <param name="targetY">プレイヤー目標 Y 座標</param>
    /// <param name="spreadAngle">拡散角度（ラジアン）</param>
    /// <param name="laserCount">照射レーザー本数</param>
    /// <param name="progress">前隙進行度 (0.0 ～ 1.0)</param>
    /// <param name="color">レーザーカラー</param>
    /// <param name="isPrecisionSniper">高精度スナイパー演出（加算発光・長距離ロック）を行うか</param>
    public static void DrawAimingLasers(
        float startX,
        float startY,
        float targetX,
        float targetY,
        float spreadAngle,
        int laserCount,
        float progress,
        Color color,
        bool isPrecisionSniper = false)
    {
        BossEffectsRenderConfig bossFx = ConfigManager.Render.BossEffects;
        float clampedProgress = Math.Clamp(progress, 0.0f, 1.0f);
        int alpha = (int)(bossFx.LaserAlpha * (0.4f + 0.6f * clampedProgress));
        int thickness = isPrecisionSniper ? Math.Max(2, bossFx.LaserThickness + (clampedProgress > 0.8f ? 1 : 0)) : bossFx.LaserThickness;
        float maxDistance = 1400.0f;

        GameEngine.SetDrawBlendMode(BlendMode.Alpha, alpha);

        int midIndex = laserCount / 2;
        for (int i = 0; i < laserCount; i++)
        {
            float offset = (i - midIndex) * spreadAngle;
            TelegraphGeometry.CalculateAimLaserEndpoint(startX, startY, targetX, targetY, offset, maxDistance, out float endX, out float endY);
            GameEngine.DrawLine((int)startX, (int)startY, (int)endX, (int)endY, color, thickness);
        }

        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        // ターゲット地点の照準クロスヘア [+] の描画
        int tx = (int)targetX;
        int ty = (int)targetY;
        int crosshairSize = (int)(12.0f - clampedProgress * 4.0f);
        GameEngine.DrawLine(tx - crosshairSize, ty, tx + crosshairSize, ty, color, 1);
        GameEngine.DrawLine(tx, ty - crosshairSize, tx, ty + crosshairSize, color, 1);

        // スナイパー発射寸前の閃光（加算ブレンド）
        if (isPrecisionSniper && clampedProgress > 0.75f)
        {
            int flashAlpha = (int)((clampedProgress - 0.75f) / 0.25f * 220.0f);
            GameEngine.SetDrawBlendMode(BlendMode.Add, flashAlpha);
            GameEngine.DrawLine((int)startX, (int)startY, tx, ty, ColorPalette.White, thickness + 1);
            GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
        }
    }

    /// <summary>
    /// 近接斬撃・重劈用の危険判定矩形ゾーンおよびチャージ進行ゲージを描画します。
    /// </summary>
    /// <param name="bx">Boss 中心 X 座標</param>
    /// <param name="by">Boss 中心 Y 座標</param>
    /// <param name="rangeX">斬撃有効 X 半径</param>
    /// <param name="rangeY">斬撃有効 Y 半径</param>
    /// <param name="facingRight">Boss が右を向いているか</param>
    /// <param name="progress">前隙進行度 (0.0 ～ 1.0)</param>
    /// <param name="color">危険ゾーンカラー</param>
    public static void DrawMeleeDangerBox(
        float bx,
        float by,
        float rangeX,
        float rangeY,
        bool facingRight,
        float progress,
        Color color)
    {
        BossEffectsRenderConfig bossFx = ConfigManager.Render.BossEffects;
        float clampedProgress = Math.Clamp(progress, 0.0f, 1.0f);
        TelegraphGeometry.CalculateMeleeDangerRect(bx, by, rangeX, rangeY, facingRight, out int x1, out int y1, out int x2, out int y2);

        // 半透明危険ゾーン背景
        GameEngine.SetDrawBlendMode(BlendMode.Alpha, bossFx.DangerBoxAlpha);
        GameEngine.DrawBox(x1, y1, x2, y2, color, true);

        // チャージ進行度に応じた内部充填バー（Boss 側から外側へ向けて充填）
        if (facingRight)
        {
            int fillX2 = (int)(x1 + (x2 - x1) * clampedProgress);
            GameEngine.DrawBox(x1, y1, fillX2, y2, ColorPalette.White, true);
        }
        else
        {
            int fillX1 = (int)(x2 - (x2 - x1) * clampedProgress);
            GameEngine.DrawBox(fillX1, y1, x2, y2, ColorPalette.White, true);
        }

        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        // 外枠線
        GameEngine.DrawBox(x1, y1, x2, y2, color, false);

        // 斬撃方向案内テキスト
        int textX = facingRight ? x2 - 24 : x1 + 8;
        int textY = y1 + (y2 - y1) / 2 - 6;
        string arrow = facingRight ? "►►" : "◄◄";
        GameEngine.DrawString(textX, textY, arrow, bossFx.WarningTextColor);
    }

    /// <summary>
    /// 横狂暴突進用の全画面水平危険軌道レーンを描画します。
    /// </summary>
    /// <param name="bx">Boss 中心 X 座標</param>
    /// <param name="by">Boss 中心 Y 座標</param>
    /// <param name="height">突進衝突高さ</param>
    /// <param name="facingRight">突進方向（右向きか）</param>
    /// <param name="progress">前隙進行度 (0.0 ～ 1.0)</param>
    /// <param name="color">危険軌道カラー</param>
    public static void DrawChargeDangerTrack(
        float bx,
        float by,
        float height,
        bool facingRight,
        float progress,
        Color color)
    {
        BossEffectsRenderConfig bossFx = ConfigManager.Render.BossEffects;
        float clampedProgress = Math.Clamp(progress, 0.0f, 1.0f);
        ArenaConfig arenaCfg = ConfigManager.Arena;

        TelegraphGeometry.CalculateChargeTrackRect(bx, by, height, facingRight, arenaCfg.BoundaryMinX, arenaCfg.BoundaryMaxX, out int x1, out int y1, out int x2, out int y2);

        // 半透明突進帯

        GameEngine.SetDrawBlendMode(BlendMode.Alpha, bossFx.ChargeTrackAlpha);
        GameEngine.DrawBox(x1, y1, x2, y2, color, true);
        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        // 上下レール境界線
        GameEngine.DrawLine(x1, y1, x2, y1, color, 1);
        GameEngine.DrawLine(x1, y2, x2, y2, color, 1);

        // 進行方向シェブロン矢印の連続描画
        string arrow = facingRight ? "►►►►►►►►►►►►►►►►" : "◄◄◄◄◄◄◄◄◄◄◄◄◄◄◄◄";
        int textY = y1 + (y2 - y1) / 2 - 6;
        GameEngine.DrawString(x1 + 30, textY, arrow, bossFx.WarningTextColor);

        // Boss 自身の突進チャージ脈動（加算ブレンド）
        int pulseAlpha = (int)(80.0f + 140.0f * clampedProgress);
        GameEngine.SetDrawBlendMode(BlendMode.Add, pulseAlpha);
        GameEngine.DrawBox((int)bx - 20, y1, (int)bx + 20, y2, ColorPalette.White, true);
        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
    }

    /// <summary>
    /// 俯衝突進・メテオダイブ（DiveRam）用の突進軌道線および着地危険領域を描画します。
    /// </summary>
    /// <param name="bx">Boss 中心 X 座標</param>
    /// <param name="by">Boss 中心 Y 座標</param>
    /// <param name="targetX">着地目標 X 座標</param>
    /// <param name="targetY">着地目標 Y 座標</param>
    /// <param name="impactRadius">着地衝撃波半径</param>
    /// <param name="progress">前隙進行度 (0.0 ～ 1.0)</param>
    /// <param name="color">予兆カラー</param>
    public static void DrawDiveRamTelegraph(
        float bx,
        float by,
        float targetX,
        float targetY,
        float impactRadius,
        float progress,
        Color color)
    {
        BossEffectsRenderConfig bossFx = ConfigManager.Render.BossEffects;
        PlayerConfig playerCfg = ConfigManager.Player;
        float clampedProgress = Math.Clamp(progress, 0.0f, 1.0f);

        // Boss から着地地点へのダイブ軌道線
        GameEngine.SetDrawBlendMode(BlendMode.Alpha, bossFx.LaserAlpha);
        GameEngine.DrawLine((int)bx, (int)by, (int)targetX, (int)targetY, color, 2);
        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        // 着地危険領域 (プレイヤーの衝突箱高さを基準に算出)
        int tx = (int)targetX;
        int ty = (int)targetY;
        int halfWidth = (int)impactRadius;
        int halfHeight = (int)(playerCfg.HitboxHeight / 2.0f);
        int x1 = tx - halfWidth;
        int y1 = ty - halfHeight;
        int x2 = tx + halfWidth;
        int y2 = ty + halfHeight;

        GameEngine.SetDrawBlendMode(BlendMode.Alpha, bossFx.DangerBoxAlpha);
        GameEngine.DrawBox(x1, y1, x2, y2, color, true);
        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        GameEngine.DrawBox(x1, y1, x2, y2, color, false);

        int textX = (int)(tx - bossFx.DiveRamTextOffsetX);
        int textY = (int)(y1 - bossFx.DiveRamTextOffsetY);
        GameEngine.DrawString(textX, textY, "▼ DIVE ▼", bossFx.WarningTextColor);
    }

    /// <summary>
    /// 撼地踏撃用の地表水平警告レールを描画します。
    /// </summary>
    /// <param name="bx">Boss 中心 X 座標</param>
    /// <param name="groundY">地面 Y 座標</param>
    /// <param name="progress">前隙進行度 (0.0 ～ 1.0)</param>
    /// <param name="color">予兆カラー</param>
    public static void DrawGroundShockwaveRails(
        float bx,
        float groundY,
        float progress,
        Color color)
    {
        BossEffectsRenderConfig bossFx = ConfigManager.Render.BossEffects;
        float clampedProgress = Math.Clamp(progress, 0.0f, 1.0f);
        float halfSpan = 200.0f + 150.0f * clampedProgress;

        TelegraphGeometry.CalculateGroundRails(bx, groundY, halfSpan, out int leftX1, out int rightX2, out int y1, out int y2);

        // 地表半透明帯
        GameEngine.SetDrawBlendMode(BlendMode.Alpha, bossFx.DangerBoxAlpha);
        GameEngine.DrawBox(leftX1, y1, rightX2, y2, color, true);
        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        // 上下枠線
        GameEngine.DrawLine(leftX1, y1, rightX2, y1, color, 1);
        GameEngine.DrawLine(leftX1, y2, rightX2, y2, color, 1);

        // 左右拡散案内矢印 (設定ファイルからオフセットを適用)
        int textLeftX = (int)(leftX1 + bossFx.StompTextOffsetX);
        int textLeftY = (int)(y1 - bossFx.StompTextOffsetY);
        int textRightX = (int)(rightX2 - 65);
        int textRightY = (int)(y1 - bossFx.StompTextOffsetY);
        GameEngine.DrawString(textLeftX, textLeftY, "◄◄ STOMP", bossFx.WarningTextColor);
        GameEngine.DrawString(textRightX, textRightY, "STOMP ►►", bossFx.WarningTextColor);
    }

    /// <summary>
    /// 泰山圧頂・巨力飛撲用の着地危険ゾーンを描画します。
    /// </summary>
    /// <param name="targetX">着地目標 X 座標</param>
    /// <param name="targetY">着地目標 Y 座標</param>
    /// <param name="progress">前隙進行度 (0.0 ～ 1.0)</param>
    /// <param name="color">予兆カラー</param>
    public static void DrawLeapSmashTelegraph(
        float targetX,
        float targetY,
        float progress,
        Color color)
    {
        BossEffectsRenderConfig bossFx = ConfigManager.Render.BossEffects;
        PlayerConfig playerCfg = ConfigManager.Player;
        Boss2Config b2Cfg = ConfigManager.Boss2;
        float clampedProgress = Math.Clamp(progress, 0.0f, 1.0f);

        int tx = (int)targetX;
        int ty = (int)targetY;
        int halfWidth = (int)(b2Cfg.BodyHitboxWidth > 0.0f ? b2Cfg.BodyHitboxWidth : 70.0f);
        int halfHeight = (int)(playerCfg.HitboxHeight / 2.0f);
        int x1 = tx - halfWidth;
        int y1 = ty - halfHeight;
        int x2 = tx + halfWidth;
        int y2 = ty + halfHeight;


        // 地面半透明危険ゾーン
        GameEngine.SetDrawBlendMode(BlendMode.Alpha, (int)(bossFx.DangerBoxAlpha * (0.6f + 0.4f * clampedProgress)));
        GameEngine.DrawBox(x1, y1, x2, y2, color, true);
        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        GameEngine.DrawBox(x1, y1, x2, y2, color, false);

        // 垂直照準破線風ガイド
        GameEngine.DrawLine(tx, 0, tx, ty, color, 1);
        int textX = (int)(tx - bossFx.LeapSmashTextOffsetX);
        int textY = (int)(y1 - bossFx.LeapSmashTextOffsetY);
        GameEngine.DrawString(textX, textY, "▼ SMASH ▼", bossFx.WarningTextColor);
    }

    /// <summary>
    /// 降雨・落石用の垂直半透明危険光柱を描画します。
    /// </summary>
    /// <param name="minX">発生最小 X 座標</param>
    /// <param name="maxX">発生最大 X 座標</param>
    /// <param name="columnCount">光柱本数</param>
    /// <param name="groundY">地面 Y 座標</param>
    /// <param name="progress">前隙進行度 (0.0 ～ 1.0)</param>
    /// <param name="color">予兆カラー</param>
    /// <param name="markerText">光柱上部表示テキスト</param>
    public static void DrawSkyfallColumns(
        float minX,
        float maxX,
        int columnCount,
        float groundY,
        float progress,
        Color color,
        string markerText)
    {
        BossEffectsRenderConfig bossFx = ConfigManager.Render.BossEffects;
        float clampedProgress = Math.Clamp(progress, 0.0f, 1.0f);
        int alpha = (int)(bossFx.DangerBoxAlpha * (0.4f + 0.6f * clampedProgress));
        float step = (maxX - minX) / Math.Max(1, columnCount + 1);

        GameEngine.SetDrawBlendMode(BlendMode.Alpha, alpha);

        for (int i = 1; i <= columnCount; i++)
        {
            int cx = (int)(minX + i * step);
            int x1 = cx - 14;
            int x2 = cx + 14;
            int y1 = 40;
            int y2 = (int)groundY;

            GameEngine.DrawBox(x1, y1, x2, y2, color, true);
        }

        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        // 光柱上部の警告マーカー (設定ファイルからオフセットを適用)
        for (int i = 1; i <= columnCount; i++)
        {
            int cx = (int)(minX + i * step);
            int x1 = cx - 14;
            int x2 = cx + 14;
            int y1 = 40;
            int y2 = (int)groundY;

            GameEngine.DrawLine(x1, y1, x1, y2, color, 1);
            GameEngine.DrawLine(x2, y1, x2, y2, color, 1);
            int textX = (int)(cx - bossFx.SkyfallTextOffsetX);
            int textY = (int)(y1 - bossFx.SkyfallTextOffsetY);
            GameEngine.DrawString(textX, textY, markerText, bossFx.WarningTextColor);
        }
    }


    /// <summary>
    /// 全方位弾幕用の放射状スパイク光線を描画します。
    /// </summary>
    /// <param name="bx">Boss 中心 X 座標</param>
    /// <param name="by">Boss 中心 Y 座標</param>
    /// <param name="spikeCount">放射スパイク本数</param>
    /// <param name="baseAngle">回転基準角度（ラジアン）</param>
    /// <param name="spikeLength">スパイク長さ (px)</param>
    /// <param name="progress">前隙進行度 (0.0 ～ 1.0)</param>
    /// <param name="color">スパイクカラー</param>
    public static void DrawRadialThreatSpikes(
        float bx,
        float by,
        int spikeCount,
        float baseAngle,
        float spikeLength,
        float progress,
        Color color)
    {
        BossEffectsRenderConfig bossFx = ConfigManager.Render.BossEffects;
        float clampedProgress = Math.Clamp(progress, 0.0f, 1.0f);
        float currentLength = spikeLength * (0.5f + 0.5f * clampedProgress);
        int alpha = (int)(bossFx.LaserAlpha * (0.5f + 0.5f * clampedProgress));
        float angleStep = MathF.PI * 2.0f / spikeCount;

        GameEngine.SetDrawBlendMode(BlendMode.Alpha, alpha);

        for (int i = 0; i < spikeCount; i++)
        {
            float angle = baseAngle + i * angleStep;
            TelegraphGeometry.CalculateSpikeRay(bx, by, angle, currentLength, out int x1, out int y1, out int x2, out int y2);
            GameEngine.DrawLine(x1, y1, x2, y2, color, 2);
        }

        // Boss 本体の集束エネルギーパルス（加算ブレンド）
        int centerAlpha = (int)(100.0f + 120.0f * clampedProgress);
        GameEngine.SetDrawBlendMode(BlendMode.Add, centerAlpha);
        GameEngine.DrawBox((int)bx - 12, (int)by - 12, (int)bx + 12, (int)by + 12, color, true);

        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
    }
}

