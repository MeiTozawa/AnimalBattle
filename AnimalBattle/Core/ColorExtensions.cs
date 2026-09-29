using System.Drawing;
using System.Globalization;
using ErrorOr;

namespace AnimalBattle.Core;

/// <summary>
///     <see cref="Color" />、<see cref="uint" /> (32 ビット ARGB)、および 16 進数・カラー名・数値文字列間の
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
    ///     <see cref="Color" /> を 32 ビット符号なし整数 (ARGB) へ明示的に変換します。
    /// </summary>
    /// <param name="color">対象の <see cref="Color" /></param>
    /// <returns>32 ビット符号なし ARGB 値 (<see cref="uint" />)</returns>
    public static uint ToUint(this Color color)
    {
        return (uint)color.ToArgb();
    }

    /// <summary>
    ///     16 進数文字列 ("0xAARRGGBB", "0xRRGGBB", "#AARRGGBB", "#RRGGBB")、10 進数値文字列、または標準色名文字列から
    ///     <see cref="Color" /> への厳格な解析を行います。
    /// </summary>
    /// <param name="colorString">変換対象のカラー文字列</param>
    /// <returns>成功時は <see cref="Color" />、失敗時は <see cref="Error" /></returns>
    public static ErrorOr<Color> ParseColor(this string? colorString)
    {
        if (string.IsNullOrWhiteSpace(colorString)) return Error.Validation("Color.Empty", "カラー文字列が null または空白です。");

        var trimmed = colorString.Trim();

        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("0X", StringComparison.OrdinalIgnoreCase))
        {
            var hex = trimmed[2..];
            if (uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb))
            {
                if (hex.Length <= 6) argb |= 0xFF000000;
                return Color.FromArgb((int)argb);
            }

            return Error.Validation("Color.InvalidHex", $"16 進数カラー文字列の解析に失敗しました: '{colorString}'");
        }

        if (trimmed.StartsWith('#'))
        {
            var hex = trimmed[1..];
            if (uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb))
            {
                if (hex.Length <= 6) argb |= 0xFF000000;
                return Color.FromArgb((int)argb);
            }

            return Error.Validation("Color.InvalidHashHex", $"'#' プレフィックスカラー文字列の解析に失敗しました: '{colorString}'");
        }

        if (uint.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var decArgb))
            return Color.FromArgb((int)decArgb);

        var named = Color.FromName(trimmed);
        if (named.IsKnownColor) return named;

        return Error.NotFound("Color.NotFound", $"未対応の色名またはカラー値です: '{colorString}'");
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
        var result = colorString.ParseColor();
        if (!result.IsError) return result.Value;

        if (!string.IsNullOrWhiteSpace(colorString))
            Console.WriteLine(
                $"[C# 描画警告] カラー文字列の解析に失敗しました ({result.FirstError.Description})。デフォルト色 ({fallback}) を使用します。");

        return fallback;
    }
}