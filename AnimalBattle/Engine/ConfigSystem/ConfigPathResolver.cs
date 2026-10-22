namespace AnimalBattle.Configs;

/// <summary>
/// 実行時ディレクトリおよびリソースディレクトリから設定パスを探索する標準リゾルバー。
/// </summary>
public sealed class ConfigPathResolver(string? baseDirectory = null) : IConfigPathResolver {
    private static readonly string[] _configSubDirs = ["Config", "Resource", ""];
    private readonly string _baseDir = baseDirectory ?? AppDomain.CurrentDomain.BaseDirectory;

    /// <summary>
    /// 標準のシングルトンインスタンスを取得します。
    /// </summary>
    public static ConfigPathResolver Instance { get; } = new();

    /// <inheritdoc />
    public string? ResolveConfigPath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;

        if (Path.IsPathRooted(fileName) && File.Exists(fileName)) return Path.GetFullPath(fileName);

        string[] directRoots = [_baseDir, Environment.CurrentDirectory];
        foreach (var root in directRoots)
        {
            var found = FindInSubDirectories(root, _configSubDirs, fileName);
            if (found != null) return found;
        }

        DirectoryInfo? dir = new(_baseDir);
        while (dir != null)
        {
            var found = FindInSubDirectories(dir.FullName, _configSubDirs, fileName);
            if (found != null) return found;
            dir = dir.Parent;
        }

        return null;
    }

    private static string? FindInSubDirectories(string rootDir, string[] subDirs, string fileName)
    {
        foreach (var subDir in subDirs)
        {
            var candidate = string.IsNullOrEmpty(subDir)
                ? Path.Combine(rootDir, fileName)
                : Path.Combine(rootDir, subDir, fileName);

            if (File.Exists(candidate)) return Path.GetFullPath(candidate);
        }

        return null;
    }
}