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

        if (Path.IsPathRooted(relativePath) && File.Exists(relativePath)) return Path.GetFullPath(relativePath);

        try
        {
            string[] directRoots = [_baseDir, Environment.CurrentDirectory];
            foreach (var root in directRoots)
            {
                var found = FindAssetFile(root, relativePath);
                if (found != null) return found;
            }

            DirectoryInfo? dir = new(_baseDir);
            while (dir != null)
            {
                var found = FindAssetFile(dir.FullName, relativePath);
                if (found != null) return found;
                dir = dir.Parent;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[C# アセット警告] アセットパス探索中に例外が発生しました ({relativePath}): {ex.Message}");
        }

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