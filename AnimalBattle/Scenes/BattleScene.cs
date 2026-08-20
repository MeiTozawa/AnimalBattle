using System;
using Flecs.NET.Core;
using AnimalBattle.Bindings;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Core.Asset;
using AnimalBattle.Maps;
using AnimalBattle.Plugins;

namespace AnimalBattle.Scenes;

/// <summary>
/// Flecs.NET ワールドおよび Boss 戦パイプラインを統括するバトルシーンクラス。
/// TOML 設定ファイル（player.toml, boss.toml, arena.toml）から初期パラメータをロードし、
/// プレイヤー、Boss、プラットフォームへ Bevy スタイルの Hitbox / Hurtbox を付与します。
/// </summary>
public partial class BattleScene : IDisposable
{
    private readonly World _ecsWorld;
    private TileMap? _tileMap;
    private bool _isDisposed;

    /// <summary>
    /// 現在アクティブなタイルマップインスタンスを取得します。
    /// </summary>
    public static TileMap? CurrentTileMap { get; private set; }

    /// <summary>
    /// <see cref="BattleScene"/> の新しいインスタンスを初期化します。
    /// </summary>
    public BattleScene()
    {
        _ecsWorld = World.Create();
    }

    /// <summary>
    /// 各種 TOML 設定ファイル、システムおよびエンティティ（プレイヤー、Boss、プラットフォーム）の初期化を行います。
    /// </summary>
    public void Initialize()
    {
        if (!ConfigManager.Initialize())
        {
            Console.WriteLine("[BattleScene 警告] 一部の TOML 設定ファイルの初期化に失敗しました。デフォルト値を使用します。");
        }

        if (!AssetManager.Initialize())
        {
            Console.WriteLine("[BattleScene 警告] 一部のキャラクタースプライト画像のロードに失敗しました。");
        }

        int playerHandle = AssetManager.GetCharacterSpriteHandle(CharacterType.Cat, "Idle");
        int bossHandle = AssetManager.GetTexture(ResourcePath.Characters.Character031_3Png);

        App app = new App(_ecsWorld)
            .AddPluginGroup(new DefaultPlugins());
        app.FinishSetup();

        CombatEffectHelper.Initialize(_ecsWorld);

        PlayerConfig playerCfg = ConfigManager.Player;
        BossConfig bossCfg = ConfigManager.Boss;

        // 1. プレイヤーの生成 (地形 CollisionBox + TOML 設定駆動 Hurtbox)
        _ecsWorld.Entity("Player1")
            .Set(new Transform(playerCfg.SpawnX, playerCfg.SpawnY, scaleX: playerCfg.ScaleX, scaleY: playerCfg.ScaleY))
            .Set(new Velocity())
            .Set(new CollisionBox(playerCfg.HitboxWidth, playerCfg.HitboxHeight))
            .Set(new Hurtbox(playerCfg.GetHurtboxShape(), Faction.Friendly))
            .Set(new Health(playerCfg.MaxHp))
            .Set(new DoubleJump(playerCfg.MaxJumps))
            .Set(new PlayerState())
            .Set(new Sprite(playerHandle, flipX: false, isVisible: true, srcRect: new Rect<int>(0, 0, 32, 32)))
            .Set(new Animation("Idle", CharacterType.Cat, isPlaying: true))
            .Add<PlayerTag>();

        // 2. Boss の生成 (TOML 設定駆動 被弾 Hurtbox + 肉体接触突進 Hitbox)
        _ecsWorld.Entity("Boss")
            .Set(new Transform(bossCfg.BaseX, bossCfg.BaseY, scaleX: bossCfg.ScaleX, scaleY: bossCfg.ScaleY))
            .Set(new Velocity())
            .Set(new Hurtbox(bossCfg.GetHurtboxShape(), Faction.Hostile))
            .Set(new Hitbox(bossCfg.GetHitboxShape(), damage: bossCfg.BodyContactDamage, faction: Faction.Hostile, destroyOnHit: false))
            .Set(new Health(bossCfg.MaxHp))
            .Set(new BossStats(baseX: bossCfg.BaseX, baseY: bossCfg.BaseY))
            .Set(new BossStateMachine())
            .Set(new Sprite(bossHandle, flipX: false, isVisible: true, srcRect: new Rect<int>(0, 0, 192, 192)))
            .Add<BossTag>();

        // 3. アリーナ足場および TileMap
        SetupArena();

        // 4. TOML 設定ホットリロード時の Entity コンポーネント自動再同期
        RegisterConfigReloadHooks();

        Console.WriteLine("[初期化完了] Boss バトルシーンが正常に初期化されました！");
    }

    /// <summary>
    /// TOML 設定ファイル変更時のホットリロードコールバックを登録し、Entity の衝突判定・スケールを即時再同期します。
    /// </summary>
    private void RegisterConfigReloadHooks()
    {
        ConfigManager.BossContainer.OnConfigReloaded += OnBossConfigReloaded;
        ConfigManager.PlayerContainer.OnConfigReloaded += OnPlayerConfigReloaded;
    }

    private void OnBossConfigReloaded()
    {
        Entity bossEntity = _ecsWorld.Lookup("Boss");
        if (bossEntity.IsAlive())
        {
            BossConfig bCfg = ConfigManager.Boss;
            bossEntity.Set(new Hurtbox(bCfg.GetHurtboxShape(), Faction.Hostile));
            bossEntity.Set(new Hitbox(bCfg.GetHitboxShape(), damage: bCfg.BodyContactDamage, faction: Faction.Hostile, destroyOnHit: false));

            if (bossEntity.Has<Transform>())
            {
                ref Transform t = ref bossEntity.GetMut<Transform>();
                t.ScaleX = bCfg.ScaleX;
                t.ScaleY = bCfg.ScaleY;
            }

            Console.WriteLine($"[BattleScene] Boss の Hurtbox / Hitbox / Scale を TOML からリアルタイム同期しました (HurtboxRadius: {bCfg.HurtboxRadius}, BodyRadius: {bCfg.BodyHitboxRadius})");
        }
    }

    private void OnPlayerConfigReloaded()
    {
        Entity playerEntity = _ecsWorld.Lookup("Player1");
        if (playerEntity.IsAlive())
        {
            PlayerConfig pCfg = ConfigManager.Player;
            playerEntity.Set(new Hurtbox(pCfg.GetHurtboxShape(), Faction.Friendly));
            playerEntity.Set(new CollisionBox(pCfg.HitboxWidth, pCfg.HitboxHeight));

            if (playerEntity.Has<Transform>())
            {
                ref Transform t = ref playerEntity.GetMut<Transform>();
                t.ScaleX = pCfg.ScaleX;
                t.ScaleY = pCfg.ScaleY;
            }

            Console.WriteLine($"[BattleScene] Player の Hurtbox / CollisionBox / Scale を TOML からリアルタイム同期しました (HurtboxRadius: {pCfg.HurtboxRadius}, TerrainBox: {pCfg.HitboxWidth}x{pCfg.HitboxHeight})");
        }
    }

    /// <summary>
    /// arena.toml の TileMap 設定および TileSet の衝突領域に基づいてプラットフォームコライダーを生成します。
    /// </summary>
    private void SetupArena()
    {
        ArenaConfig arenaCfg = ConfigManager.Arena;

        // TileMap のロード、初期化およびコライダー生成
        TileMapConfig tileMapCfg = arenaCfg.TileMap;
        Result<ConfigBase<TileSetConfig>> tileSetConfigResult = TileSetConfig.LoadFromFile(tileMapCfg.TileConfig);
        if (tileSetConfigResult.IsOk && tileSetConfigResult.Value != null)
        {
            int sheetHandle = AssetManager.LoadTexture(tileMapCfg.TileSheet);
            TileSet tileSet = new TileSet(sheetHandle, tileSetConfigResult.Value.Config);
            _tileMap = TileMap.FromConfig(tileMapCfg, tileSet);
            _ecsWorld.Set(_tileMap);
            CurrentTileMap = _tileMap;

            _tileMap.BuildPlatformColliders(_ecsWorld);
            Console.WriteLine("[BattleScene] TileMap および TileSet プラットフォームコライダーが正常に初期化されました。");
        }
        else
        {
            Console.WriteLine($"[BattleScene 警告] TileSet 設定のロードに失敗しました: {tileSetConfigResult.Error?.Description}");
        }
    }

    /// <summary>
    /// シーンのフレーム更新処理を行います。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間（秒）</param>
    /// <returns>正常続行時は true</returns>
    public bool Update(float deltaTime)
    {
        _ecsWorld.Progress(deltaTime);
        return true;
    }

    /// <summary>
    /// シーンの描画処理を行います。
    /// </summary>
    public void Render()
    {
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            ConfigManager.BossContainer.OnConfigReloaded -= OnBossConfigReloaded;
            ConfigManager.PlayerContainer.OnConfigReloaded -= OnPlayerConfigReloaded;

            _ecsWorld.Dispose();
            _isDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
