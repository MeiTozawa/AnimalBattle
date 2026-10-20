using System.Drawing;
using AnimalBattle.Core;

namespace AnimalBattle.Combat;

/// <summary>
///     弾幕種別に応じた既定カラーの解決および色彩整合性ポリシーを提供する静的クラスです。
/// </summary>
public static class BulletPalette
{
    /// <summary>
    ///     弾幕種別に応じた既定の塗りつぶしカラーを取得します。
    /// </summary>
    public static Color GetDefaultFillColor(BulletType type)
    {
        return type switch {
            BulletType.Normal => ColorPalette.Red,
            BulletType.Magenta => ColorPalette.BulletMagentaFill,
            BulletType.Yellow => ColorPalette.BulletYellowFill,
            BulletType.Cyan => ColorPalette.BulletCyanFill,
            BulletType.Black => ColorPalette.BulletBlackFill,
            _ => AssertHelper.FailWithDefault($"[BulletPalette 警告] 未知の BulletType (Fill): {type}", ColorPalette.RedBright)
        };
    }

    /// <summary>
    ///     弾幕種別に応じた既定の外枠カラーを取得します。
    /// </summary>
    public static Color GetDefaultOutlineColor(BulletType type)
    {
        return type switch {
            BulletType.Normal => ColorPalette.White,
            BulletType.Magenta => ColorPalette.BulletMagentaOutline,
            BulletType.Yellow => ColorPalette.BulletYellowOutline,
            BulletType.Cyan => ColorPalette.BulletCyanOutline,
            BulletType.Black => ColorPalette.BulletBlackOutline,
            _ => AssertHelper.FailWithDefault($"[BulletPalette 警告] 未知の BulletType (Outline): {type}", ColorPalette.White)
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
