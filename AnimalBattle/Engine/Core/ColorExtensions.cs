using AnimalBattle.Logging;
using System.Drawing;
using System.Globalization;

namespace AnimalBattle.Core;

/// <summary>
///     <see cref="Color" />、<see cref="uint" /> 32ビットARGB形式、および 16 進数・カラー名・数値文字列間の
///     明示的型変換を提供する拡張メソッドクラス。
/// </summary>
public static class ColorExtensions {
    /// <summary>
    ///     ARGB 値を表す <see cref="uint" /> から <see cref="Color" /> へ明示的に変換します。
    /// </summary>
    /// <param name="argb">32 ビット符号なし ARGB カラー値</param>
    /// <returns>変換された <see cref="Color" /></returns>
    public static Color ToColor(this uint argb)
    {
        return Color.FromArgb((int)argb);
    }

    /// <summary>
    ///     <see cref="Color" /> を 32 ビット符号なし整数 ARGB形式 へ明示的に変換します。
    /// </summary>
    /// <param name="color">対象の <see cref="Color" /></param>
    /// <returns>32 ビット符号なし ARGB 値 <see cref="uint" /></returns>
    public static uint ToUint(this Color color)
    {
        return (uint)color.ToArgb();
    }


    /// <summary>
    ///     カラー文字列から Color への解析を試行します。
    /// </summary>
    /// <param name="colorString">変換対象のカラー文字列。</param>
    /// <param name="color">解析された Color。</param>
    /// <returns>解析成功時は true、失敗時は false。</returns>
    public static bool TryParseColor(this string? colorString, out Color color)
    {
        if (string.IsNullOrWhiteSpace(colorString))
        {
            color = default;
            return false;
        }

        var trimmed = colorString.Trim();

        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("0X", StringComparison.OrdinalIgnoreCase))
        {
            var hex = trimmed[2..];
            if (uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb))
            {
                if (hex.Length <= 6) argb |= 0xFF000000;
                color = Color.FromArgb((int)argb);
                return true;
            }

            color = default;
            return false;
        }

        if (trimmed.StartsWith('#'))
        {
            var hex = trimmed[1..];
            if (uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb))
            {
                if (hex.Length <= 6) argb |= 0xFF000000;
                color = Color.FromArgb((int)argb);
                return true;
            }

            color = default;
            return false;
        }

        if (uint.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var decArgb))
        {
            color = Color.FromArgb((int)decArgb);
            return true;
        }

        var named = Color.FromName(trimmed);
        if (named.IsKnownColor)
        {
            color = named;
            return true;
        }

        color = default;
        return false;
    }

    /// <summary>
    ///     16 進数文字列、10 進数値文字列、または標準色名文字列から <see cref="Color" /> へ変換します。
    ///     解析に失敗した場合は警告を出力し、指定されたデフォルトカラーを返します。
    /// </summary>
    /// <param name="colorString">変換対象のカラー文字列</param>
    /// <param name="fallback">解析失敗時に返却するデフォルトカラー</param>
    /// <returns>変換された <see cref="Color" />。失敗時は <paramref name="fallback" /></returns>
    public static Color ToColor(this string? colorString, Color fallback = default)
    {
        if (colorString.TryParseColor(out var color)) return color;

        if (!string.IsNullOrWhiteSpace(colorString))
        {
            Log.Warn("ColorExtensions", $"カラー文字列の解析に失敗しました: '{colorString}'。デフォルト色を使用します。");
            Debug.Fail($"[ColorExtensions 警告] カラー文字列の解析に失敗しました: '{colorString}'。デフォルト色を使用します。");
        }

        return fallback;
    }
}