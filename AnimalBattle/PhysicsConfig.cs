using System;
using System.Collections.Generic;
using System.IO;
using Tomlyn;

namespace AnimalBattle;

public static class PhysicsConfig
{
    private static readonly Dictionary<string, float> _values = [];
    private static DateTime _lastWriteTime = DateTime.MinValue;
    private static readonly string ConfigPath = "Config/physics.toml";

    public static float Get(string key, float defaultValue = 0.0f)
    {
        TryReload();
        return _values.TryGetValue(key, out float val) ? val : defaultValue;
    }

    private static void TryReload()
    {
        try
        {
            if (!File.Exists(ConfigPath)) return;

            DateTime currentWriteTime = File.GetLastWriteTimeUtc(ConfigPath);
            if (currentWriteTime == _lastWriteTime) return;

            _lastWriteTime = currentWriteTime;
            _values.Clear();

            string tomlText = File.ReadAllText(ConfigPath);
            var tomlModel = TomlSerializer.Deserialize<Dictionary<string, float>>(tomlText);

            if (tomlModel != null)
            {
                foreach (var kvp in tomlModel)
                {
                    _values[kvp.Key] = kvp.Value;
                }
            }

            Console.WriteLine($"[C# Config] Reloaded {ConfigPath} via TomlSerializer (Timestamp: {_lastWriteTime})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[C# Config] Error reading config: {ex.Message}");
        }
    }
}
