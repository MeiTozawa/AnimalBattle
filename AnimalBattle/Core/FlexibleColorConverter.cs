using System.ComponentModel;
using System.Drawing;
using System.Globalization;

namespace AnimalBattle.Core;

/// <summary>
///     16進数文字列 (0xRRGGBB, 0xAARRGGBB, #RRGGBB, #AARRGGBB)、符号なし 32 ビット整数 (uint)、および色名から
///     <see cref="Color" /> への自動変換をサポートする TypeConverter。
///     int や long の曖昧な変換は行わず、uint および string に限定して安全に処理します。
/// </summary>
public sealed class FlexibleColorConverter : ColorConverter {
    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) ||
            sourceType == typeof(uint) ||
            base.CanConvertFrom(context, sourceType);
    }

    /// <inheritdoc />
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string str) return str.ToColor();

        if (value is uint u) return u.ToColor();

        return base.ConvertFrom(context, culture, value);
    }
}