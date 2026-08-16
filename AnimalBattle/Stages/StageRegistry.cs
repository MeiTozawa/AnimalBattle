using System;
using System.Collections.Generic;
using System.IO;
using AnimalBattle.Core;

namespace AnimalBattle.Stages;
/// <summary>
/// TOML 設定ファイル (<c>Config/stages.toml</c>) からマップおよび足場データを動的読み込み・パースし、
/// アクティブマップの管理を提供する静的レジストリクラス。
/// </summary>
public static class StageRegistry
{
    private static readonly List<StageInfo> _stages = [];
    private static int _currentStageIndex;

    /// <summary>全ステージリストを取得します。</summary>
    public static IReadOnlyList<StageInfo> Stages => _stages;

    /// <summary>現在アクティブなステージ情報を取得します。</summary>
    public static StageInfo CurrentStage
    {
        get
        {
            if (_stages.Count == 0)
            {
                LoadStagesFromToml();
            }
            return _stages[_currentStageIndex % Math.Max(1, _stages.Count)];
        }
    }

    /// <summary>現在のアクティブステージインデックス (0～3) を取得します。</summary>
    public static int CurrentStageIndex => _currentStageIndex;

    static StageRegistry()
    {
        LoadStagesFromToml();
    }

    /// <summary>
    /// 指定されたステージインデックス (0～3) にアクティブマップを切り替えます。
    /// </summary>
    /// <param name="index">ステージインデックス</param>
    public static void SetStageIndex(int index)
    {
        if (_stages.Count > 0)
        {
            _currentStageIndex = Math.Clamp(index, 0, _stages.Count - 1);
        }
    }

    /// <summary>
    /// 次のステージへ循環切り替えします。
    /// </summary>
    public static void NextStage()
    {
        if (_stages.Count > 0)
        {
            _currentStageIndex = (_currentStageIndex + 1) % _stages.Count;
        }
    }

    /// <summary>
    /// <c>Config/stages.toml</c> ファイルを探索・パースし、ステージリストを初期化します。
    /// </summary>
    public static void LoadStagesFromToml()
    {
        _stages.Clear();
        string? tomlPath = FindStagesTomlPath();

        if (tomlPath != null && File.Exists(tomlPath))
        {
            try
            {
                string[] lines = File.ReadAllLines(tomlPath);
                StageInfo? currentStage = null;
                PlatformConfig? currentPlat = null;

                foreach (string rawLine in lines)
                {
                    string line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                    {
                        continue;
                    }

                    if (line == "[[stages]]")
                    {
                        currentStage = new StageInfo();
                        _stages.Add(currentStage);
                        currentPlat = null;
                        continue;
                    }

                    if (line == "[[stages.platforms]]" && currentStage != null)
                    {
                        currentPlat = new PlatformConfig();
                        currentStage.Platforms.Add(currentPlat);
                        continue;
                    }

                    if (!line.Contains('='))
                    {
                        continue;
                    }
                    string[] parts = line.Split('=', 2);
                    string key = parts[0].Trim().ToLowerInvariant();
                    string val = parts[1].Trim();

                    if (currentPlat != null)
                    {
                        if (key == "x")
                        {
                            currentPlat.X = ParseFloat(val);
                        }
                        else if (key == "y")
                        {
                            currentPlat.Y = ParseFloat(val);
                        }
                        else if (key == "width")
                        {
                            currentPlat.Width = ParseFloat(val);
                        }
                        else if (key == "height")
                        {
                            currentPlat.Height = ParseFloat(val);
                        }
                        else if (key == "fill_color")
                        {
                            currentPlat.FillColor = ParseColor(val);
                        }
                        else if (key == "outline_color")
                        {
                            currentPlat.OutlineColor = ParseColor(val);
                        }
                    }
                    else if (currentStage != null)
                    {
                        if (key == "id")
                        {
                            currentStage.Id = (int)ParseFloat(val);
                        }
                        else if (key == "name")
                        {
                            currentStage.Name = CleanString(val);
                        }
                        else if (key == "subtitle")
                        {
                            currentStage.Subtitle = CleanString(val);
                        }
                        else if (key == "theme_color")
                        {
                            currentStage.ThemeColor = ParseColor(val);
                        }
                        else if (key == "bg_color")
                        {
                            currentStage.BgColor = ParseColor(val);
                        }
                    }
                }

                foreach (StageInfo st in _stages)
                {
                    if (st.Spawns.Length == 0)
                    {
                        st.Spawns =
                        [
                            new Point<float>(380.0f, 320.0f),
                            new Point<float>(540.0f, 450.0f),
                            new Point<float>(740.0f, 450.0f),
                            new Point<float>(900.0f, 320.0f)
                        ];
                    }
                }

                if (_stages.Count > 0)
                {
                    Console.WriteLine($"[TOML 読込成功] {tomlPath} から {_stages.Count} つの対戦マップをロードしました。");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TOML 読込警告] {tomlPath} のパース中にエラーが発生しました: {ex.Message}");
            }
        }

        Console.WriteLine("[TOML 読込] stages.toml が見つからないため、デフォルトマップを生成します。");
        CreateDefaultFallbackStage();
    }

    private static float ParseFloat(string val)
    {
        string clean = val.Trim().Trim('"', '\'');
        if (float.TryParse(clean, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float res))
        {
            return res;
        }

        return 0.0f;
    }

    private static string CleanString(string val)
    {
        return val.Trim().Trim('"', '\'');
    }

    private static uint ParseColor(object? colorObj)
    {
        if (colorObj == null)
        {
            return 0xFFFFFFFF;
        }
        string str = colorObj.ToString()?.Trim() ?? "";
        if (str.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return Convert.ToUInt32(str[2..], 16);
        }
        if (uint.TryParse(str, out uint result))
        {
            return result;
        }

        return 0xFFFFFFFF;
    }

    private static string? FindStagesTomlPath()
    {
        string[] candidates =
        [
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "stages.toml"),
            Path.Combine(Directory.GetCurrentDirectory(), "Config", "stages.toml"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "Config", "stages.toml"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "stages.toml"),
            Path.Combine(Directory.GetCurrentDirectory(), "stages.toml")
        ];

        foreach (string path in candidates)
        {
            if (File.Exists(path))
            {
                return Path.GetFullPath(path);
            }
        }

        return null;
    }

    private static void CreateDefaultFallbackStage()
    {
        _stages.Add(new StageInfo
        {
            Id = 1,
            Name = "Battlefield Citadel",
            Subtitle = "Fallback Default Map",
            ThemeColor = 0xFF00ADB5,
            BgColor = 0xFF121B24,
            Spawns =
            [
                new Point<float>(380.0f, 320.0f),
                new Point<float>(540.0f, 450.0f),
                new Point<float>(740.0f, 450.0f),
                new Point<float>(900.0f, 320.0f)
            ],
            Platforms =
            [
                new PlatformConfig { X = 640.0f, Y = 560.0f, Width = 760.0f, Height = 30.0f, FillColor = 0xFF1E5128, OutlineColor = 0xFF4E9F3D }
            ]
        });
    }
}
