namespace AnimalBattle.Configs;

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using Flecs.NET.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Tomlyn.Extensions.Configuration;

/// <summary>
/// TOML 設定ファイルの読み込みおよびバックグラウンド変更監視を提供するローダー。
/// </summary>
/// <typeparam name="T">対象の設定クラスの型</typeparam>
public sealed class TomlHotReloadLoader<T> : IConfigLoader<T>
    where T : class, new()
{
    private const int DebounceDelayMs = 250;
    private const int MaxRetryCount = 5;
    private const int RetryDelayMs = 60;

    private readonly object _lock = new();
    private readonly string _relativeFilePath;
    private readonly ConfigHolder<T> _holder;
    private readonly Action<IConfiguration, T> _binder;
    private readonly Action<World, T>? _onReloaded;
    private readonly IConfigPathResolver _resolver;
    private bool _isDisposed;

#if DEBUG
    private FileSystemWatcher? _fileWatcher;
    private CancellationTokenSource? _debounceCts;
    private int _isReloading;
#endif

    /// <summary>
    /// <see cref="TomlHotReloadLoader{T}" /> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="relativeFilePath">設定ファイルの相対パスまたはファイル名</param>
    /// <param name="holder">更新対象のインメモリ設定コンテナ</param>
    /// <param name="binder">具象型に対応する AOT 互換設定バインドデリゲート</param>
    /// <param name="onReloaded">ホットリロード適用時にメインスレッドで実行する追加同期処理</param>
    /// <param name="resolver">パス解決リゾルバー（省略時は標準リゾルバー）</param>
    public TomlHotReloadLoader(
        string relativeFilePath,
        ConfigHolder<T> holder,
        Action<IConfiguration, T> binder,
        Action<World, T>? onReloaded = null,
        IConfigPathResolver? resolver = null)
    {
        Debug.Assert(!string.IsNullOrEmpty(relativeFilePath), "ファイルパスは空にできません。");
        Debug.Assert(holder != null, "ConfigHolder は null にできません。");
        Debug.Assert(binder != null, "Binder は null にできません。");

        _relativeFilePath = relativeFilePath;
        _holder = holder;
        _binder = binder;
        _onReloaded = onReloaded;
        _resolver = resolver ?? ConfigPathResolver.Instance;
    }

    /// <inheritdoc />
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
            // 外部エディタによる保存時の二重パースとファイルロック競合を防止するため、内部の自動リロードは無効化
            builder.SetBasePath(directory)
                   .AddTomlFile(fileName, optional: true, reloadOnChange: false);

            var configRoot = builder.Build();
            var instance = new T();
            _binder(configRoot, instance);

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
            SetupWatcher(directory, fileName, resolvedPath, configRoot);
#endif

            return instance;
        }
        catch (Exception ex)
        {
            Debug.Fail($"[C# 設定警告] TOML 読み込み中に例外が発生しました: {ex.Message}");
            return Error.Failure("Config.Exception", ex.Message);
        }
    }

#if DEBUG
    private void SetupWatcher(string directory, string fileName, string resolvedPath, IConfigurationRoot configRoot)
    {
        _fileWatcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
            EnableRaisingEvents = true
        };

        FileSystemEventHandler onEvent = (_, _) => TriggerDebouncedReload(configRoot, resolvedPath);
        _fileWatcher.Changed += onEvent;
        _fileWatcher.Created += onEvent;
        _fileWatcher.Renamed += (_, _) => TriggerDebouncedReload(configRoot, resolvedPath);
    }

    private void TriggerDebouncedReload(IConfigurationRoot configRoot, string resolvedPath)
    {
        // 内部リロード実行中の再帰的トリガーを防止
        if (Volatile.Read(ref _isReloading) != 0) return;

        CancellationTokenSource cts;
        lock (_lock)
        {
            if (_isDisposed) return;
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = new CancellationTokenSource();
            cts = _debounceCts;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                // エディタによる連続ファイル保存シーケンス（トランケート・書き込み・解放）を待機して集約
                await Task.Delay(DebounceDelayMs, cts.Token).ConfigureAwait(false);
                if (cts.Token.IsCancellationRequested) return;

                lock (_lock)
                {
                    if (_isDisposed) return;
                }

                await ReloadWithRetryAsync(configRoot, resolvedPath, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // 後続のファイル保存イベントにより安全にキャンセル
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[C# 設定警告] {typeof(T).Name} の非同期リロード中に予期せぬエラーが発生しました: {ex.Message}");
            }
        }, cts.Token);
    }

    private async Task ReloadWithRetryAsync(IConfigurationRoot configRoot, string resolvedPath, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxRetryCount; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // 1. ファイルの存在確認および排他ロック・一時的空ファイル（0バイト）の解消チェック
                if (!File.Exists(resolvedPath))
                {
                    throw new FileNotFoundException("設定ファイルが見つかりません。", resolvedPath);
                }

                await using (var stream = new FileStream(resolvedPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (stream.Length == 0)
                    {
                        throw new IOException("設定ファイルが一時的に 0 バイト（エディタによるトランケート中）です。");
                    }
                }

                // 2. configRoot の再読み込み（自らの RaiseChanged による再帰を防止）
                Interlocked.Exchange(ref _isReloading, 1);
                try
                {
                    configRoot.Reload();
                }
                finally
                {
                    Interlocked.Exchange(ref _isReloading, 0);
                }

                // 3. バインディングとライフサイクル検証
                var reloaded = new T();
                _binder(configRoot, reloaded);

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

                // 4. メインスレッドディスパッチ
                ConfigHotReloadDispatcher.Enqueue(world =>
                {
                    var updateResult = _holder.Update(reloaded);
                    if (!updateResult.IsError)
                    {
                        Debug.WriteLine($"[C# 設定] {typeof(T).Name} が自動リロードされました: {resolvedPath}");
                        _onReloaded?.Invoke(world, reloaded);
                    }
                });

                return;
            }
            catch (IOException ex) when (attempt < MaxRetryCount)
            {
                Debug.WriteLine($"[C# 設定情報] {typeof(T).Name} のファイルアクセス競合を検知 (試行 {attempt}/{MaxRetryCount}): {ex.Message}。待機して再試行します...");
                await Task.Delay(RetryDelayMs * attempt, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (attempt < MaxRetryCount && ex is not OperationCanceledException)
            {
                Debug.WriteLine($"[C# 設定情報] {typeof(T).Name} のリロード中間エラーを検知 (試行 {attempt}/{MaxRetryCount}): {ex.Message}。待機して再試行します...");
                await Task.Delay(RetryDelayMs * attempt, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // 最大試行回数を超過した場合はクラッシュさせず警告ログを出力し、次回ファイル保存時の再試行へフォールバック
                Debug.WriteLine($"[C# 設定警告] {typeof(T).Name} のリロードに失敗しました (次回保存時に再試行可能): {ex.Message}");
                return;
            }
        }
    }
#endif

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_lock)
        {
            if (_isDisposed) return;
            _isDisposed = true;

#if DEBUG
            _fileWatcher?.Dispose();
            _fileWatcher = null;
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = null;
#endif
        }
    }
}
