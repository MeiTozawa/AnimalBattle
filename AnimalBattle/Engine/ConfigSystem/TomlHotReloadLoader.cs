namespace AnimalBattle.Configs;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ErrorOr;
using Flecs.NET.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Tomlyn.Extensions.Configuration;

/// <summary>
/// TOML 設定ファイルの読み込みおよびバックグラウンド変更監視を提供するローダー。
/// </summary>
/// <typeparam name="T">対象の設定クラスの型</typeparam>
public sealed class TomlHotReloadLoader<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : IConfigLoader<T>
    where T : class, new()
{
    private readonly string _relativeFilePath;
    private readonly ConfigHolder<T> _holder;
    private readonly Action<World, T>? _onReloaded;
    private readonly IConfigPathResolver _resolver;
    private IDisposable? _changeTokenSubscription;
    private bool _isDisposed;

    /// <summary>
    /// <see cref="TomlHotReloadLoader{T}" /> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="relativeFilePath">設定ファイルの相対パスまたはファイル名</param>
    /// <param name="holder">更新対象のインメモリ設定コンテナ</param>
    /// <param name="onReloaded">ホットリロード適用時にメインスレッドで実行する追加同期処理</param>
    /// <param name="resolver">パス解決リゾルバー（省略時は標準リゾルバー）</param>
    public TomlHotReloadLoader(
        string relativeFilePath,
        ConfigHolder<T> holder,
        Action<World, T>? onReloaded = null,
        IConfigPathResolver? resolver = null)
    {
        Debug.Assert(!string.IsNullOrEmpty(relativeFilePath), "ファイルパスは空にできません。");
        Debug.Assert(holder != null, "ConfigHolder は null にできません。");

        _relativeFilePath = relativeFilePath;
        _holder = holder;
        _onReloaded = onReloaded;
        _resolver = resolver ?? ConfigPathResolver.Instance;
    }

    /// <inheritdoc />
    [UnconditionalSuppressMessage("Microsoft.Configuration.Binder", "SYSLIB1104",
        Justification = "Generic binder fallback is intentional in config container class.")]
    public ErrorOr<T> LoadInitial()
    {
        var resolvedPath = _resolver.ResolveConfigPath(_relativeFilePath);
        if (string.IsNullOrEmpty(resolvedPath) || !File.Exists(resolvedPath))
        {
            Debug.Fail($"[C# 設定警告] TOML 設定ファイルが見つかりませんでした: {_relativeFilePath}");
            return Error.NotFound("Config.NotFound", $"TOML 設定ファイルが見つかりません ({_relativeFilePath})。");
        }

        try
        {
            var directory = Path.GetDirectoryName(resolvedPath) ?? AppDomain.CurrentDomain.BaseDirectory;
            var fileName = Path.GetFileName(resolvedPath);

            var builder = new ConfigurationBuilder();
            builder.SetBasePath(directory)
                   .AddTomlFile(fileName, optional: true, reloadOnChange: true);

            var configRoot = builder.Build();
            var instance = new T();
            configRoot.Bind(instance);

            if (instance is IConfigLifecycle lifecycle)
            {
                var valid = lifecycle.Validate();
                if (valid.IsError)
                {
                    Debug.Fail($"[C# 設定警告] {typeof(T).Name} の初期検証に失敗しました: {valid.FirstError.Description}");
                    return valid.FirstError;
                }
                lifecycle.PostProcess();
            }

#if DEBUG
            _changeTokenSubscription = ChangeToken.OnChange(
                configRoot.GetReloadToken,
                () =>
                {
                    try
                    {
                        var reloaded = new T();
                        configRoot.Bind(reloaded);

                        if (reloaded is IConfigLifecycle reloadedLifecycle)
                        {
                            var valid = reloadedLifecycle.Validate();
                            if (valid.IsError)
                            {
                                Debug.WriteLine($"[C# 設定警告] {typeof(T).Name} のリロード検証に失敗したためスキップしました: {valid.FirstError.Description}");
                                return;
                            }
                            reloadedLifecycle.PostProcess();
                        }

                        ConfigHotReloadDispatcher.Enqueue(world =>
                        {
                            var updateResult = _holder.Update(reloaded);
                            if (!updateResult.IsError)
                            {
                                Debug.WriteLine($"[C# 設定] {typeof(T).Name} が自動リロードされました: {resolvedPath}");
                                _onReloaded?.Invoke(world, reloaded);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.Fail($"[C# 設定警告] {typeof(T).Name} のリロード中に例外が発生しました: {ex.Message}");
                    }
                });
#endif

            return instance;
        }
        catch (Exception ex)
        {
            Debug.Fail($"[C# 設定警告] TOML 読み込み中に例外が発生しました: {ex.Message}");
            return Error.Failure("Config.Exception", ex.Message);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed) return;
        _changeTokenSubscription?.Dispose();
        _changeTokenSubscription = null;
        _isDisposed = true;
    }
}
