using System.Reflection;

namespace AnimalBattle.Configs;

/// <summary>
///     実行時ディレクトリおよびリソースディレクトリから設定パスを探索する標準リゾルバー。
/// </summary>
public sealed class ConfigPathResolver(string? baseDirectory = null) : IConfigPathResolver {
    private static readonly string[] _configSubDirs = ["Config", "Resource", ""];
    private readonly string _baseDir = baseDirectory ?? AppDomain.CurrentDomain.BaseDirectory;

    /// <summary>
    ///     標準のシングルトンインスタンスを取得します。
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

    /// <inheritdoc />
    public Stream? OpenBinaryStream(string? overridePath = null)
    {
        if (!string.IsNullOrEmpty(overridePath) && File.Exists(overridePath)) return File.OpenRead(overridePath);

        string[] searchPaths = [
            Path.Combine(_baseDir, "Resource", "baked_configs.bin"),
            Path.Combine(_baseDir, "baked_configs.bin"),
            Path.Combine(Environment.CurrentDirectory, "Resource", "baked_configs.bin"),
            Path.Combine(Environment.CurrentDirectory, "baked_configs.bin")
        ];

        foreach (var path in searchPaths)
            if (File.Exists(path))
                return File.OpenRead(path);

        DirectoryInfo? dir = new(_baseDir);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Resource", "baked_configs.bin");
            if (File.Exists(candidate)) return File.OpenRead(candidate);
            dir = dir.Parent;
        }

        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceStream("baked_configs.bin")
         ?? assembly.GetManifestResourceStream("AnimalBattle.Resource.baked_configs.bin")
         ?? assembly.GetManifestResourceStream("AnimalBattle.baked_configs.bin");
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