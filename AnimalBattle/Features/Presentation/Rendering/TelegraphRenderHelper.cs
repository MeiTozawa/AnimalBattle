using System.Drawing;

namespace AnimalBattle.Presentation.Rendering;

/// <summary>
///     Boss の攻撃予兆：Telegraph / Indicator描画を一括管理する静的ヘルパークラス。
///     従来の円環を全廃し、現代の弾幕およびアクションゲームに準拠した高視認性・直感的な危険予兆を提供します。
/// </summary>
public static class TelegraphRenderHelper {
    /// <summary>
    ///     設定未指定時に使用される安全なデフォルト演出設定。
    /// </summary>
    public static BossEffectsRenderConfig DefaultEffects { get; set; } = new();

    /// <summary>
    ///     設定未指定時に使用される安全なデフォルトアリーナ設定。
    /// </summary>
    private static readonly ArenaConfig DefaultArena = new();

    /// <summary>
    ///     自機狙い・スナイパー用の照準レーザー線および照準クロスヘアを描画します。
    /// </summary>
    /// <param name="start">Boss 基準座標</param>
    /// <param name="target">プレイヤー目標座標</param>
    /// <param name="spreadAngle">拡散角度ラジアン単位</param>
    /// <param name="laserCount">照射レーザー本数</param>
    /// <param name="progress">前隙進行度 0.0から1.0の範囲</param>
    /// <param name="color">レーザーカラー</param>
    /// <param name="effects">演出設定：省略時はデフォルト</param>
    /// <param name="isPrecisionSniper">高精度スナイパー演出：加算発光・長距離ロックを行うか</param>
    public static void DrawAimingLasers(
        Vec2f start,
        Vec2f target,
        float spreadAngle,
        int laserCount,
        float progress,
        Color color,
        BossEffectsRenderConfig? effects = null,
        bool isPrecisionSniper = false)
    {
        var eff = effects ?? DefaultEffects;
        var clampedProgress = Math.Clamp(progress, 0.0f, 1.0f);
        var alpha = (int)(eff.LaserAlpha * (0.4f + 0.6f * clampedProgress));
        var thickness = isPrecisionSniper
            ? Math.Max(2, eff.LaserThickness + (clampedProgress > 0.8f ? 1 : 0))
            : eff.LaserThickness;
        var maxDistance = 1400.0f;

        GameEngine.SetDrawBlendMode(BlendMode.Alpha, alpha);

        var midIndex = laserCount / 2;
        var startI = (Vec2i)start;
        for (var i = 0; i < laserCount; i++)
        {
            var offset = (i - midIndex) * spreadAngle;
            var end = TelegraphGeometry.CalculateAimLaserEndpoint(start, target, offset, maxDistance);
            GameEngine.DrawLine(startI, (Vec2i)end, color, thickness);
        }

        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        // ターゲット地点の照準クロスヘア [+] の描画
        var targetI = (Vec2i)target;
        var crosshairSize = (int)(12.0f - clampedProgress * 4.0f);
        GameEngine.DrawLine(new Vec2i(targetI.X - crosshairSize, targetI.Y), new Vec2i(targetI.X + crosshairSize, targetI.Y), color);
        GameEngine.DrawLine(new Vec2i(targetI.X, targetI.Y - crosshairSize), new Vec2i(targetI.X, targetI.Y + crosshairSize), color);

        // スナイパー発射寸前の閃光：加算ブレンド
        if (isPrecisionSniper && clampedProgress > 0.75f)
        {
            var flashAlpha = (int)((clampedProgress - 0.75f) / 0.25f * 220.0f);
            GameEngine.SetDrawBlendMode(BlendMode.Add, flashAlpha);
            GameEngine.DrawLine(startI, targetI, eff.ChargeFillColor, thickness + 1);
            GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
        }
    }

    /// <summary>
    ///     近接斬撃・重劈用の危険判定矩形ゾーンおよびチャージ進行ゲージを描画します。
    /// </summary>
    /// <param name="bPos">Boss 中心座標</param>
    /// <param name="range">斬撃有効半径 水平および垂直半径</param>
    /// <param name="facingRight">Boss が右を向いているか</param>
    /// <param name="progress">前隙進行度 0.0から1.0の範囲</param>
    /// <param name="color">危険ゾーンカラー</param>
    /// <param name="effects">演出設定：省略時はデフォルト</param>
    public static void DrawMeleeDangerBox(
        Vec2f bPos,
        Vec2f range,
        bool facingRight,
        float progress,
        Color color,
        BossEffectsRenderConfig? effects = null)
    {
        var eff = effects ?? DefaultEffects;
        var clampedProgress = Math.Clamp(progress, 0.0f, 1.0f);
        TelegraphGeometry.CalculateMeleeDangerRect(bPos, range, facingRight, out var p1, out var p2);

        // 半透明危険ゾーン背景
        GameEngine.SetDrawBlendMode(BlendMode.Alpha, eff.DangerBoxAlpha);
        GameEngine.DrawBox(p1, p2, color, true);

        // チャージ進行度に応じた内部充填バー：Boss 側から外側へ向けて充填
        if (facingRight)
        {
            var fillX2 = (int)(p1.X + (p2.X - p1.X) * clampedProgress);
            GameEngine.DrawBox(p1, new Vec2i(fillX2, p2.Y), eff.ChargeFillColor, true);
        }
        else
        {
            var fillX1 = (int)(p2.X - (p2.X - p1.X) * clampedProgress);
            GameEngine.DrawBox(new Vec2i(fillX1, p1.Y), p2, eff.ChargeFillColor, true);
        }

        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        // 外枠線
        GameEngine.DrawBox(p1, p2, color, false);

        // 斬撃方向案内テキスト：左右配置と垂直中央揃え
        var centerY = p1.Y + (p2.Y - p1.Y) / 2;
        if (facingRight)
            GameEngine.DrawRightString(new Vec2i(p2.X - 8, centerY), ">>>", eff.WarningTextColor);
        else
            GameEngine.DrawLeftString(new Vec2i(p1.X + 8, centerY), "<<<", eff.WarningTextColor);
    }

    /// <summary>
    ///     横狂暴突進用の全画面水平危険軌道レーンを描画します。
    /// </summary>
    /// <param name="bPos">Boss 中心座標</param>
    /// <param name="height">突進衝突高さ</param>
    /// <param name="facingRight">突進方向：右向きか</param>
    /// <param name="progress">前隙進行度 0.0から1.0の範囲</param>
    /// <param name="color">危険軌道カラー</param>
    /// <param name="effects">演出設定：省略時はデフォルト</param>
    /// <param name="arena">アリーナ設定：省略時はデフォルト</param>
    public static void DrawChargeDangerTrack(
        Vec2f bPos,
        float height,
        bool facingRight,
        float progress,
        Color color,
        BossEffectsRenderConfig? effects = null,
        ArenaConfig? arena = null)
    {
        var eff = effects ?? DefaultEffects;
        var ar = arena ?? DefaultArena;
        var clampedProgress = Math.Clamp(progress, 0.0f, 1.0f);

        TelegraphGeometry.CalculateChargeTrackRect(bPos, height, facingRight, ar.BoundaryMinX,
            ar.BoundaryMaxX, out var p1, out var p2);

        // 半透明突進帯
        GameEngine.SetDrawBlendMode(BlendMode.Alpha, eff.ChargeTrackAlpha);
        GameEngine.DrawBox(p1, p2, color, true);

        // 突進チャージ内部充填バー：Boss から突進方向へ向けて充填
        if (facingRight)
        {
            var fillX2 = (int)(bPos.X + (p2.X - bPos.X) * clampedProgress);
            GameEngine.DrawBox(new Vec2i((int)bPos.X, p1.Y), new Vec2i(fillX2, p2.Y), eff.ChargeFillColor, true);
        }
        else
        {
            var fillX1 = (int)(bPos.X - (bPos.X - p1.X) * clampedProgress);
            GameEngine.DrawBox(new Vec2i(fillX1, p1.Y), new Vec2i((int)bPos.X, p2.Y), eff.ChargeFillColor, true);
        }
        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        // 上下レール境界線
        GameEngine.DrawLine(p1, new Vec2i(p2.X, p1.Y), color);
        GameEngine.DrawLine(new Vec2i(p1.X, p2.Y), p2, color);

        // 進行方向案内矢印：Boss の身前から突進方向へ表示
        var centerY = p1.Y + (p2.Y - p1.Y) / 2;
        if (facingRight)
            GameEngine.DrawLeftString(new Vec2i((int)bPos.X + 20, centerY), ">>>>>>>>", eff.WarningTextColor);
        else
            GameEngine.DrawRightString(new Vec2i((int)bPos.X - 20, centerY), "<<<<<<<<", eff.WarningTextColor);

        // Boss 自身の突進チャージ脈動：加算ブレンド
        var pulseAlpha = (int)(80.0f + 140.0f * clampedProgress);
        GameEngine.SetDrawBlendMode(BlendMode.Add, pulseAlpha);
        GameEngine.DrawBox(new Vec2i((int)bPos.X - 20, p1.Y), new Vec2i((int)bPos.X + 20, p2.Y), eff.ChargeFillColor, true);
        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
    }

    /// <summary>
    ///     俯衝突進・メテオダイブ：DiveRam用の突進軌道線および着地危険領域を描画します。
    /// </summary>
    /// <param name="bPos">Boss 中心座標</param>
    /// <param name="target">着地目標座標</param>
    /// <param name="impactRadius">着地衝撃波半径</param>
    /// <param name="progress">前隙進行度 0.0から1.0の範囲</param>
    /// <param name="color">予兆カラー</param>
    /// <param name="effects">演出設定：省略時はデフォルト</param>
    /// <param name="playerCollisionHeight">プレイヤー衝突高さ</param>
    public static void DrawDiveRamTelegraph(
        Vec2f bPos,
        Vec2f target,
        float impactRadius,
        float progress,
        Color color,
        BossEffectsRenderConfig? effects = null,
        float playerCollisionHeight = 32.0f)
    {
        var eff = effects ?? DefaultEffects;
        var clampedProgress = Math.Clamp(progress, 0.0f, 1.0f);

        // Boss から着地地点へのダイブ軌道線：進行度に応じたアルファ脈動
        var laserAlpha = (int)(eff.LaserAlpha * (0.5f + 0.5f * clampedProgress));
        GameEngine.SetDrawBlendMode(BlendMode.Alpha, laserAlpha);
        GameEngine.DrawLine((Vec2i)bPos, (Vec2i)target, color, eff.LaserThickness);
        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        // 着地危険領域 プレイヤーの衝突箱高さを基準に算出します
        var targetI = (Vec2i)target;
        var halfWidth = (int)impactRadius;
        var halfHeight = (int)(playerCollisionHeight / 2.0f);
        var p1 = new Vec2i(targetI.X - halfWidth, targetI.Y - halfHeight);
        var p2 = new Vec2i(targetI.X + halfWidth, targetI.Y + halfHeight);

        // 半透明危険ゾーン背景：進行度に応じてアルファ増大
        var boxAlpha = (int)(eff.DangerBoxAlpha * (0.6f + 0.4f * clampedProgress));
        GameEngine.SetDrawBlendMode(BlendMode.Alpha, boxAlpha);
        GameEngine.DrawBox(p1, p2, color, true);

        // 進行度に応じた中心からの危険充填矩形
        var fillW = (int)(halfWidth * clampedProgress);
        var fillH = (int)(halfHeight * clampedProgress);
        GameEngine.DrawBox(
            new Vec2i(targetI.X - fillW, targetI.Y - fillH),
            new Vec2i(targetI.X + fillW, targetI.Y + fillH),
            eff.ChargeFillColor,
            true);
        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        // 外枠線
        GameEngine.DrawBox(p1, p2, color, false);

        // 着地領域へ収縮する警戒カウントダウンフレーム
        var shrinkRatio = 1.0f + (1.0f - clampedProgress) * 1.5f;
        var shrinkW = (int)(halfWidth * shrinkRatio);
        var shrinkH = (int)(halfHeight * shrinkRatio);
        GameEngine.SetDrawBlendMode(BlendMode.Alpha, (int)(180.0f * clampedProgress));
        GameEngine.DrawBox(
            new Vec2i(targetI.X - shrinkW, targetI.Y - shrinkH),
            new Vec2i(targetI.X + shrinkW, targetI.Y + shrinkH),
            color,
            false);
        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        // 警告テキスト：ターゲット中心に水平垂直中央揃え
        var textPos = new Vec2i(
            (int)target.X,
            (int)(p1.Y - eff.DiveRamTextOffset.Y)
        );
        GameEngine.DrawCenterString(textPos, "▼ DIVE ▼", eff.WarningTextColor);
    }

    /// <summary>
    ///     撼地踏撃用の地表水平警告レールを描画します。
    /// </summary>
    /// <param name="bPos">Boss 中心座標</param>
    /// <param name="groundY">地面 Y 座標</param>
    /// <param name="progress">前隙進行度 0.0から1.0の範囲</param>
    /// <param name="color">予兆カラー</param>
    /// <param name="effects">演出設定：省略時はデフォルト</param>
    public static void DrawGroundShockwaveRails(
        Vec2f bPos,
        float groundY,
        float progress,
        Color color,
        BossEffectsRenderConfig? effects = null)
    {
        var eff = effects ?? DefaultEffects;
        var clampedProgress = Math.Clamp(progress, 0.0f, 1.0f);
        var halfSpan = 200.0f + 150.0f * clampedProgress;

        TelegraphGeometry.CalculateGroundRails(bPos, groundY, halfSpan, out var p1, out var p2);

        // 地表半透明帯
        GameEngine.SetDrawBlendMode(BlendMode.Alpha, eff.DangerBoxAlpha);
        GameEngine.DrawBox(p1, p2, color, true);
        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        // 上下枠線
        GameEngine.DrawLine(p1, new Vec2i(p2.X, p1.Y), color);
        GameEngine.DrawLine(new Vec2i(p1.X, p2.Y), p2, color);

        // 左右拡散案内矢印：左揃えおよび右揃えで配置
        var textY = (int)(p1.Y - eff.StompTextOffset.Y);
        var textLeftPos = new Vec2i(
            (int)(p1.X + eff.StompTextOffset.X),
            textY
        );
        var textRightPos = new Vec2i(
            (int)(p2.X - eff.StompTextOffset.X),
            textY
        );
        GameEngine.DrawLeftString(textLeftPos, "<<< STOMP", eff.WarningTextColor);
        GameEngine.DrawRightString(textRightPos, "STOMP >>>", eff.WarningTextColor);
    }

    /// <summary>
    ///     泰山圧頂・巨力飛撲用の着地危険ゾーンを描画します。
    /// </summary>
    /// <param name="target">着地目標座標</param>
    /// <param name="progress">前隙進行度 0.0から1.0の範囲</param>
    /// <param name="color">予兆カラー</param>
    /// <param name="effects">演出設定：省略時はデフォルト</param>
    /// <param name="playerCollisionHeight">プレイヤー衝突高さ</param>
    /// <param name="bossBodyHitboxWidth">ボス衝突幅</param>
    public static void DrawLeapSmashTelegraph(
        Vec2f target,
        float progress,
        Color color,
        BossEffectsRenderConfig? effects = null,
        float playerCollisionHeight = 32.0f,
        float bossBodyHitboxWidth = 70.0f)
    {
        var eff = effects ?? DefaultEffects;
        var clampedProgress = Math.Clamp(progress, 0.0f, 1.0f);

        var targetI = (Vec2i)target;
        var halfWidth = (int)(bossBodyHitboxWidth > 0.0f ? bossBodyHitboxWidth : 70.0f);
        var halfHeight = (int)(playerCollisionHeight / 2.0f);
        var p1 = new Vec2i(targetI.X - halfWidth, targetI.Y - halfHeight);
        var p2 = new Vec2i(targetI.X + halfWidth, targetI.Y + halfHeight);

        // 地面半透明危険ゾーン
        GameEngine.SetDrawBlendMode(BlendMode.Alpha, (int)(eff.DangerBoxAlpha * (0.6f + 0.4f * clampedProgress)));
        GameEngine.DrawBox(p1, p2, color, true);

        // 進行度に応じた水平充填
        var fillW = (int)(halfWidth * clampedProgress);
        GameEngine.DrawBox(new Vec2i(targetI.X - fillW, p1.Y), new Vec2i(targetI.X + fillW, p2.Y), eff.ChargeFillColor, true);
        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        GameEngine.DrawBox(p1, p2, color, false);

        // 着地領域へ収縮する警戒カウントダウンフレーム
        var shrinkRatio = 1.0f + (1.0f - clampedProgress) * 1.5f;
        var shrinkW = (int)(halfWidth * shrinkRatio);
        var shrinkH = (int)(halfHeight * shrinkRatio);
        GameEngine.SetDrawBlendMode(BlendMode.Alpha, (int)(180.0f * clampedProgress));
        GameEngine.DrawBox(
            new Vec2i(targetI.X - shrinkW, targetI.Y - shrinkH),
            new Vec2i(targetI.X + shrinkW, targetI.Y + shrinkH),
            color,
            false);
        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        // 垂直照準ガイド
        GameEngine.DrawLine(new Vec2i(targetI.X, 0), targetI, color);
        var textPos = new Vec2i(
            (int)target.X,
            (int)(p1.Y - eff.LeapSmashTextOffset.Y)
        );
        GameEngine.DrawCenterString(textPos, "▼ SMASH ▼", eff.WarningTextColor);
    }

    /// <summary>
    ///     降雨・落石用の垂直半透明危険光柱を描画します。
    /// </summary>
    /// <param name="minX">発生最小 X 座標</param>
    /// <param name="maxX">発生最大 X 座標</param>
    /// <param name="columnCount">光柱本数</param>
    /// <param name="groundY">地面 Y 座標</param>
    /// <param name="progress">前隙進行度 0.0から1.0の範囲</param>
    /// <param name="color">予兆カラー</param>
    /// <param name="markerText">光柱上部表示テキスト</param>
    /// <param name="effects">演出設定：省略時はデフォルト</param>
    public static void DrawSkyfallColumns(
        float minX,
        float maxX,
        int columnCount,
        float groundY,
        float progress,
        Color color,
        string markerText,
        BossEffectsRenderConfig? effects = null)
    {
        var eff = effects ?? DefaultEffects;
        var clampedProgress = Math.Clamp(progress, 0.0f, 1.0f);
        var alpha = (int)(eff.DangerBoxAlpha * (0.4f + 0.6f * clampedProgress));
        var step = (maxX - minX) / Math.Max(1, columnCount + 1);

        GameEngine.SetDrawBlendMode(BlendMode.Alpha, alpha);

        for (var i = 1; i <= columnCount; i++)
        {
            var cx = (int)(minX + i * step);
            var p1 = new Vec2i(cx - 14, 40);
            var p2 = new Vec2i(cx + 14, (int)groundY);

            GameEngine.DrawBox(p1, p2, color, true);
        }

        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        // 光柱上部の警告マーカー 設定ファイルからオフセットを適用します
        for (var i = 1; i <= columnCount; i++)
        {
            var cx = (int)(minX + i * step);
            var p1 = new Vec2i(cx - 14, 40);
            var p2 = new Vec2i(cx + 14, (int)groundY);

            GameEngine.DrawLine(p1, new Vec2i(p1.X, p2.Y), color);
            GameEngine.DrawLine(new Vec2i(p2.X, p1.Y), p2, color);
            var textPos = new Vec2i(
                cx,
                (int)(p1.Y - eff.SkyfallTextOffset.Y)
            );
            GameEngine.DrawCenterString(textPos, markerText, eff.WarningTextColor);
        }
    }

    /// <summary>
    ///     全方位弾幕用の放射状スパイク光線を描画します。
    /// </summary>
    /// <param name="bPos">Boss 中心座標</param>
    /// <param name="spikeCount">放射スパイク本数</param>
    /// <param name="baseAngle">回転基準角度ラジアン単位</param>
    /// <param name="spikeLength">スパイク長さ ピクセル単位</param>
    /// <param name="progress">前隙進行度 0.0から1.0の範囲</param>
    /// <param name="color">スパイクカラー</param>
    /// <param name="effects">演出設定：省略時はデフォルト</param>
    public static void DrawRadialThreatSpikes(
        Vec2f bPos,
        int spikeCount,
        float baseAngle,
        float spikeLength,
        float progress,
        Color color,
        BossEffectsRenderConfig? effects = null)
    {
        var eff = effects ?? DefaultEffects;
        var clampedProgress = Math.Clamp(progress, 0.0f, 1.0f);
        var currentLength = spikeLength * (0.5f + 0.5f * clampedProgress);
        var alpha = (int)(eff.LaserAlpha * (0.5f + 0.5f * clampedProgress));
        var angleStep = MathF.PI * 2.0f / spikeCount;

        GameEngine.SetDrawBlendMode(BlendMode.Alpha, alpha);

        for (var i = 0; i < spikeCount; i++)
        {
            var angle = baseAngle + i * angleStep;
            TelegraphGeometry.CalculateSpikeRay(bPos, angle, currentLength, out var p1, out var p2);
            GameEngine.DrawLine(p1, p2, color, 2);
        }

        // Boss 本体の集束エネルギーパルス：加算ブレンド
        var centerAlpha = (int)(100.0f + 120.0f * clampedProgress);
        var centerI = (Vec2i)bPos;
        GameEngine.SetDrawBlendMode(BlendMode.Add, centerAlpha);
        GameEngine.DrawBox(
            new Vec2i(centerI.X - 12, centerI.Y - 12),
            new Vec2i(centerI.X + 12, centerI.Y + 12),
            color,
            true);

        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
    }
}
