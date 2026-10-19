namespace AnimalBattle.Configs;

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using Flecs.NET.Core;

/// <summary>
/// 全設定コンテナの読み込み、ライフサイクル管理、および Flecs.NET ワールドへの単例注入を統括するサービス。
/// </summary>
public sealed class ConfigService : IDisposable
{
    public ConfigHolder<CatConfig> CatHolder { get; }
    public ConfigHolder<BunnyConfig> BunnyHolder { get; }
    public ConfigHolder<CatPrefabConfig> CatPrefabHolder { get; }
    public ConfigHolder<BunnyPrefabConfig> BunnyPrefabHolder { get; }
    public ConfigHolder<Boss1Config> Boss1Holder { get; }
    public ConfigHolder<Boss2Config> Boss2Holder { get; }
    public ConfigHolder<Boss1PatternConfig> Boss1PatternsHolder { get; }
    public ConfigHolder<Boss2PatternConfig> Boss2PatternsHolder { get; }
    public ConfigHolder<Boss1PrefabConfig> Boss1PrefabHolder { get; }
    public ConfigHolder<Boss2PrefabConfig> Boss2PrefabHolder { get; }
    public ConfigHolder<SpecialBulletConfig> SpecialBulletsHolder { get; }
    public ConfigHolder<ArenaConfig> ArenaHolder { get; }
    public ConfigHolder<RenderConfig> RenderHolder { get; }
    public ConfigHolder<CameraConfig> CameraHolder { get; }
    public ConfigHolder<TileSetConfig> TileSetHolder { get; }
    public ConfigHolder<AudioConfig> AudioHolder { get; }
    public ConfigHolder<CombatPrefabConfig> CombatPrefabsHolder { get; }
    public ConfigHolder<StartSceneConfig> StartSceneHolder { get; }

    private readonly List<IDisposable> _loaders = new();
    private bool _isDisposed;

    public ConfigService()
    {
#if DEBUG
        TypeDescriptor.AddAttributes(typeof(Color), new TypeConverterAttribute(typeof(FlexibleColorConverter)));

        CatHolder = CreateTomlHolder<CatConfig>(ResourcePath.Config.Cat);
        BunnyHolder = CreateTomlHolder<BunnyConfig>(ResourcePath.Config.Bunny);
        CatPrefabHolder = CreateTomlHolder<CatPrefabConfig>(ResourcePath.Config.CatPrefab);
        BunnyPrefabHolder = CreateTomlHolder<BunnyPrefabConfig>(ResourcePath.Config.BunnyPrefab);
        Boss1Holder = CreateTomlHolder<Boss1Config>(ResourcePath.Config.Boss1);
        Boss2Holder = CreateTomlHolder<Boss2Config>(ResourcePath.Config.Boss2);
        Boss1PatternsHolder = CreateTomlHolder<Boss1PatternConfig>(ResourcePath.Config.Boss1Patterns);
        Boss2PatternsHolder = CreateTomlHolder<Boss2PatternConfig>(ResourcePath.Config.Boss2Patterns);
        Boss1PrefabHolder = CreateTomlHolder<Boss1PrefabConfig>(ResourcePath.Config.Boss1Prefab);
        Boss2PrefabHolder = CreateTomlHolder<Boss2PrefabConfig>(ResourcePath.Config.Boss2Prefab);
        SpecialBulletsHolder = CreateTomlHolder<SpecialBulletConfig>(ResourcePath.Config.SpecialBullets);
        ArenaHolder = CreateTomlHolder<ArenaConfig>(ResourcePath.Config.Arena);
        RenderHolder = CreateTomlHolder<RenderConfig>(ResourcePath.Config.Render);
        CameraHolder = CreateTomlHolder<CameraConfig>(ResourcePath.Config.Camera);
        TileSetHolder = CreateTomlHolder<TileSetConfig>(ResourcePath.Platform.SheetToml);
        AudioHolder = CreateTomlHolder<AudioConfig>(ResourcePath.Config.Audio);
        CombatPrefabsHolder = CreateTomlHolder<CombatPrefabConfig>(ResourcePath.Config.CombatPrefab);
        StartSceneHolder = CreateTomlHolder<StartSceneConfig>(ResourcePath.Config.StartScene);
#else
        var loadResult = BinaryConfigLoader.Load();
        Debug.Assert(!loadResult.IsError, $"Release バイナリ設定の読み込みに失敗しました: {loadResult.FirstError.Description}");
        var b = loadResult.Value;

        CatHolder = new(b.Cat);
        BunnyHolder = new(b.Bunny);
        CatPrefabHolder = new(b.CatPrefab);
        BunnyPrefabHolder = new(b.BunnyPrefab);
        Boss1Holder = new(b.Boss1);
        Boss2Holder = new(b.Boss2);
        Boss1PatternsHolder = new(b.Boss1Pattern);
        Boss2PatternsHolder = new(b.Boss2Pattern);
        Boss1PrefabHolder = new(b.Boss1Prefab);
        Boss2PrefabHolder = new(b.Boss2Prefab);
        SpecialBulletsHolder = new(b.SpecialBullet);
        ArenaHolder = new(b.Arena);
        RenderHolder = new(b.Render);
        CameraHolder = new(b.Camera);
        TileSetHolder = new(b.TileSet);
        AudioHolder = new(new AudioConfig());
        CombatPrefabsHolder = new(b.CombatPrefab);
        StartSceneHolder = new(b.StartScene ?? new StartSceneConfig());
#endif
    }

    /// <summary>
    /// 全設定スナップショットを Flecs.NET ワールドへ単例コンポーネントとして注入します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    public void Initialize(World world)
    {
        Debug.Assert(world != null, "有効な Flecs ワールドが必要です。");

        world.Set(CatHolder.Value);
        world.Set(BunnyHolder.Value);
        world.Set(CatPrefabHolder.Value);
        world.Set(BunnyPrefabHolder.Value);
        world.Set(Boss1Holder.Value);
        world.Set(Boss2Holder.Value);
        world.Set(Boss1PatternsHolder.Value);
        world.Set(Boss2PatternsHolder.Value);
        world.Set(Boss1PrefabHolder.Value);
        world.Set(Boss2PrefabHolder.Value);
        world.Set(SpecialBulletsHolder.Value);
        world.Set(ArenaHolder.Value);
        world.Set(RenderHolder.Value);
        world.Set(CameraHolder.Value);
        world.Set(TileSetHolder.Value);
        world.Set(AudioHolder.Value);
        world.Set(CombatPrefabsHolder.Value);
        world.Set(StartSceneHolder.Value);

        Debug.WriteLine("[C# 設定初期化] 全ての設定単例を Flecs ワールドへ正常に注入しました。");
    }

#if DEBUG
    private ConfigHolder<T> CreateTomlHolder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string path) where T : class, new()
    {
        var holder = new ConfigHolder<T>(new T());
        var loader = new TomlHotReloadLoader<T>(path, holder, onReloaded: (w, cfg) =>
        {
            w.Set(cfg);
        });

        var initial = loader.LoadInitial();
        if (!initial.IsError)
        {
            holder.Update(initial.Value);
        }
        else
        {
            Debug.Fail($"[C# 設定警告] {path} の初期読み込みに失敗しました: {initial.FirstError.Description}");
        }

        _loaders.Add(loader);
        return holder;
    }
#endif

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed) return;
        foreach (var loader in _loaders)
        {
            loader.Dispose();
        }
        _loaders.Clear();
        CatHolder.Dispose();
        BunnyHolder.Dispose();
        CatPrefabHolder.Dispose();
        BunnyPrefabHolder.Dispose();
        Boss1Holder.Dispose();
        Boss2Holder.Dispose();
        Boss1PatternsHolder.Dispose();
        Boss2PatternsHolder.Dispose();
        Boss1PrefabHolder.Dispose();
        Boss2PrefabHolder.Dispose();
        SpecialBulletsHolder.Dispose();
        ArenaHolder.Dispose();
        RenderHolder.Dispose();
        CameraHolder.Dispose();
        TileSetHolder.Dispose();
        AudioHolder.Dispose();
        CombatPrefabsHolder.Dispose();
        StartSceneHolder.Dispose();
        _isDisposed = true;
    }
}
