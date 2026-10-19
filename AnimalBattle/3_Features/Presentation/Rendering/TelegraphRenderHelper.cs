using System.Diagnostics;
using System.Drawing;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Core.Geometry;

namespace AnimalBattle.Render;

/// <summary>
///     Boss の攻撃予兆（Telegraph / Indicator）描画を一括管理する静的ヘルパークラス。
///     従来の円環を全廃し、現代の弾幕およびアクションゲームに準拠した高視認性・直感的な危険予兆を提供します。
/// </summary>
public static class TelegraphRenderHelper {
    /// <summary>
    ///     現在の首領演出描画設定。描画システムまたは ConfigService から設定されます。
    /// </summary>
    public static BossEffectsRenderConfig CurrentEffects { get; set; } = null!;

    /// <summary>
    ///     現在の戦闘アリーナ設定。描画システムまたは ConfigService から設定されます。
    /// </summary>
    public static ArenaConfig CurrentArena { get; set; } = null!;

    /// <summary>
    ///     自機狙い・スナイパー用の照準レーザー線および照準クロスヘアを描画します。
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
        Debug.Assert(CurrentEffects != null, "TelegraphRenderHelper.CurrentEffects が初期化されていません。");

        var clampedProgress = Math.Clamp(progress, 0.0f, 1.0f);
        var alpha = (int)(CurrentEffects.LaserAlpha * (0.4f + 0.6f * clampedProgress));
        var thickness = isPrecisionSniper
            ? Math.Max(2, CurrentEffects.LaserThickness + (clampedProgress > 0.8f ? 1 : 0))
            : CurrentEffects.LaserThickness;
        var maxDistance = 1400.0f;

        GameEngine.SetDrawBlendMode(BlendMode.Alpha, alpha);

        var midIndex = laserCount / 2;
        for (var i = 0; i < laserCount; i++)
        {
            var offset = (i - midIndex) * spreadAngle;
            TelegraphGeometry.CalculateAimLaserEndpoint(startX, startY, targetX, targetY, offset, maxDistance,
                out var endX, out var endY);
            GameEngine.DrawLine((int)startX, (int)startY, (int)endX, (int)endY, color, thickness);
        }

        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        // ターゲット地点の照準クロスヘア [+] の描画
        var tx = (int)targetX;
        var ty = (int)targetY;
        var crosshairSize = (int)(12.0f - clampedProgress * 4.0f);
        GameEngine.DrawLine(tx - crosshairSize, ty, tx + crosshairSize, ty, color);
        GameEngine.DrawLine(tx, ty - crosshairSize, tx, ty + crosshairSize, color);

        // スナイパー発射寸前の閃光（加算ブレンド）
        if (isPrecisionSniper && clampedProgress > 0.75f)
        {
            var flashAlpha = (int)((clampedProgress - 0.75f) / 0.25f * 220.0f);
            GameEngine.SetDrawBlendMode(BlendMode.Add, flashAlpha);
            GameEngine.DrawLine((int)startX, (int)startY, tx, ty, ColorPalette.White, thickness + 1);
            GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
        }
    }

    /// <summary>
    ///     近接斬撃・重劈用の危険判定矩形ゾーンおよびチャージ進行ゲージを描画します。
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
        Debug.Assert(CurrentEffects != null, "TelegraphRenderHelper.CurrentEffects が初期化されていません。");

        var clampedProgress = Math.Clamp(progress, 0.0f, 1.0f);
        TelegraphGeometry.CalculateMeleeDangerRect(bx, by, rangeX, rangeY, facingRight, out var x1, out var y1,
            out var x2, out var y2);

        // 半透明危険ゾーン背景
        GameEngine.SetDrawBlendMode(BlendMode.Alpha, CurrentEffects.DangerBoxAlpha);
        GameEngine.DrawBox(x1, y1, x2, y2, color, true);

        // チャージ進行度に応じた内部充填バー（Boss 側から外側へ向けて充填）
        if (facingRight)
        {
            var fillX2 = (int)(x1 + (x2 - x1) * clampedProgress);
            GameEngine.DrawBox(x1, y1, fillX2, y2, ColorPalette.White, true);
        }
        else
        {
            var fillX1 = (int)(x2 - (x2 - x1) * clampedProgress);
            GameEngine.DrawBox(fillX1, y1, x2, y2, ColorPalette.White, true);
        }

        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        // 外枠線
        GameEngine.DrawBox(x1, y1, x2, y2, color, false);

        // 斬撃方向案内テキスト（ASCII 矢印によるフォント互換性確保）
        var textX = facingRight ? x2 - 24 : x1 + 8;
        var textY = y1 + (y2 - y1) / 2 - 6;
        var arrow = facingRight ? ">>>" : "<<<";
        GameEngine.DrawString(textX, textY, arrow, CurrentEffects.WarningTextColor);
    }

    /// <summary>
    ///     横狂暴突進用の全画面水平危険軌道レーンを描画します。
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
        Debug.Assert(CurrentEffects != null, "TelegraphRenderHelper.CurrentEffects が初期化されていません。");
        Debug.Assert(CurrentArena != null, "TelegraphRenderHelper.CurrentArena が初期化されていません。");

        var clampedProgress = Math.Clamp(progress, 0.0f, 1.0f);

        TelegraphGeometry.CalculateChargeTrackRect(bx, by, height, facingRight, CurrentArena.BoundaryMinX,
            CurrentArena.BoundaryMaxX, out var x1, out var y1, out var x2, out var y2);

        // 半透明突進帯
        GameEngine.SetDrawBlendMode(BlendMode.Alpha, CurrentEffects.ChargeTrackAlpha);
        GameEngine.DrawBox(x1, y1, x2, y2, color, true);

        // 突進チャージ内部充填バー（Boss から突進方向へ向けて充填）
        if (facingRight)
        {
            var fillX2 = (int)(bx + (x2 - bx) * clampedProgress);
            GameEngine.DrawBox((int)bx, y1, fillX2, y2, ColorPalette.White, true);
        }
        else
        {
            var fillX1 = (int)(bx - (bx - x1) * clampedProgress);
            GameEngine.DrawBox(fillX1, y1, (int)bx, y2, ColorPalette.White, true);
        }
        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        // 上下レール境界線
        GameEngine.DrawLine(x1, y1, x2, y1, color);
        GameEngine.DrawLine(x1, y2, x2, y2, color);

        // 進行方向案内矢印（Boss の身前から突進方向へ表示）
        var arrow = facingRight ? ">>>>>>>>" : "<<<<<<<<";
        var textY = y1 + (y2 - y1) / 2 - 6;
        var textX = facingRight ? (int)bx + 20 : (int)bx - 76;
        GameEngine.DrawString(textX, textY, arrow, CurrentEffects.WarningTextColor);

        // Boss 自身の突進チャージ脈動（加算ブレンド）
        var pulseAlpha = (int)(80.0f + 140.0f * clampedProgress);
        GameEngine.SetDrawBlendMode(BlendMode.Add, pulseAlpha);
        GameEngine.DrawBox((int)bx - 20, y1, (int)bx + 20, y2, ColorPalette.White, true);
        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
    }

    /// <summary>
    ///     俯衝突進・メテオダイブ（DiveRam）用の突進軌道線および着地危険領域を描画します。
    /// </summary>
    /// <param name="bx">Boss 中心 X 座標</param>
    /// <param name="by">Boss 中心 Y 座標</param>
    /// <param name="targetX">着地目標 X 座標</param>
    /// <param name="targetY">着地目標 Y 座標</param>
    /// <param name="impactRadius">着地衝撃波半径</param>
    /// <param name="progress">前隙進行度 (0.0 ～ 1.0)</param>
    /// <param name="color">予兆カラー</param>
    /// <param name="playerCollisionHeight">プレイヤー衝突高さ</param>
    public static void DrawDiveRamTelegraph(
        float bx,
        float by,
        float targetX,
        float targetY,
        float impactRadius,
        float progress,
        Color color,
        float playerCollisionHeight = 32.0f)
    {
        Debug.Assert(CurrentEffects != null, "TelegraphRenderHelper.CurrentEffects が初期化されていません。");

        var clampedProgress = Math.Clamp(progress, 0.0f, 1.0f);

        // Boss から着地地点へのダイブ軌道線（進行度に応じたアルファ脈動）
        var laserAlpha = (int)(CurrentEffects.LaserAlpha * (0.5f + 0.5f * clampedProgress));
        GameEngine.SetDrawBlendMode(BlendMode.Alpha, laserAlpha);
        GameEngine.DrawLine((int)bx, (int)by, (int)targetX, (int)targetY, color, CurrentEffects.LaserThickness);
        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        // 着地危険領域 (プレイヤーの衝突箱高さを基準に算出)
        var tx = (int)targetX;
        var ty = (int)targetY;
        var halfWidth = (int)impactRadius;
        var halfHeight = (int)(playerCollisionHeight / 2.0f);
        var x1 = tx - halfWidth;
        var y1 = ty - halfHeight;
        var x2 = tx + halfWidth;
        var y2 = ty + halfHeight;

        // 半透明危険ゾーン背景（進行度に応じてアルファ増大）
        var boxAlpha = (int)(CurrentEffects.DangerBoxAlpha * (0.6f + 0.4f * clampedProgress));
        GameEngine.SetDrawBlendMode(BlendMode.Alpha, boxAlpha);
        GameEngine.DrawBox(x1, y1, x2, y2, color, true);

        // 進行度に応じた中心からの危険充填矩形
        var fillW = (int)(halfWidth * clampedProgress);
        var fillH = (int)(halfHeight * clampedProgress);
        GameEngine.DrawBox(tx - fillW, ty - fillH, tx + fillW, ty + fillH, ColorPalette.White, true);
        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        // 外枠線
        GameEngine.DrawBox(x1, y1, x2, y2, color, false);

        // 着地領域へ収縮する警戒カウントダウンフレーム
        var shrinkRatio = 1.0f + (1.0f - clampedProgress) * 1.5f;
        var shrinkW = (int)(halfWidth * shrinkRatio);
        var shrinkH = (int)(halfHeight * shrinkRatio);
        GameEngine.SetDrawBlendMode(BlendMode.Alpha, (int)(180.0f * clampedProgress));
        GameEngine.DrawBox(tx - shrinkW, ty - shrinkH, tx + shrinkW, ty + shrinkH, color, false);
        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        // 警告テキスト
        var textX = (int)(tx - CurrentEffects.DiveRamTextOffsetX);
        var textY = (int)(y1 - CurrentEffects.DiveRamTextOffsetY);
        GameEngine.DrawString(textX, textY, "▼ DIVE ▼", CurrentEffects.WarningTextColor);
    }

    /// <summary>
    ///     撼地踏撃用の地表水平警告レールを描画します。
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
        Debug.Assert(CurrentEffects != null, "TelegraphRenderHelper.CurrentEffects が初期化されていません。");

        var clampedProgress = Math.Clamp(progress, 0.0f, 1.0f);
        var halfSpan = 200.0f + 150.0f * clampedProgress;

        TelegraphGeometry.CalculateGroundRails(bx, groundY, halfSpan, out var leftX1, out var rightX2, out var y1,
            out var y2);

        // 地表半透明帯
        GameEngine.SetDrawBlendMode(BlendMode.Alpha, CurrentEffects.DangerBoxAlpha);
        GameEngine.DrawBox(leftX1, y1, rightX2, y2, color, true);
        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        // 上下枠線
        GameEngine.DrawLine(leftX1, y1, rightX2, y1, color);
        GameEngine.DrawLine(leftX1, y2, rightX2, y2, color);

        // 左右拡散案内矢印 (設定ファイルからオフセットを適用)
        var textLeftX = (int)(leftX1 + CurrentEffects.StompTextOffsetX);
        var textLeftY = (int)(y1 - CurrentEffects.StompTextOffsetY);
        var textRightX = (int)(rightX2 - 65 - CurrentEffects.StompTextOffsetX);
        var textRightY = (int)(y1 - CurrentEffects.StompTextOffsetY);
        GameEngine.DrawString(textLeftX, textLeftY, "<<< STOMP", CurrentEffects.WarningTextColor);
        GameEngine.DrawString(textRightX, textRightY, "STOMP >>>", CurrentEffects.WarningTextColor);
    }

    /// <summary>
    ///     泰山圧頂・巨力飛撲用の着地危険ゾーンを描画します。
    /// </summary>
    /// <param name="targetX">着地目標 X 座標</param>
    /// <param name="targetY">着地目標 Y 座標</param>
    /// <param name="progress">前隙進行度 (0.0 ～ 1.0)</param>
    /// <param name="color">予兆カラー</param>
    /// <param name="playerCollisionHeight">プレイヤー衝突高さ</param>
    /// <param name="bossBodyHitboxWidth">首領衝突幅</param>
    public static void DrawLeapSmashTelegraph(
        float targetX,
        float targetY,
        float progress,
        Color color,
        float playerCollisionHeight = 32.0f,
        float bossBodyHitboxWidth = 70.0f)
    {
        Debug.Assert(CurrentEffects != null, "TelegraphRenderHelper.CurrentEffects が初期化されていません。");

        var clampedProgress = Math.Clamp(progress, 0.0f, 1.0f);

        var tx = (int)targetX;
        var ty = (int)targetY;
        var halfWidth = (int)(bossBodyHitboxWidth > 0.0f ? bossBodyHitboxWidth : 70.0f);
        var halfHeight = (int)(playerCollisionHeight / 2.0f);
        var x1 = tx - halfWidth;
        var y1 = ty - halfHeight;
        var x2 = tx + halfWidth;
        var y2 = ty + halfHeight;

        // 地面半透明危険ゾーン
        GameEngine.SetDrawBlendMode(BlendMode.Alpha, (int)(CurrentEffects.DangerBoxAlpha * (0.6f + 0.4f * clampedProgress)));
        GameEngine.DrawBox(x1, y1, x2, y2, color, true);

        // 進行度に応じた水平充填
        var fillW = (int)(halfWidth * clampedProgress);
        GameEngine.DrawBox(tx - fillW, y1, tx + fillW, y2, ColorPalette.White, true);
        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        GameEngine.DrawBox(x1, y1, x2, y2, color, false);

        // 着地領域へ収縮する警戒カウントダウンフレーム
        var shrinkRatio = 1.0f + (1.0f - clampedProgress) * 1.5f;
        var shrinkW = (int)(halfWidth * shrinkRatio);
        var shrinkH = (int)(halfHeight * shrinkRatio);
        GameEngine.SetDrawBlendMode(BlendMode.Alpha, (int)(180.0f * clampedProgress));
        GameEngine.DrawBox(tx - shrinkW, ty - shrinkH, tx + shrinkW, ty + shrinkH, color, false);
        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        // 垂直照準ガイド
        GameEngine.DrawLine(tx, 0, tx, ty, color);
        var textX = (int)(tx - CurrentEffects.LeapSmashTextOffsetX);
        var textY = (int)(y1 - CurrentEffects.LeapSmashTextOffsetY);
        GameEngine.DrawString(textX, textY, "▼ SMASH ▼", CurrentEffects.WarningTextColor);
    }

    /// <summary>
    ///     降雨・落石用の垂直半透明危険光柱を描画します。
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
        Debug.Assert(CurrentEffects != null, "TelegraphRenderHelper.CurrentEffects が初期化されていません。");

        var clampedProgress = Math.Clamp(progress, 0.0f, 1.0f);
        var alpha = (int)(CurrentEffects.DangerBoxAlpha * (0.4f + 0.6f * clampedProgress));
        var step = (maxX - minX) / Math.Max(1, columnCount + 1);

        GameEngine.SetDrawBlendMode(BlendMode.Alpha, alpha);

        for (var i = 1; i <= columnCount; i++)
        {
            var cx = (int)(minX + i * step);
            var x1 = cx - 14;
            var x2 = cx + 14;
            var y1 = 40;
            var y2 = (int)groundY;

            GameEngine.DrawBox(x1, y1, x2, y2, color, true);
        }

        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        // 光柱上部の警告マーカー (設定ファイルからオフセットを適用)
        for (var i = 1; i <= columnCount; i++)
        {
            var cx = (int)(minX + i * step);
            var x1 = cx - 14;
            var x2 = cx + 14;
            var y1 = 40;
            var y2 = (int)groundY;

            GameEngine.DrawLine(x1, y1, x1, y2, color);
            GameEngine.DrawLine(x2, y1, x2, y2, color);
            var textX = (int)(cx - CurrentEffects.SkyfallTextOffsetX);
            var textY = (int)(y1 - CurrentEffects.SkyfallTextOffsetY);
            GameEngine.DrawString(textX, textY, markerText, CurrentEffects.WarningTextColor);
        }
    }

    /// <summary>
    ///     全方位弾幕用の放射状スパイク光線を描画します。
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
        Debug.Assert(CurrentEffects != null, "TelegraphRenderHelper.CurrentEffects が初期化されていません。");

        var clampedProgress = Math.Clamp(progress, 0.0f, 1.0f);
        var currentLength = spikeLength * (0.5f + 0.5f * clampedProgress);
        var alpha = (int)(CurrentEffects.LaserAlpha * (0.5f + 0.5f * clampedProgress));
        var angleStep = MathF.PI * 2.0f / spikeCount;

        GameEngine.SetDrawBlendMode(BlendMode.Alpha, alpha);

        for (var i = 0; i < spikeCount; i++)
        {
            var angle = baseAngle + i * angleStep;
            TelegraphGeometry.CalculateSpikeRay(bx, by, angle, currentLength, out var x1, out var y1, out var x2,
                out var y2);
            GameEngine.DrawLine(x1, y1, x2, y2, color, 2);
        }

        // Boss 本体の集束エネルギーパルス（加算ブレンド）
        var centerAlpha = (int)(100.0f + 120.0f * clampedProgress);
        GameEngine.SetDrawBlendMode(BlendMode.Add, centerAlpha);
        GameEngine.DrawBox((int)bx - 12, (int)by - 12, (int)bx + 12, (int)by + 12, color, true);

        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
    }
}
