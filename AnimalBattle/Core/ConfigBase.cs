using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Tomlyn.Extensions.Configuration;

namespace AnimalBattle.Core;

/// <summary>
/// TOML 設定ファイルからの動的読み込み、自動リロード（reloadOnChange）、およびプロパティ設定をサポートする抽象基底クラス。
/// </summary>
/// <typeparam name="T">派生する設定クラスの型</typeparam>
public abstract class ConfigBase<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> where T : ConfigBase<T>, new()
{
    private IDisposable? _changeTokenSubscription;

    /// <summary>
    /// 設定ファイル変更にともない自動リロードが発生した際に発火するイベント。
    /// </summary>
    public event Action? OnConfigReloaded;

    /// <summary>
    /// 指定された TOML ファイルから設定を読み込み、自動リロード（reloadOnChange: true）を有効にしたインスタンスを生成します。
    /// </summary>
    /// <param name="defaultFileName">TOML ファイル名</param>
    /// <returns>成功時は設定クラスインスタンスを含む <see cref="Result{T}"/>、失敗時は <see cref="SimpleError"/></returns>
    [UnconditionalSuppressMessage("Microsoft.Configuration.Binder", "SYSLIB1104", Justification = "Generic binder fallback is intentional in abstract base config class.")]
    public static Result<T> LoadFromFile(string defaultFileName)
    {
        string? fullPath = FindConfigPath(defaultFileName);
        if (fullPath == null || !File.Exists(fullPath))
        {
            return SimpleError.NotFound($"TOML 設定ファイルが見つかりません ({defaultFileName})。");
        }

        try
        {
            var config = new T();
            string directory = Path.GetDirectoryName(fullPath) ?? AppDomain.CurrentDomain.BaseDirectory;
            string fileName = Path.GetFileName(fullPath);

            var builder = new ConfigurationBuilder()
                .SetBasePath(directory)
                .AddTomlFile(fileName, optional: true, reloadOnChange: true);

            IConfigurationRoot configRoot = builder.Build();
            configRoot.Bind(config);

            config._changeTokenSubscription = ChangeToken.OnChange(
                configRoot.GetReloadToken,
                () =>
                {
                    try
                    {
                        configRoot.Bind(config);
                        Console.WriteLine($"[C# 設定] {typeof(T).Name} がファイル変更に伴い自動リロードされました (reloadOnChange: true): {fullPath}");
                        config.OnConfigReloaded?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[C# 設定] {typeof(T).Name} のリロード中にエラーが発生しました: {ex.Message}");
                    }
                });

            Console.WriteLine($"[C# 設定] TOML ファイルから {typeof(T).Name} を読み込みました (reloadOnChange: true): {fullPath}");
            return config;
        }
        catch (Exception ex)
        {
            return SimpleError.Exception(ex);
        }
    }

    /// <summary>
    /// 辞書データから設定プロパティを読み込み、数値を反映します。
    /// </summary>
    /// <param name="values">設定項目キーと数値のマップ</param>
    public virtual void LoadFromDictionary(IDictionary<string, float> values)
    {
        var properties = typeof(T).GetProperties();
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
