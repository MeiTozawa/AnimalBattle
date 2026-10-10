using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Boss2;
using ErrorOr;

namespace AnimalBattle.Configs;

/// <summary>
///     各種設定を統括管理し、
///     Debug ビルド時の動的自動リロードおよび Release ビルド時のバイナリ高速ロードを提供する静的設定マネージャークラス。
/// </summary>
public static class ConfigManager {
    private static ConfigBase<PlayerConfig> _playerConfigContainer = new(new PlayerConfig());
    private static ConfigBase<Boss1Config> _boss1ConfigContainer = new(new Boss1Config());
    private static ConfigBase<Boss2Config> _boss2ConfigContainer = new(new Boss2Config());
    private static ConfigBase<Boss1PatternConfig> _boss1PatternsContainer = new(new Boss1PatternConfig());
    private static ConfigBase<Boss2PatternConfig> _boss2PatternsContainer = new(new Boss2PatternConfig());
    private static ConfigBase<SpecialBulletConfig> _specialBulletsContainer = new(new SpecialBulletConfig());
    private static ConfigBase<ArenaConfig> _arenaConfigContainer = new(new ArenaConfig());
    private static ConfigBase<RenderConfig> _renderConfigContainer = new(new RenderConfig());
    private static ConfigBase<CameraConfig> _cameraConfigContainer = new(new CameraConfig());
    private static ConfigBase<TileSetConfig> _tileSetConfigContainer = new(new TileSetConfig());
    private static ConfigBase<AudioConfig> _audioConfigContainer = new(new AudioConfig());
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

    /// <summary>Boss 1 弾幕・技能パターン設定オブジェクトを取得します。</summary>
    public static Boss1PatternConfig Boss1Patterns => _boss1PatternsContainer.Config;

    /// <summary>Boss 2 近接・技能パターン設定オブジェクトを取得します。</summary>
    public static Boss2PatternConfig Boss2Patterns => _boss2PatternsContainer.Config;

    /// <summary>特殊弾幕変調設定オブジェクトを取得します。</summary>
    public static SpecialBulletConfig SpecialBullets => _specialBulletsContainer.Config;

    /// <summary>アリーナ足場設定オブジェクトを取得します。</summary>
    public static ArenaConfig Arena => _arenaConfigContainer.Config;

    /// <summary>描画および UI 設定オブジェクトを取得します。</summary>
    public static RenderConfig Render => _renderConfigContainer.Config;

    /// <summary>カメラおよび画面振動設定オブジェクトを取得します。</summary>
    public static CameraConfig Camera => _cameraConfigContainer.Config;

    /// <summary>タイルセット足場設定オブジェクトを取得します。</summary>
    public static TileSetConfig TileSet => _tileSetConfigContainer.Config;

    /// <summary>音響および音量設定オブジェクトを取得します。</summary>
    public static AudioConfig Audio => _audioConfigContainer.Config;

    /// <summary>プレイヤー設定コンテナを取得します。</summary>
    public static ConfigBase<PlayerConfig> PlayerContainer => _playerConfigContainer;

    /// <summary>Boss 1 設定コンテナを取得します。</summary>
    public static ConfigBase<Boss1Config> Boss1Container => _boss1ConfigContainer;

    /// <summary>Boss 2 設定コンテナを取得します。</summary>
    public static ConfigBase<Boss2Config> Boss2Container => _boss2ConfigContainer;

    /// <summary>Boss 1 パターン設定コンテナを取得します。</summary>
    public static ConfigBase<Boss1PatternConfig> Boss1PatternsContainer => _boss1PatternsContainer;

    /// <summary>Boss 2 パターン設定コンテナを取得します。</summary>
    public static ConfigBase<Boss2PatternConfig> Boss2PatternsContainer => _boss2PatternsContainer;

    /// <summary>特殊弾幕設定コンテナを取得します。</summary>
    public static ConfigBase<SpecialBulletConfig> SpecialBulletsContainer => _specialBulletsContainer;

    /// <summary>アリーナ足場設定コンテナを取得します。</summary>
    public static ConfigBase<ArenaConfig> ArenaContainer => _arenaConfigContainer;

    /// <summary>描画設定コンテナを取得します。</summary>
    public static ConfigBase<RenderConfig> RenderContainer => _renderConfigContainer;

    /// <summary>カメラ設定コンテナを取得します。</summary>
    public static ConfigBase<CameraConfig> CameraContainer => _cameraConfigContainer;

    /// <summary>タイルセット設定コンテナを取得します。</summary>
    public static ConfigBase<TileSetConfig> TileSetContainer => _tileSetConfigContainer;

    /// <summary>音響設定コンテナを取得します。</summary>
    public static ConfigBase<AudioConfig> AudioContainer => _audioConfigContainer;

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
        LoadConfig(ResourcePath.Config.Boss1Patterns, ref _boss1PatternsContainer, ref successCount, ref failureCount);
        LoadConfig(ResourcePath.Config.Boss2Patterns, ref _boss2PatternsContainer, ref successCount, ref failureCount);
        LoadConfig(ResourcePath.Config.SpecialBullets, ref _specialBulletsContainer, ref successCount,
            ref failureCount);
        LoadConfig(ResourcePath.Config.Arena, ref _arenaConfigContainer, ref successCount, ref failureCount);
        LoadConfig(ResourcePath.Config.Render, ref _renderConfigContainer, ref successCount, ref failureCount);
        LoadConfig(ResourcePath.Config.Camera, ref _cameraConfigContainer, ref successCount, ref failureCount);
        LoadConfig(ResourcePath.Platform.SheetToml, ref _tileSetConfigContainer, ref successCount, ref failureCount);
        LoadConfig(ResourcePath.Config.Audio, ref _audioConfigContainer, ref successCount, ref failureCount);

        Debug.Assert(failureCount == 0, "一部の TOML 設定ファイルの読み込みに失敗しました。");

        if (failureCount > 0)
        {
            Debug.WriteLine($"[C# 設定警告] TOML 設定読み込み完了 (成功: {successCount}, 失敗: {failureCount})。");
            return;
        }

        Debug.WriteLine($"[C# 設定初期化] 全ての TOML 設定ファイル（{successCount} 件）の読み込みが正常に完了しました！");
        Debug.Assert(
            SpecialBullets.CyanWaveAmplitude > 0.0f &&
            SpecialBullets.MagentaPulseAmplitude > 0.0f &&
            SpecialBullets.YellowOrbitSpeed > 0.0f,
            "特殊弾幕変調パラメータが正常に読み込まれていません。");
        Debug.WriteLine(
            $"[C# 特殊弾幕設定] Cyan Amp={SpecialBullets.CyanWaveAmplitude} | Magenta Amp={SpecialBullets.MagentaPulseAmplitude} | Yellow Speed={SpecialBullets.YellowOrbitSpeed}");

        _boss1ConfigContainer.OnConfigReloaded += Boss1PipelineDefinition.Reload;
        _boss2ConfigContainer.OnConfigReloaded += Boss2PipelineDefinition.Reload;
        _boss1PatternsContainer.OnConfigReloaded += Boss1PipelineDefinition.Reload;
        _boss2PatternsContainer.OnConfigReloaded += Boss2PipelineDefinition.Reload;
#else
        ErrorOr<BakedConfigBundle> loadResult = BinaryConfigLoader.Load();
        if (loadResult.IsError)
        {
            Debug.WriteLine($"[C# 設定エラー] Release 版バイナリ設定 (baked_configs.bin) の読み込みに失敗しました: {loadResult.FirstError.Description}");
            return;
        }

        BakedConfigBundle bundle = loadResult.Value;
        _playerConfigContainer = new(bundle.Player);
        _boss1ConfigContainer = new(bundle.Boss1);
        _boss2ConfigContainer = new(bundle.Boss2);
        _boss1PatternsContainer = new(bundle.Boss1Pattern);
        _boss2PatternsContainer = new(bundle.Boss2Pattern);
        _specialBulletsContainer = new(bundle.SpecialBullet);
        _arenaConfigContainer = new(bundle.Arena);
        _renderConfigContainer = new(bundle.Render);
        _cameraConfigContainer = new(bundle.Camera);
        _tileSetConfigContainer = new(bundle.TileSet);

        Debug.WriteLine("[C# 設定初期化] Release 版: バイナリ設定データ (baked_configs.bin) を正常にロードして起動しました。");
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
            Debug.WriteLine(
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
        _boss1PatternsContainer.Dispose();
        _boss2PatternsContainer.Dispose();
        _specialBulletsContainer.Dispose();
        _arenaConfigContainer.Dispose();
        _renderConfigContainer.Dispose();
        _cameraConfigContainer.Dispose();
        _tileSetConfigContainer.Dispose();
    }
}