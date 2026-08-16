using System;
using System.Collections.Generic;
using Flecs.NET.Core;

namespace AnimalBattle.Plugins;
/// <summary>
/// Bevy エンジン (<c>bevy_app/app</c>) に準拠したモジュール・プラグイン統合アプリケーションコンテナ。
/// ドメインごとの ECS システム登録、リソース管理、およびプラグイン構築のライフサイクルを提供します。
/// </summary>
public class App
{
    private readonly World _world;
    private readonly Dictionary<Type, IPlugin> _plugins = [];
    private readonly Dictionary<Type, object> _resources = [];
    private bool _isBuilt;

    /// <summary>
    /// 管理対象の <see cref="World"/> インスタンスを取得します。
    /// </summary>
    public World World => _world;

    /// <summary>
    /// プラグイン群の構築が完了しているか判定します。
    /// </summary>
    public bool IsBuilt => _isBuilt;

    /// <summary>
    /// 指定された Flecs.NET World で <see cref="App"/> コンテナを初期化します。
    /// </summary>
    /// <param name="world">初期化対象の ECS ワールド</param>
    public App(World world)
    {
        _world = world;
    }

    /// <summary>
    /// 新しい単一プラグインを登録します。
    /// </summary>
    /// <typeparam name="T">登録するプラグイン型</typeparam>
    /// <param name="plugin">プラグインインスタンス</param>
    /// <returns>チェーン呼び出し用の <see cref="App"/> 自身</returns>
    public App AddPlugin<T>(T plugin) where T : IPlugin
    {
        Type type = typeof(T);
        if (!_plugins.ContainsKey(type))
        {
            _plugins[type] = plugin;
            plugin.Build(this);
        }
        return this;
    }

    /// <summary>
    /// 複数のプラグインを一括登録するプラグイングループを登録します。
    /// </summary>
    /// <param name="group">プラグイングループインスタンス</param>
    /// <returns>チェーン呼び出し用の <see cref="App"/> 自身</returns>
    public App AddPluginGroup(IPluginGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.Build(this);
        return this;
    }

    /// <summary>
    /// ECS システム（Action&lt;World&gt;）をワールドへ登録します。
    /// </summary>
    /// <param name="system">登録するシステムデリゲート</param>
    /// <returns>チェーン呼び出し用の <see cref="App"/> 自身</returns>
    public App AddSystem(Action<World> system)
    {
        ArgumentNullException.ThrowIfNull(system);
        system(_world);
        return this;
    }

    /// <summary>
    /// 型安全なクエリを持つ ECS システム（Action&lt;Query&lt;...&gt;&gt;）をワールドへ登録します。
    /// </summary>
    /// <typeparam name="TQuery">クエリ型</typeparam>
    /// <param name="system">登録するシステムデリゲート</param>
    /// <returns>チェーン呼び出し用の <see cref="App"/> 自身</returns>
    public App AddSystem<TQuery>(Action<Query<TQuery>> system) where TQuery : struct
    {
        ArgumentNullException.ThrowIfNull(system);
        Query<TQuery> query = _world.Query<TQuery>();
        system(query);
        return this;
    }

    /// <summary>
    /// アプリケーション共有リソースを登録します。
    /// </summary>
    /// <typeparam name="T">リソースの型</typeparam>
    /// <param name="resource">リソースインスタンス</param>
    /// <returns>チェーン呼び出し用の <see cref="App"/> 自身</returns>
    public App InsertResource<T>(T resource) where T : class
    {
        _resources[typeof(T)] = resource ?? throw new ArgumentNullException(nameof(resource));
        return this;
    }

    /// <summary>
    /// 登録されている共有リソースを取得します。
    /// </summary>
    /// <typeparam name="T">リソースの型</typeparam>
    /// <returns>リソースインスタンス（存在しない場合は null）</returns>
    public T? GetResource<T>() where T : class
    {
        return _resources.TryGetValue(typeof(T), out object? res) ? (T)res : null;
    }

    /// <summary>
    /// 全プラグインの初期構築（Build）完了後に呼び出される後処理フェーズを実行します。
    /// </summary>
    /// <returns>チェーン呼び出し用の <see cref="App"/> 自身</returns>
    public App FinishSetup()
    {
        if (!_isBuilt)
        {
            foreach (IPlugin plugin in _plugins.Values)
            {
                plugin.Finish(this);
            }
            _isBuilt = true;
        }
        return this;
    }

    /// <summary>
    /// アプリケーション終了時に全プラグインのクリーンアップ処理を実行します。
    /// </summary>
    public void Cleanup()
    {
        foreach (IPlugin plugin in _plugins.Values)
        {
            plugin.Cleanup(this);
        }
        _plugins.Clear();
        _resources.Clear();
    }
}
