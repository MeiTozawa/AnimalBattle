using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using ErrorOr;

namespace AnimalBattle.Configs;

/// <summary>
///     各種設定を統括管理し、
///     Debug ビルド時の動的自動リロードおよび Release ビルド時のバイナリ高速ロードを提供する静的設定マネージャークラス。
/// </summary>
public static class ConfigManager {
    private static ConfigBase<PlayerConfig> _catConfigContainer = new(new PlayerConfig());
    private static ConfigBase<PlayerConfig> _bunnyConfigContainer = new(new PlayerConfig());
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
    private static ConfigBase<CombatPrefabConfig> _combatPrefabConfigContainer = new(new CombatPrefabConfig());
    private static ConfigBase<PlayerPrefabConfig> _catPrefabConfigContainer = new(new PlayerPrefabConfig());
    private static ConfigBase<PlayerPrefabConfig> _bunnyPrefabConfigContainer = new(new PlayerPrefabConfig());
    private static ConfigBase<BossPrefabConfig> _boss1PrefabConfigContainer = new(new BossPrefabConfig());
    private static ConfigBase<BossPrefabConfig> _boss2PrefabConfigContainer = new(new BossPrefabConfig());
    private static bool _isInitialized;

    static ConfigManager()
    {
        Initialize();
    }

    /// <summary>Cat (猫) プレイヤー設定オブジェクトを取得します。</summary>
    public static PlayerConfig Cat => _catConfigContainer.Config;

    /// <summary>Bunny (ウサギ) プレイヤー設定オブジェクトを取得します。</summary>
    public static PlayerConfig Bunny => _bunnyConfigContainer.Config;

    /// <summary>現在選択中のプレイヤー設定オブジェクトを取得します。</summary>
    public static PlayerConfig Player => GameManager.Current?.Player?.CurrentCharacter == CharacterType.Bunny ? Bunny : Cat;

    /// <summary>指定されたキャラクター種別のプレイヤー設定オブジェクトを取得します。</summary>
    public static PlayerConfig GetPlayerConfig(CharacterType charType) => charType == CharacterType.Bunny ? Bunny : Cat;

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

    /// <summary>戦闘原型設定オブジェクトを取得します。</summary>
    public static CombatPrefabConfig CombatPrefabs => _combatPrefabConfigContainer.Config;

    /// <summary>Cat プレイヤー原型設定オブジェクトを取得します。</summary>
    public static PlayerPrefabConfig CatPrefab => _catPrefabConfigContainer.Config;

    /// <summary>Bunny プレイヤー原型設定オブジェクトを取得します。</summary>
    public static PlayerPrefabConfig BunnyPrefab => _bunnyPrefabConfigContainer.Config;

    /// <summary>Boss 1 原型設定オブジェクトを取得します。</summary>
    public static BossPrefabConfig Boss1Prefab => _boss1PrefabConfigContainer.Config;

    /// <summary>Boss 2 原型設定オブジェクトを取得します。</summary>
    public static BossPrefabConfig Boss2Prefab => _boss2PrefabConfigContainer.Config;

    /// <summary>Cat プレイヤー設定コンテナを取得します。</summary>
    public static ConfigBase<PlayerConfig> CatContainer => _catConfigContainer;

    /// <summary>Bunny プレイヤー設定コンテナを取得します。</summary>
    public static ConfigBase<PlayerConfig> BunnyContainer => _bunnyConfigContainer;

    /// <summary>現在選択中のプレイヤー設定コンテナを取得します（後方互換）。</summary>
    public static ConfigBase<PlayerConfig> PlayerContainer => GameManager.Current?.Player?.CurrentCharacter == CharacterType.Bunny ? _bunnyConfigContainer : _catConfigContainer;

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

    /// <summary>戦闘原型設定コンテナを取得します。</summary>
    public static ConfigBase<CombatPrefabConfig> CombatPrefabsContainer => _combatPrefabConfigContainer;

    /// <summary>Cat プレイヤー原型設定コンテナを取得します。</summary>
    public static ConfigBase<PlayerPrefabConfig> CatPrefabContainer => _catPrefabConfigContainer;

    /// <summary>Bunny プレイヤー原型設定コンテナを取得します。</summary>
    public static ConfigBase<PlayerPrefabConfig> BunnyPrefabContainer => _bunnyPrefabConfigContainer;

    /// <summary>Boss 1 原型設定コンテナを取得します。</summary>
    public static ConfigBase<BossPrefabConfig> Boss1PrefabContainer => _boss1PrefabConfigContainer;

    /// <summary>Boss 2 原型設定コンテナを取得します。</summary>
    public static ConfigBase<BossPrefabConfig> Boss2PrefabContainer => _boss2PrefabConfigContainer;

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

        LoadConfig(ResourcePath.Config.Cat, ref _catConfigContainer, ref successCount, ref failureCount);
        LoadConfig(ResourcePath.Config.Bunny, ref _bunnyConfigContainer, ref successCount, ref failureCount);
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
        LoadConfig(ResourcePath.Config.CombatPrefab, ref _combatPrefabConfigContainer, ref successCount, ref failureCount);
        LoadConfig(ResourcePath.Config.CatPrefab, ref _catPrefabConfigContainer, ref successCount, ref failureCount);
        LoadConfig(ResourcePath.Config.BunnyPrefab, ref _bunnyPrefabConfigContainer, ref successCount, ref failureCount);
        LoadConfig(ResourcePath.Config.Boss1Prefab, ref _boss1PrefabConfigContainer, ref successCount, ref failureCount);
        LoadConfig(ResourcePath.Config.Boss2Prefab, ref _boss2PrefabConfigContainer, ref successCount, ref failureCount);

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
#else
        ErrorOr<BakedConfigBundle> loadResult = BinaryConfigLoader.Load();
        if (loadResult.IsError)
        {
            Debug.Fail($"[ConfigManager エラー] Release 版バイナリ設定 (baked_configs.bin) の読み込みに失敗しました: {loadResult.FirstError.Description}");
            return;
        }

        BakedConfigBundle bundle = loadResult.Value;
        _catConfigContainer = new(bundle.Cat ?? bundle.Player);
        _bunnyConfigContainer = new(bundle.Bunny ?? bundle.Player);
        _boss1ConfigContainer = new(bundle.Boss1);
        _boss2ConfigContainer = new(bundle.Boss2);
        _boss1PatternsContainer = new(bundle.Boss1Pattern);
        _boss2PatternsContainer = new(bundle.Boss2Pattern);
        _specialBulletsContainer = new(bundle.SpecialBullet);
        _arenaConfigContainer = new(bundle.Arena);
        _renderConfigContainer = new(bundle.Render);
        _cameraConfigContainer = new(bundle.Camera);
        _tileSetConfigContainer = new(bundle.TileSet);
        _combatPrefabConfigContainer = new(bundle.CombatPrefab);
        _catPrefabConfigContainer = new(bundle.CatPrefab);
        _bunnyPrefabConfigContainer = new(bundle.BunnyPrefab);
        _boss1PrefabConfigContainer = new(bundle.Boss1Prefab);
        _boss2PrefabConfigContainer = new(bundle.Boss2Prefab);

        Debug.WriteLine("[C# 設定初期化] Release 版: バイナリ設定データ (baked_configs.bin) を正常にロードして起動しました。");
#endif
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
            Debug.Assert(false, $"[ConfigManager 警告] {configPath} の読み込みに失敗しました ({res.FirstError.Type}): {res.FirstError.Description}");
            failureCount++;
        }
    }
#endif

    /// <summary>
    ///     全設定コンテナのファイル監視リソースを安全に解放します。
    /// </summary>
    public static void Dispose()
    {
        _catConfigContainer.Dispose();
        _bunnyConfigContainer.Dispose();
        _boss1ConfigContainer.Dispose();
        _boss2ConfigContainer.Dispose();
        _boss1PatternsContainer.Dispose();
        _boss2PatternsContainer.Dispose();
        _specialBulletsContainer.Dispose();
        _arenaConfigContainer.Dispose();
        _renderConfigContainer.Dispose();
        _cameraConfigContainer.Dispose();
        _tileSetConfigContainer.Dispose();
        _audioConfigContainer.Dispose();
        _combatPrefabConfigContainer.Dispose();
        _catPrefabConfigContainer.Dispose();
        _bunnyPrefabConfigContainer.Dispose();
        _boss1PrefabConfigContainer.Dispose();
        _boss2PrefabConfigContainer.Dispose();
    }
}