using AnimalBattle.Logging;
namespace AnimalBattle.Configs;

using System.Diagnostics;
using Flecs.NET.Core;
using Microsoft.Extensions.Configuration;

/// <summary>
/// <see cref="IConfigRegistry"/> の標準実装。
/// 各機能ドメインが自身のコンフィグを登録し、型安全に取得・管理するための汎用コンテナ。
/// </summary>
public sealed class ConfigRegistry : IConfigRegistry
{
    private readonly Dictionary<Type, IConfigHolder> _holders = new();
    private readonly List<IDisposable> _loaders = new();
    private bool _isDisposed;

    /// <inheritdoc />
    public ConfigHolder<T> RegisterToml<T>(
        string relativePath,
        Action<IConfiguration, T> binder,
        Action<World, T>? onReloaded = null) where T : class, new()
    {
        var type = typeof(T);
        if (_holders.TryGetValue(type, out var existing))
        {
            return (ConfigHolder<T>)existing;
        }

        var holder = new ConfigHolder<T>(new T());
        var loader = new TomlHotReloadLoader<T>(relativePath, holder, binder, onReloaded);
        var initial = loader.LoadInitial();
        if (!initial.IsError)
        {
            holder.Update(initial.Value);
        }
        else
        {
            Log.Error("ConfigRegistry", $"設定の読み込みに失敗しました ({relativePath}): {initial.FirstError.Description}");
            Debug.Fail($"設定の読み込みに失敗しました ({relativePath}): {initial.FirstError.Description}");
        }

        _holders[type] = holder;
        _loaders.Add(loader);
        return holder;
    }

    /// <inheritdoc />
    public ConfigHolder<T> RegisterInstance<T>(T instance) where T : class
    {
        Debug.Assert(instance != null, "登録対象の設定インスタンスは null にできません。");
        var type = typeof(T);
        if (_holders.TryGetValue(type, out var existing))
        {
            return (ConfigHolder<T>)existing;
        }

        var holder = new ConfigHolder<T>(instance);
        _holders[type] = holder;
        return holder;
    }

    /// <inheritdoc />
    public ConfigHolder<T> GetHolder<T>() where T : class
    {
        if (_holders.TryGetValue(typeof(T), out var holder))
        {
            return (ConfigHolder<T>)holder;
        }

        throw new KeyNotFoundException($"未登録の設定型です: {typeof(T).FullName}");
    }

    /// <inheritdoc />
    public T Get<T>() where T : class => GetHolder<T>().Value;

    /// <inheritdoc />
    public bool Contains<T>() where T : class => _holders.ContainsKey(typeof(T));

    /// <inheritdoc />
    public void InjectAllToWorld(World world)
    {
        Debug.Assert(world != null, "有効な Flecs ワールドが必要です。");
        foreach (var holder in _holders.Values)
        {
            holder.InjectToWorld(world);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        foreach (var loader in _loaders)
        {
            loader.Dispose();
        }
        _loaders.Clear();

        foreach (var holder in _holders.Values)
        {
            holder.Dispose();
        }
        _holders.Clear();
    }
}
