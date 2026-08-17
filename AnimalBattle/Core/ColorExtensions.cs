using System;
using System.Drawing;
using System.Globalization;

namespace AnimalBattle.Core;

/// <summary>
/// <see cref="Color"/>、<see cref="uint"/> (32 ビット ARGB)、および 16 進数・カラー名文字列間の
/// 明示的型変換を提供する拡張メソッドクラス。
/// <see cref="int"/> や <see cref="long"/> による曖昧な暗黙変換を排除し、厳格な型安全性を確保します。
/// </summary>
public static class ColorExtensions
{
    /// <summary>
    /// ARGB 値を表す <see cref="uint"/> から <see cref="Color"/> へ明示的に変換します。
    /// </summary>
    /// <param name="argb">32 ビット符号なし ARGB カラー値</param>
    /// <returns>変換された <see cref="Color"/></returns>
    public static Color ToColor(this uint argb)
    {
        return Color.FromArgb((int)argb);
    }

    /// <summary>
    /// <see cref="Color"/> を 32 ビット符号なし整数 (ARGB) へ明示的に変換します。
    /// </summary>
    /// <param name="color">対象の <see cref="Color"/></param>
    /// <returns>32 ビット符号なし ARGB 値 (<see cref="uint"/>)</returns>
    public static uint ToUint(this Color color)
    {
        return (uint)color.ToArgb();
    }

    /// <summary>
    /// 16 進数文字列 ("0xAARRGGBB", "0xRRGGBB", "#AARRGGBB", "#RRGGBB") または標準色名文字列から
    /// <see cref="Color"/> へ明示的に変換します。
    /// </summary>
    /// <param name="colorString">変換対象のカラー文字列</param>
    /// <param name="fallback">解析失敗時に返却するデフォルトカラー</param>
    /// <returns>変換された <see cref="Color"/>。失敗時は <paramref name="fallback"/></returns>
    public static Color ToColor(this string colorString, Color fallback = default)
    {
        if (string.IsNullOrWhiteSpace(colorString))
        {
            return fallback;
        }

        string trimmed = colorString.Trim();

        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("0X", StringComparison.OrdinalIgnoreCase))
        {
            string hex = trimmed[2..];
            if (uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint argb))
            {
                if (hex.Length <= 6)
                {
                    argb |= 0xFF000000;
                }
                return Color.FromArgb((int)argb);
            }
        }
        else if (trimmed.StartsWith('#'))
        {
            string hex = trimmed[1..];
            if (uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint argb))
            {
                if (hex.Length <= 6)
                {
                    argb |= 0xFF000000;
                }
                return Color.FromArgb((int)argb);
            }
        }
        else
        {
            Color named = Color.FromName(trimmed);
            if (named.IsKnownColor)
            {
                return named;
            }
        }

        return fallback;
    }
}
