using System;
using System.Collections.Generic;
using System.IO;
using Tomlyn;

namespace AnimalBattle;

/// <summary>
/// 外部設定ファイル (Config/physics.toml) から物理パラメータを動的に読み込み・リロードする静的クラス。
/// </summary>
public static class PhysicsConfig
{
    private static readonly Dictionary<string, float> _values = [];
    private static DateTime _lastWriteTime = DateTime.MinValue;
    private static string? _resolvedPath = null;

    /// <summary>
    /// 指定されたキーの物理設定値を取得します。設定ファイルが存在しない場合はデフォルト値を返します。
    /// </summary>
    /// <param name="key">設定パラメータ名</param>
    /// <param name="defaultValue">デフォルト値</param>
    /// <return>パラメータ値</return>
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
                // 無効なパスフォーマット例外を無視
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

            if (_resolvedPath == null || !File.Exists(_resolvedPath))
            {
                return;
            }

            DateTime currentWriteTime = File.GetLastWriteTimeUtc(_resolvedPath);
            if (currentWriteTime == _lastWriteTime)
            {
                return;
            }

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

            Console.WriteLine($"[C# 設定] {_resolvedPath} を再読み込みしました (タイムスタンプ: {_lastWriteTime})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[C# 設定] 設定ファイル読み込みエラー: {ex.Message}");
        }
    }
}
