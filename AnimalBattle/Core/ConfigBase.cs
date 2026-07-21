using System;
using System.Collections.Generic;
using System.IO;
using Tomlyn;

namespace AnimalBattle.Core;

/// <summary>
/// TOML 設定ファイルからの動的読み込みおよびプロパティ設定をサポートする抽象基底クラス。
/// </summary>
/// <typeparam name="T">派生する設定クラスの型</typeparam>
public abstract class ConfigBase<T> where T : ConfigBase<T>, new()
{
    /// <summary>
    /// 指定された TOML ファイルから設定を読み込んだインスタンスを生成します。
    /// </summary>
    /// <param name="defaultFileName">TOML ファイル名</param>
    /// <returns>読み込まれた設定クラスのインスタンス</returns>
    public static T LoadFromFile(string defaultFileName)
    {
        var config = new T();
        string? fullPath = FindConfigPath(defaultFileName);
        if (fullPath != null && File.Exists(fullPath))
        {
            try
            {
                string tomlText = File.ReadAllText(fullPath);
                var dict = TomlSerializer.Deserialize<Dictionary<string, float>>(tomlText);
                if (dict != null)
                {
                    config.LoadFromDictionary(dict);
                }
                Console.WriteLine($"[C# 設定] TOML ファイルから {typeof(T).Name} を読み込みました: {fullPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[C# 設定] {typeof(T).Name} TOML 読み込みエラー ({fullPath}): {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"[C# 設定] 警告: TOML 設定ファイルが見つかりません ({defaultFileName})。デフォルト値を使用します。");
        }

        return config;
    }

    /// <summary>
    /// 辞書データから設定プロパティを読み込み、数値を反映します。
    /// </summary>
    /// <param name="values">設定項目キーと数値のマップ</param>
    public virtual void LoadFromDictionary(IDictionary<string, float> values)
    {
        var properties = GetType().GetProperties();
        foreach (var prop in properties)
        {
            if (prop.PropertyType != typeof(float) || !prop.CanWrite)
            {
                continue;
            }

            string name = prop.Name;
            string camelCase = char.ToLowerInvariant(name[0]) + name[1..];

            if (values.TryGetValue(camelCase, out float val) ||
                values.TryGetValue(name, out val) ||
                values.TryGetValue(name.ToLowerInvariant(), out val))
            {
                prop.SetValue(this, val);
            }
        }
    }

    /// <summary>
    /// 設定ファイルの検索パスを取得します。
    /// </summary>
    /// <param name="fileName">対象ファイル名</param>
    /// <returns>絶対パス。見つからない場合は null</returns>
    protected static string? FindConfigPath(string fileName)
    {
        try
        {
            if (File.Exists(fileName))
            {
                return Path.GetFullPath(fileName);
            }

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string fullPath = Path.GetFullPath(Path.Combine(baseDir, "..", "Config", fileName));
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }
        catch
        {
            // 例外無視
        }

        return null;
    }
}
