using System;
using System.Collections.Generic;
using System.IO;
using Tomlyn;

namespace AnimalBattle;

public static class PhysicsConfig
{
    private static readonly Dictionary<string, float> _values = [];
    private static DateTime _lastWriteTime = DateTime.MinValue;
    private static string? _resolvedPath = null;

    public static float Get(string key, float defaultValue = 0.0f)
    {
        TryReload();
        return _values.TryGetValue(key, out float val) ? val : defaultValue;
    }

    private static string? FindConfigPath()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string cwd = Directory.GetCurrentDirectory();

        string[] candidates =
        [
            Path.Combine(cwd, "Config", "physics.toml"),
            Path.Combine(cwd, "..", "Config", "physics.toml"),
            Path.Combine(cwd, "..", "..", "Config", "physics.toml"),
            Path.Combine(baseDir, "Config", "physics.toml"),
            Path.Combine(baseDir, "..", "Config", "physics.toml"),
            Path.Combine(baseDir, "..", "..", "Config", "physics.toml"),
            Path.Combine(baseDir, "..", "..", "..", "Config", "physics.toml"),
            Path.Combine(baseDir, "..", "..", "..", "..", "Config", "physics.toml")
        ];

        foreach (string candidate in candidates)
        {
            try
            {
                string fullPath = Path.GetFullPath(candidate);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
            catch
            {
                // Ignore invalid path format exceptions
            }
        }

        return null;
    }

    private static void TryReload()
    {
        try
        {
            if (_resolvedPath == null || !File.Exists(_resolvedPath))
            {
                _resolvedPath = FindConfigPath();
            }

            if (_resolvedPath == null || !File.Exists(_resolvedPath)) return;

            DateTime currentWriteTime = File.GetLastWriteTimeUtc(_resolvedPath);
            if (currentWriteTime == _lastWriteTime) return;

            _lastWriteTime = currentWriteTime;
            _values.Clear();

            string tomlText = File.ReadAllText(_resolvedPath);
            var tomlModel = TomlSerializer.Deserialize<Dictionary<string, float>>(tomlText);

            if (tomlModel != null)
            {
                foreach (var kvp in tomlModel)
                {
                    _values[kvp.Key] = kvp.Value;
                }
            }

            Console.WriteLine($"[C# Config] Reloaded {_resolvedPath} via TomlSerializer (Timestamp: {_lastWriteTime})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[C# Config] Error reading config: {ex.Message}");
        }
    }
}
