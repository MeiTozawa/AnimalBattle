using System.Drawing;

namespace AnimalBattle.Combat;

/// <summary>
///     弾幕種別に応じた既定カラーの解決および色彩整合性ポリシーを提供する静的クラスです。
/// </summary>
public static class BulletPalette
{
    /// <summary>
    ///     現在アクティブな弾幕描画設定。
    /// </summary>
    public static BulletRenderConfig? CurrentConfig { get; set; }

    /// <summary>
    ///     弾幕種別に応じた既定の塗りつぶしカラーを取得します。
    /// </summary>
    public static Color GetDefaultFillColor(BulletType type)
    {
        var cfg = CurrentConfig;
        if (cfg == null) return Color.Empty;

        return type switch {
            BulletType.Normal => cfg.BulletNormalFillColor,
            BulletType.Magenta => cfg.BulletMagentaFillColor,
            BulletType.Yellow => cfg.BulletYellowFillColor,
            BulletType.Cyan => cfg.BulletCyanFillColor,
            BulletType.Black => cfg.BulletBlackFillColor,
            _ => AssertHelper.FailWithDefault($"[BulletPalette 警告] 未知の BulletType (Fill): {type}", cfg.BulletNormalFillColor)
        };
    }

    /// <summary>
    ///     弾幕種別に応じた既定の外枠カラーを取得します。
    /// </summary>
    public static Color GetDefaultOutlineColor(BulletType type)
    {
        var cfg = CurrentConfig;
        if (cfg == null) return Color.Empty;

        return type switch {
            BulletType.Normal => cfg.BulletNormalOutlineColor,
            BulletType.Magenta => cfg.BulletMagentaOutlineColor,
            BulletType.Yellow => cfg.BulletYellowOutlineColor,
            BulletType.Cyan => cfg.BulletCyanOutlineColor,
            BulletType.Black => cfg.BulletBlackOutlineColor,
            _ => AssertHelper.FailWithDefault($"[BulletPalette 警告] 未知の BulletType (Outline): {type}", cfg.BulletNormalOutlineColor)
        };
    }

    /// <summary>
    ///     弾幕種別と色彩の整合性を検証・補正し、種別仕様に合致した塗りつぶし色および外枠色を取得します。
    /// </summary>
    public static (Color Fill, Color Outline) ResolveBulletColors(
        BulletType type,
        Color? customFill = null,
        Color? customOutline = null)
    {
        var hasFill = customFill != null && !customFill.Value.IsEmpty && customFill.Value.A > 0;
        var hasOutline = customOutline != null && !customOutline.Value.IsEmpty && customOutline.Value.A > 0;

        if (hasFill && hasOutline)
        {
            return (customFill!.Value, customOutline!.Value);
        }

        var defaultFill = GetDefaultFillColor(type);
        var defaultOutline = GetDefaultOutlineColor(type);

        var fill = hasFill ? customFill!.Value : defaultFill;
        var outline = hasOutline ? customOutline!.Value : defaultOutline;

        return (fill, outline);
    }
}
