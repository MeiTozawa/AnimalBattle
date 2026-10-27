using AnimalBattle.Logging;
using System.Diagnostics;

namespace AnimalBattle.Asset;

/// <summary>
///     実行ディレクトリおよびプロジェクトリソースフォルダからアセットパスを探索する標準リゾルバー。
/// </summary>
public sealed class AssetPathResolver(string? baseDirectory = null) : IAssetPathResolver {
    private readonly string _baseDir = baseDirectory ?? AppDomain.CurrentDomain.BaseDirectory;

    /// <summary>
    ///     標準のシングルトンインスタンスを取得します。
    /// </summary>
    public static AssetPathResolver Instance { get; } = new();

    /// <inheritdoc />
    public string ResolveAssetPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return relativePath;

        // 相対パス文字列の事前検証
        if (relativePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            Log.Warn("AssetPathResolver", $"[AssetPathResolver 警告] 不正な文字を含む相対パスです: {relativePath}");
            return relativePath;
        }

        if (Path.IsPathRooted(relativePath) && File.Exists(relativePath)) return Path.GetFullPath(relativePath);

        // 1. 直下リソース探索
        string[] directRoots = [_baseDir, Environment.CurrentDirectory];
        foreach (var root in directRoots)
        {
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;
            var found = FindAssetFile(root, relativePath);
            if (found != null) return found;
        }

        // 2. 親ディレクトリへの遡及探索
        if (Directory.Exists(_baseDir))
        {
            DirectoryInfo? dir = new(_baseDir);
            while (dir != null)
            {
                var found = FindAssetFile(dir.FullName, relativePath);
                if (found != null) return found;
                dir = dir.Parent;
            }
        }

        Log.Warn("AssetPathResolver", $"[AssetPathResolver 警告] リソースが見つからないため元の相対パスを返却します: {relativePath}");
        return relativePath;
    }

    private static string? FindAssetFile(string rootDir, string relPath)
    {
        var directResource = Path.Combine(rootDir, "Resource", relPath);
        if (File.Exists(directResource)) return Path.GetFullPath(directResource);

        var directCandidate = Path.Combine(rootDir, relPath);
        if (File.Exists(directCandidate)) return Path.GetFullPath(directCandidate);

        return null;
    }
}