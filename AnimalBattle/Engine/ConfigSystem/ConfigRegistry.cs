namespace AnimalBattle.Configs;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using AnimalBattle.Logging;
using Flecs.NET.Core;
using Microsoft.Extensions.Configuration;

/// <summary>
///     <see cref="IConfigRegistry"/> の標準実装。
///     各機能ドメインが自身のコンフィグを登録し、型安全に取得・管理するための汎用コンテナ。
/// </summary>
public sealed class ConfigRegistry : IConfigRegistry
{
    private readonly Dictionary<string, IConfigHolder> _holdersByPath = new();
    private readonly Dictionary<Type, IConfigHolder> _typeHolders = new();
    private readonly List<IConfigHolder> _injectableHolders = new();
    private readonly List<IDisposable> _loaders = new();
    private bool _isDisposed;

    /// <inheritdoc />
    public ConfigHolder<T> RegisterToml<T>(
        string relativePath,
        Action<IConfiguration, T> binder,
        Action<World, T>? onReloaded = null,
        bool injectToWorld = true) where T : class, new()
    {
        if (_holdersByPath.TryGetValue(relativePath, out var existing))
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

        _holdersByPath[relativePath] = holder;
        _loaders.Add(loader);

        if (injectToWorld)
        {
            _typeHolders[typeof(T)] = holder;
            _injectableHolders.Add(holder);
        }

        return holder;
    }

    /// <inheritdoc />
    public ConfigHolder<T> GetHolder<T>() where T : class
    {
        if (_typeHolders.TryGetValue(typeof(T), out var holder))
        {
            return (ConfigHolder<T>)holder;
        }

        throw new KeyNotFoundException($"未登録の設定型です: {typeof(T).FullName}");
    }

    /// <inheritdoc />
    public T Get<T>() where T : class => GetHolder<T>().Value;

    /// <inheritdoc />
    public bool Contains<T>() where T : class => _typeHolders.ContainsKey(typeof(T));

    /// <inheritdoc />
    public void InjectAllToWorld(World world)
    {
        Debug.Assert(world != null, "有効な Flecs ワールドが必要です。");
        foreach (var holder in _injectableHolders)
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

        foreach (var holder in _holdersByPath.Values)
        {
            holder.Dispose();
        }
        _holdersByPath.Clear();
        _typeHolders.Clear();
        _injectableHolders.Clear();
    }
}
