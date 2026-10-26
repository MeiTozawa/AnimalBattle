namespace AnimalBattle.Configs;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

/// <summary>
/// アセンブリ内に埋め込まれた TOML 設定リソースへのストリームアクセスを提供するプロバイダー。
/// </summary>
public static class EmbeddedConfigProvider
{
    private static readonly Assembly TargetAssembly = typeof(EmbeddedConfigProvider).Assembly;
    private static readonly Dictionary<string, string> NormalizedResourceMap = new(StringComparer.OrdinalIgnoreCase);

    static EmbeddedConfigProvider()
    {
        foreach (var name in TargetAssembly.GetManifestResourceNames())
        {
            NormalizedResourceMap[name] = name;
            var slashVariant = name.Replace('.', '/');
            NormalizedResourceMap[slashVariant] = name;

            if (name.StartsWith("Config.", StringComparison.OrdinalIgnoreCase))
            {
                var stripped = name["Config.".Length..];
                NormalizedResourceMap[stripped] = name;
                NormalizedResourceMap[stripped.Replace('.', '/')] = name;
            }
        }
    }

    /// <summary>
    /// 相対パスまたはファイル名に対応する埋め込みリソースの <see cref="Stream"/> を開きます。
    /// </summary>
    /// <param name="relativePath">ファイル名または相対パス（例: "cat.toml", "Config/cat.toml"）</param>
    /// <returns>読み取り可能なストリーム。見つからない場合は null。</returns>
    public static Stream? OpenStream(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return null;

        var normalizedKey = relativePath.Replace('\\', '/').TrimStart('/');
        var fileName = Path.GetFileName(relativePath);

        if (NormalizedResourceMap.TryGetValue(normalizedKey, out var manifestName) ||
            NormalizedResourceMap.TryGetValue($"Config/{normalizedKey}", out manifestName) ||
            NormalizedResourceMap.TryGetValue(fileName, out manifestName))
        {
            return TargetAssembly.GetManifestResourceStream(manifestName);
        }

        var dotKey = "Config." + normalizedKey.Replace('/', '.');
        if (NormalizedResourceMap.TryGetValue(dotKey, out manifestName))
        {
            return TargetAssembly.GetManifestResourceStream(manifestName);
        }

        return null;
    }

    /// <summary>
    /// 指定された相対パスまたはファイル名に対応する埋め込みリソースが存在するかどうかを判定します。
    /// </summary>
    /// <param name="relativePath">ファイル名または相対パス</param>
    /// <returns>存在する場合は true、それ以外は false。</returns>
    public static bool Exists(string relativePath)
    {
        using var stream = OpenStream(relativePath);
        return stream != null;
    }
}
