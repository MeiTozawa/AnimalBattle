using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Boss2;

namespace AnimalBattle.Configs;

/// <summary>
///     各種設定（player.toml, boss1.toml, boss2.toml, patterns.toml, arena.toml, render.toml, sheet.toml）を統括管理し、
///     Debug ビルド時の動的自動リロードおよび Release ビルド時のバイナリ高速ロードを提供する静的設定マネージャークラス。
/// </summary>
public static class ConfigManager {
    private static ConfigBase<PlayerConfig> _playerConfigContainer = new(new PlayerConfig());
    private static ConfigBase<Boss1Config> _boss1ConfigContainer = new(new Boss1Config());
    private static ConfigBase<Boss2Config> _boss2ConfigContainer = new(new Boss2Config());
    private static ConfigBase<PatternConfig> _patternConfigContainer = new(new PatternConfig());
    private static ConfigBase<ArenaConfig> _arenaConfigContainer = new(new ArenaConfig());
    private static ConfigBase<RenderConfig> _renderConfigContainer = new(new RenderConfig());
    private static ConfigBase<TileSetConfig> _tileSetConfigContainer = new(new TileSetConfig());
    private static bool _isInitialized;

    static ConfigManager()
    {
        Initialize();
    }

    /// <summary>プレイヤー設定オブジェクトを取得します。</summary>
    public static PlayerConfig Player => _playerConfigContainer.Config;

    /// <summary>Boss 1 (Ancient Cat Overlord) 設定オブジェクトを取得します。</summary>
    public static Boss1Config Boss1 => _boss1ConfigContainer.Config;

    /// <summary>Boss 2 (Ground Crusher) 設定オブジェクトを取得します。</summary>
    public static Boss2Config Boss2 => _boss2ConfigContainer.Config;

    /// <summary>弾幕・技能パターン設定オブジェクトを取得します。</summary>
    public static PatternConfig Pattern => _patternConfigContainer.Config;

    /// <summary>アリーナ足場設定オブジェクトを取得します。</summary>
    public static ArenaConfig Arena => _arenaConfigContainer.Config;

    /// <summary>描画および UI 設定オブジェクトを取得します。</summary>
    public static RenderConfig Render => _renderConfigContainer.Config;

    /// <summary>タイルセット足場設定オブジェクトを取得します。</summary>
    public static TileSetConfig TileSet => _tileSetConfigContainer.Config;

    /// <summary>プレイヤー設定コンテナを取得します。</summary>
    public static ConfigBase<PlayerConfig> PlayerContainer => _playerConfigContainer;

    /// <summary>Boss 1 設定コンテナを取得します。</summary>
    public static ConfigBase<Boss1Config> Boss1Container => _boss1ConfigContainer;

    /// <summary>Boss 2 設定コンテナを取得します。</summary>
    public static ConfigBase<Boss2Config> Boss2Container => _boss2ConfigContainer;

    /// <summary>弾幕・技能パターン設定コンテナを取得します。</summary>
    public static ConfigBase<PatternConfig> PatternContainer => _patternConfigContainer;

    /// <summary>アリーナ足場設定コンテナを取得します。</summary>
    public static ConfigBase<ArenaConfig> ArenaContainer => _arenaConfigContainer;

    /// <summary>描画設定コンテナを取得します。</summary>
    public static ConfigBase<RenderConfig> RenderContainer => _renderConfigContainer;

    /// <summary>タイルセット設定コンテナを取得します。</summary>
    public static ConfigBase<TileSetConfig> TileSetContainer => _tileSetConfigContainer;

    /// <summary>
    ///     全設定を初期化します。
    ///     Debug ビルド時は TOML ファイル群を動的ロードしてホットリロード監視を開始します。
    ///     Release ビルド時はコンパイル時にベイクされたバイナリ設定 (baked_configs.bin) をロードします。
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized) return;

        _isInitialized = true;

#if DEBUG
        TypeDescriptor.AddAttributes(typeof(Color), new TypeConverterAttribute(typeof(FlexibleColorConverter)));

        var successCount = 0;
        var failureCount = 0;

        LoadConfig(ResourcePath.Config.Player, ref _playerConfigContainer, ref successCount, ref failureCount);
        LoadConfig(ResourcePath.Config.Boss1, ref _boss1ConfigContainer, ref successCount, ref failureCount);
        LoadConfig(ResourcePath.Config.Boss2, ref _boss2ConfigContainer, ref successCount, ref failureCount);
        LoadConfig(ResourcePath.Config.Patterns, ref _patternConfigContainer, ref successCount, ref failureCount);
        LoadConfig(ResourcePath.Config.Arena, ref _arenaConfigContainer, ref successCount, ref failureCount);
        LoadConfig(ResourcePath.Config.Render, ref _renderConfigContainer, ref successCount, ref failureCount);
        LoadConfig(ResourcePath.Platform.SheetToml, ref _tileSetConfigContainer, ref successCount, ref failureCount);

        Debug.Assert(failureCount == 0, "一部の TOML 設定ファイルの読み込みに失敗しました。");

        if (failureCount > 0)
        {
            Console.WriteLine($"[C# 設定警告] TOML 設定読み込み完了 (成功: {successCount}, 失敗: {failureCount})。");
            return;
        }

        Console.WriteLine($"[C# 設定初期化] 全ての TOML 設定ファイル（{successCount} 件）の読み込みが正常に完了しました！");

        _boss1ConfigContainer.OnConfigReloaded += Boss1PipelineDefinition.Reload;
        _boss2ConfigContainer.OnConfigReloaded += Boss2PipelineDefinition.Reload;
        _patternConfigContainer.OnConfigReloaded += () =>
        {
            Boss1PipelineDefinition.Reload();
            Boss2PipelineDefinition.Reload();
        };
#else
        ErrorOr<BakedConfigBundle> loadResult = BinaryConfigLoader.Load();
        if (loadResult.IsError)
        {
            Console.WriteLine($"[C# 設定エラー] Release 版バイナリ設定 (baked_configs.bin) の読み込みに失敗しました: {loadResult.FirstError.Description}");
            return;
        }

        BakedConfigBundle bundle = loadResult.Value;
        _playerConfigContainer = new(bundle.Player);
        _boss1ConfigContainer = new(bundle.Boss1);
        _boss2ConfigContainer = new(bundle.Boss2);
        _patternConfigContainer = new(bundle.Pattern);
        _arenaConfigContainer = new(bundle.Arena);
        _renderConfigContainer = new(bundle.Render);
        _tileSetConfigContainer = new(bundle.TileSet);

        Console.WriteLine("[C# 設定初期化] Release 版: バイナリ設定データ (baked_configs.bin) を正常にロードして起動しました。");
#endif

        Boss1PipelineDefinition.Reload();
        Boss2PipelineDefinition.Reload();
    }

#if DEBUG
    private static void LoadConfig<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        string configPath,
        ref ConfigBase<T> container,
        ref int successCount,
        ref int failureCount) where T : class, new()
    {
        var res = ConfigBase<T>.LoadFromFile(configPath);
        if (!res.IsError && res.Value != null)
        {
            container = res.Value;
            successCount++;
        }
        else
        {
            Console.WriteLine(
                $"[C# 設定警告] {configPath} の読み込みに失敗しました ({res.FirstError.Type}): {res.FirstError.Description}");
            failureCount++;
        }
    }
#endif

    /// <summary>
    ///     全設定コンテナのファイル監視リソースを安全に解放します。
    /// </summary>
    public static void Dispose()
    {
        _playerConfigContainer.Dispose();
        _boss1ConfigContainer.Dispose();
        _boss2ConfigContainer.Dispose();
        _patternConfigContainer.Dispose();
        _arenaConfigContainer.Dispose();
        _renderConfigContainer.Dispose();
        _tileSetConfigContainer.Dispose();
    }
}