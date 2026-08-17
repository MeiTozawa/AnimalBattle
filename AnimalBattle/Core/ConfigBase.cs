using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Tomlyn.Extensions.Configuration;

namespace AnimalBattle.Core;
/// <summary>
/// TOML 設定ファイルからの動的読み込み、自動リロード（reloadOnChange）、およびプロパティ設定をサポートする設定コンテナクラス。
/// 組合せ（コンポジション）パターンにより、設定 POCO オブジェクトのライフサイクルとイベントを管理します。
/// </summary>
/// <typeparam name="T">対象の設定クラスの型</typeparam>
/// <remarks>
/// 指定された設定オブジェクトインスタンスで <see cref="ConfigBase{T}"/> の新しいインスタンスを初期化します。
/// </remarks>
/// <param name="config">設定オブジェクトインスタンス</param>
public class ConfigBase<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(T config) : IDisposable where T : class, new()
{
    private IDisposable? _changeTokenSubscription;

    /// <summary>
    /// 管理対象の設定オブジェクトインスタンスを取得します。
    /// </summary>
    public T Config { get; } = config;

    /// <summary>
    /// 設定ファイル変更にともない自動リロードが発生した際に発火するイベント。
    /// </summary>
    public event Action? OnConfigReloaded;

    /// <summary>
    /// デフォルト設定オブジェクトインスタンスで <see cref="ConfigBase{T}"/> の新しいインスタンスを初期化します。
    /// </summary>
    public ConfigBase() : this(new T())
    {
    }

    /// <summary>
    /// 指定された TOML ファイルから設定を読み込み、自動リロード（reloadOnChange: true）を有効にした <see cref="ConfigBase{T}"/> インスタンスを生成します。
    /// </summary>
    /// <param name="defaultFileName">TOML ファイル名</param>
    /// <returns>成功時は設定コンテナインスタンスを含む <see cref="Result{T}"/>、失敗時は <see cref="SimpleError"/></returns>
    [UnconditionalSuppressMessage("Microsoft.Configuration.Binder", "SYSLIB1104", Justification = "Generic binder fallback is intentional in config container class.")]
    public static Result<ConfigBase<T>> LoadFromFile(string defaultFileName)
    {
        string? fullPath = FindConfigPath(defaultFileName);
        if (fullPath == null || !File.Exists(fullPath))
        {
            Console.WriteLine($"[C# 設定警告] TOML 設定ファイルが見つかりませんでした: {defaultFileName}");
            return SimpleError.NotFound($"TOML 設定ファイルが見つかりません ({defaultFileName})。");
        }

        try
        {
            T configInstance = new T();
            ConfigBase<T> container = new ConfigBase<T>(configInstance);

            string directory = Path.GetDirectoryName(fullPath) ?? AppDomain.CurrentDomain.BaseDirectory;
            string fileName = Path.GetFileName(fullPath);

            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(directory)
                .AddTomlFile(
                    fileName,
                    optional: true,
#if DEBUG
                    reloadOnChange: true
#endif
                );

            IConfigurationRoot configRoot = builder.Build();
            configRoot.Bind(container.Config);

            container._changeTokenSubscription = ChangeToken.OnChange(
                configRoot.GetReloadToken,
                () =>
                {
                    try
                    {
                        configRoot.Bind(container.Config);
                        Console.WriteLine($"[C# 設定] {typeof(T).Name} がファイル変更に伴い自動リロードされました (reloadOnChange: true): {fullPath}");
                        container.OnConfigReloaded?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[C# 設定] {typeof(T).Name} のリロード中にエラーが発生しました: {ex.Message}");
                    }
                });

            Console.WriteLine($"[C# 設定] TOML ファイルから {typeof(T).Name} を読み込みました (reloadOnChange: true): {fullPath}");
            return container;
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
    public void LoadFromDictionary(IDictionary<string, float> values)
    {
        PropertyInfo[] properties = typeof(T).GetProperties();
        foreach (PropertyInfo prop in properties)
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
                prop.SetValue(Config, val);
            }
        }
    }

    /// <summary>
    /// 設定ファイルの検索パスを取得します。
    /// カレントディレクトリ、出力ディレクトリ、およびプロジェクトルートの Config フォルダを優先的に探索します。
    /// </summary>
    /// <param name="fileName">対象ファイル名</param>
    /// <returns>絶対パス。見つからない場合は null</returns>
    private static string? FindConfigPath(string fileName)
    {
        try
        {
            string directConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", fileName);
            if (File.Exists(directConfig))
            {
                return Path.GetFullPath(directConfig);
            }

            string cwdConfig = Path.Combine(Environment.CurrentDirectory, "Config", fileName);
            if (File.Exists(cwdConfig))
            {
                return Path.GetFullPath(cwdConfig);
            }

            DirectoryInfo? dir = new(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null)
            {
                string candidate = Path.Combine(dir.FullName, "Config", fileName);
                if (File.Exists(candidate))
                {
                    return Path.GetFullPath(candidate);
                }

                dir = dir.Parent;
            }
        }
        catch
        {
        }

        return null;
    }

    private bool _isDisposed;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _changeTokenSubscription?.Dispose();
            _changeTokenSubscription = null;
            _isDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
