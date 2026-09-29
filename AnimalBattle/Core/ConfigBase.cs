using System.Diagnostics.CodeAnalysis;
using AnimalBattle.Configs;
using ErrorOr;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Tomlyn.Extensions.Configuration;

namespace AnimalBattle.Core;

/// <summary>
///     TOML 設定ファイルからの動的読み込み、自動リロード（reloadOnChange）、およびプロパティ設定をサポートする設定コンテナクラス。
///     組合せ（コンポジション）パターンにより、設定 POCO オブジェクトのライフサイクルとイベントを管理します。
/// </summary>
/// <typeparam name="T">対象の設定クラスの型</typeparam>
/// <remarks>
///     指定された設定オブジェクトインスタンスで <see cref="ConfigBase{T}" /> の新しいインスタンスを初期化します。
/// </remarks>
/// <param name="config">設定オブジェクトインスタンス</param>
public class ConfigBase<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(T config)
    : IDisposable where T : class, new() {
    private IDisposable? _changeTokenSubscription;


    private bool _isDisposed;

    /// <summary>
    ///     デフォルト設定オブジェクトインスタンスで <see cref="ConfigBase{T}" /> の新しいインスタンスを初期化します。
    /// </summary>
    public ConfigBase() : this(new T())
    {
    }

    /// <summary>
    ///     管理対象の設定オブジェクトインスタンスを取得します。
    /// </summary>
    public T Config { get; private set; } = config;

    /// <inheritdoc />
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

    /// <summary>
    ///     設定ファイル変更にともない自動リロードが発生した際に発火するイベント。
    /// </summary>
#pragma warning disable CS0067
    public event Action? OnConfigReloaded;
#pragma warning restore CS0067

    /// <summary>
    ///     指定された TOML ファイルから設定を読み込み、自動リロード（reloadOnChange: true）を有効にした <see cref="ConfigBase{T}" /> インスタンスを生成します。
    ///     Release ビルド時は外部ファイルに依存せず、コード内にベイクされたデフォルト設定インスタンスを即座に返却します。
    /// </summary>
    /// <param name="defaultFileName">TOML ファイル名</param>
    /// <param name="resolver">探索に使用するパスリゾルバー（省略時は標準リゾルバー）。</param>
    /// <returns>成功時は設定コンテナインスタンスを含む <see cref="ErrorOr{T}" />、失敗時は <see cref="Error" /></returns>
    [UnconditionalSuppressMessage("Microsoft.Configuration.Binder", "SYSLIB1104",
        Justification = "Generic binder fallback is intentional in config container class.")]
    public static ErrorOr<ConfigBase<T>> LoadFromFile(string defaultFileName, IConfigPathResolver? resolver = null)
    {
#if !DEBUG
        ConfigBase<T> config = new(new T());
        return config;
#else
        resolver ??= ConfigPathResolver.Instance;
        var resolvedPath = resolver.ResolveConfigPath(defaultFileName);
        if (string.IsNullOrEmpty(resolvedPath) || !File.Exists(resolvedPath))
        {
            Console.WriteLine($"[C# 設定警告] TOML 設定ファイルが見つかりませんでした: {defaultFileName}");
            return Error.NotFound("Config.NotFound", $"TOML 設定ファイルが見つかりません ({defaultFileName})。");
        }

        var fullPath = resolvedPath;

        try
        {
            T configInstance = new();
            ConfigBase<T> container = new(configInstance);

            var directory = Path.GetDirectoryName(fullPath) ?? AppDomain.CurrentDomain.BaseDirectory;
            var fileName = Path.GetFileName(fullPath);

            ConfigurationBuilder builder = new();
            builder.SetBasePath(directory)
               .AddTomlFile(
                    fileName,
                    true,
                    true
                );

            var configRoot = builder.Build();
            configRoot.Bind(container.Config);

            container._changeTokenSubscription = ChangeToken.OnChange(
                configRoot.GetReloadToken,
                () =>
                {
                    try
                    {
                        T reloadedConfig = new();
                        configRoot.Bind(reloadedConfig);
                        container.Config = reloadedConfig;
                        Console.WriteLine(
                            $"[C# 設定] {typeof(T).Name} がファイル変更に伴い自動リロードされました (reloadOnChange: true): {fullPath}");
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
            return Error.Failure("Config.Exception", ex.Message);
        }
#endif
    }
}