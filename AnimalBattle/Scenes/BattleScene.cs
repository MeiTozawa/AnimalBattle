using System;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Bindings;
using AnimalBattle.Characters;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Core.Asset;
using AnimalBattle.Core.Input;
using AnimalBattle.Core.Physics;
using AnimalBattle.Maps;
using AnimalBattle.Modules;
using AnimalBattle.Systems;
using ErrorOr;
using Flecs.NET.Core;

namespace AnimalBattle.Scenes;

/// <summary>
/// Flecs.NET ワールドおよび多 Boss (Boss 1 / Boss 2) 戦パイプラインを統括するバトルシーンクラス。
/// TOML 設定ファイル（player.toml, boss1.toml, boss2.toml, arena.toml）から初期パラメータをロードし、
/// プレイヤー、Boss、プラットフォームへ Bevy スタイルの Hitbox / Hurtbox を付与します。
/// </summary>
public partial class BattleScene : IDisposable
{
    private readonly World _ecsWorld;
    private readonly WorldRenderSystem _worldRenderer;
    private readonly DebugRenderSystem _debugRenderer;
    private readonly Query _playerBulletsQuery;
    private readonly Query _effectsQuery;
    private Player? _player;
    private BossCharacter? _currentBoss;
    private Query<Transform, CollisionBox> _platformQuery;
    private TileMap? _tileMap;
    private bool _prevSelectBoss1;
    private bool _prevSelectBoss2;
    private bool _isDisposed;

    /// <summary>
    /// 現在アクティブな BattleScene インスタンスを取得します。
    /// </summary>
    public static BattleScene? Current { get; private set; }

    /// <summary>
    /// 現在アクティブなタイルマップインスタンスを取得します。
    /// </summary>
    public static TileMap? CurrentTileMap { get; private set; }

    /// <summary>
    /// 現在アクティブなプレイヤー自機インスタンスを取得します。
    /// </summary>
    public Player? Player => _player;

    /// <summary>
    /// 現在アクティブな Boss インスタンスを取得します。
    /// </summary>
    public BossCharacter? Boss => _currentBoss;

    /// <summary>
    /// <see cref="BattleScene"/> の新しいインスタンスを初期化し、全 ECS パイプラインを登録します。
    /// </summary>
    public BattleScene()
    {
        _ecsWorld = World.Create();
        Current = this;

        // 0. 基本設定 & アセットのロード
        ConfigManager.Initialize();
        AssetManager.Initialize();

        ErrorOr<int> playerHandleRes = AssetManager.GetCharacterSpriteHandle(CharacterType.Cat, "Idle");
        int playerHandle = !playerHandleRes.IsError ? playerHandleRes.Value : -1;

        _ecsWorld.Import<ComponentRequirementsModule>();
        _ecsWorld.Import<EffectModule>();
        _ecsWorld.Import<BulletModule>();

        CombatEffectHelper.Initialize(_ecsWorld);

        PlayerConfig playerCfg = ConfigManager.Player;
        ArenaConfig arenaCfg = ConfigManager.Arena;

        // 1. プレイヤーの生成 (高内聚な領域オブジェクト Player の初期化)
        Entity playerEntity = _ecsWorld.Entity("Player1");
        _player = new(playerEntity, playerCfg, playerHandle);

        // 2. タイルマップとプラットフォームの生成
        SetupArena();

        _platformQuery = _ecsWorld.QueryBuilder<Transform, CollisionBox>()
            .With<PlatformTag>()
            .Build();

        _worldRenderer = new(_ecsWorld);
        _debugRenderer = new(_ecsWorld);
        _playerBulletsQuery = _ecsWorld.QueryBuilder()
                                       .With<PlayerBulletTag>()
                                       .Build();
        _effectsQuery = _ecsWorld.QueryBuilder()
                                 .With<EffectTag>()
                                 .Build();

        // 3. アクティブな Boss (デフォルト: Boss 1) の生成
        SpawnBoss(CurrentBossType);
    }

    /// <summary>
    /// 指定された種類の Boss エンティティを生成またはリセットします。
    /// </summary>
    /// <param name="bossType">生成する Boss の種別 (<see cref="BossType.Boss1"/> または <see cref="BossType.Boss2"/>)</param>
    /// <returns>生成または再初期化された Flecs エンティティ</returns>
    public Entity SpawnBoss(BossType bossType)
    {
        string otherBossName = bossType == BossType.Boss1 ? "Boss2" : "Boss1";
        Entity otherBoss = _ecsWorld.Lookup(otherBossName);
        if (otherBoss.IsAlive())
        {
            otherBoss.Destruct();
        }

        string entityName = bossType == BossType.Boss1 ? "Boss1" : "Boss2";
        Entity bossEntity = _ecsWorld.Entity(entityName);

        if (bossType == BossType.Boss1)
        {
            SpawnBoss1(bossEntity);
        }
        else if (bossType == BossType.Boss2)
        {
            SpawnBoss2(bossEntity);
        }

        return bossEntity;
    }

    private void SpawnBoss1(Entity bossEntity)
    {
        ErrorOr<int> boss1HandleRes = AssetManager.GetTexture(ResourcePath.Characters.Character031_3Png);
        int boss1Handle = !boss1HandleRes.IsError ? boss1HandleRes.Value : -1;
        _currentBoss = new(bossEntity, BossType.Boss1, boss1Handle);
        Console.WriteLine($"[BattleScene] Boss 1 (Ancient Cat Overlord) を生成・リセットしました (HP: {ConfigManager.Boss1.MaxHp}, Pos: {ConfigManager.Boss1.SpawnX}, {ConfigManager.Boss1.SpawnY})");
    }

    private void SpawnBoss2(Entity bossEntity)
    {
        ErrorOr<int> boss2HandleRes = AssetManager.GetTexture(ResourcePath.Characters.Character027_3Png);
        int boss2Handle = !boss2HandleRes.IsError ? boss2HandleRes.Value : -1;
        _currentBoss = new(bossEntity, BossType.Boss2, boss2Handle);
        Console.WriteLine($"[BattleScene] Boss 2 (Ground Crusher) を生成・リセットしました (HP: {ConfigManager.Boss2.MaxHp}, Pos: {ConfigManager.Boss2.SpawnX}, {ConfigManager.Boss2.SpawnY})");
    }

    /// <summary>
    /// 現在アクティブな Boss の種別を取得または設定します。
    /// </summary>
    public BossType CurrentBossType { get; private set; } = BossType.Boss1;

    /// <summary>
    /// 現在アクティブな Boss の識別名を取得します。
    /// </summary>
    public string CurrentBossId => CurrentBossType == BossType.Boss1 ? "Boss1" : "Boss2";

    /// <summary>
    /// シーンの追加初期化処理（設定ホットリロードフックの登録等）を行います。
    /// </summary>
    public void Initialize()
    {
        RegisterConfigReloadHooks();
    }

    /// <summary>
    /// 現在の Boss を初期状態にリセットして再生成します。
    /// </summary>
    public void ResetCurrentBoss()
    {
        _currentBoss?.Reset();
    }

    /// <summary>
    /// 対象の Boss へ切り替えます。
    /// </summary>
    /// <param name="bossId">切り替え先の Boss 識別名 ("Boss1" または "Boss2")</param>
    public void SwitchBoss(string bossId)
    {
        BossType targetType = bossId.Equals("Boss2", StringComparison.OrdinalIgnoreCase)
            ? BossType.Boss2
            : BossType.Boss1;
        CurrentBossType = targetType;
        SpawnBoss(targetType);
    }

    /// <summary>
    /// TOML 設定ファイル変更時のホットリロードコールバックを登録し、Entity の衝突判定・スケールを即時再同期します。
    /// </summary>
    private void RegisterConfigReloadHooks()
    {
        ConfigManager.Boss1Container.OnConfigReloaded += OnBoss1ConfigReloaded;
        ConfigManager.Boss2Container.OnConfigReloaded += OnBoss2ConfigReloaded;
        ConfigManager.PlayerContainer.OnConfigReloaded += OnPlayerConfigReloaded;
    }

    private void OnBoss1ConfigReloaded()
    {
        if (CurrentBossId.Equals("Boss1", StringComparison.OrdinalIgnoreCase))
        {
            Entity bossEntity = _ecsWorld.Lookup("Boss1");
            if (bossEntity.IsAlive() && bossEntity.Has<Boss1Tag>())
            {
                Boss1Config bCfg = ConfigManager.Boss1;
                bossEntity.Set(new Hurtbox(bCfg.GetHurtboxShape(), Faction.Hostile));
                bossEntity.Set(new Hitbox(bCfg.GetHitboxShape(), damage: bCfg.BodyContactDamage, faction: Faction.Hostile, destroyOnHit: false));

                if (bossEntity.Has<Transform>())
                {
                    ref Transform t = ref bossEntity.GetMut<Transform>();
                    t.ScaleX = bCfg.ScaleX;
                    t.ScaleY = bCfg.ScaleY;
                }

                Console.WriteLine($"[BattleScene] Boss 1 の Hurtbox / Hitbox / Scale を TOML からリアルタイム同期しました");
            }
        }
    }

    private void OnBoss2ConfigReloaded()
    {
        if (CurrentBossId.Equals("Boss2", StringComparison.OrdinalIgnoreCase))
        {
            Entity bossEntity = _ecsWorld.Lookup("Boss2");
            if (bossEntity.IsAlive() && bossEntity.Has<Boss2Tag>())
            {
                Boss2Config bCfg = ConfigManager.Boss2;
                bossEntity.Set(new Hurtbox(bCfg.GetHurtboxShape(), Faction.Hostile));
                bossEntity.Set(new Hitbox(bCfg.GetHitboxShape(), damage: bCfg.BodyContactDamage, faction: Faction.Hostile, destroyOnHit: false));
                bossEntity.Set(new CollisionBox(bCfg.CollisionWidth, bCfg.CollisionHeight));

                if (bossEntity.Has<Transform>())
                {
                    ref Transform t = ref bossEntity.GetMut<Transform>();
                    t.ScaleX = bCfg.ScaleX;
                    t.ScaleY = bCfg.ScaleY;
                }

                Console.WriteLine($"[BattleScene] Boss 2 の Hurtbox / Hitbox / CollisionBox / Scale を TOML からリアルタイム同期しました");
            }
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

            Console.WriteLine($"[BattleScene] Player の Hurtbox / CollisionBox / Scale を TOML からリアルタイム同期しました");
        }
    }

    /// <summary>
    /// arena.toml の TileMap 設定および TileSet の衝突領域に基づいてプラットフォームコライダーを生成します。
    /// </summary>
    private void SetupArena()
    {
        ArenaConfig arenaCfg = ConfigManager.Arena;

        TileMapConfig tileMapCfg = arenaCfg.TileMap;
        ErrorOr<ConfigBase<TileSetConfig>> tileSetConfigResult = TileSetConfig.LoadFromFile(tileMapCfg.TileConfig);
        if (!tileSetConfigResult.IsError && tileSetConfigResult.Value != null)
        {
            ErrorOr<int> sheetHandleRes = AssetManager.LoadTexture(tileMapCfg.TileSheet);
            int sheetHandle = !sheetHandleRes.IsError ? sheetHandleRes.Value : -1;
            TileSet tileSet = new(sheetHandle, tileSetConfigResult.Value.Config);
            _tileMap = TileMap.FromConfig(tileMapCfg, tileSet);
            _ecsWorld.Set(_tileMap);
            CurrentTileMap = _tileMap;
            AnimalBattle.Ai.Boss2.Boss2PatrolChaseModule.NavGraphProvider = () => CurrentTileMap?.NavGraph;

            _tileMap.BuildPlatformColliders(_ecsWorld);
            Console.WriteLine("[BattleScene] TileMap および TileSet プラットフォームコライダーが正常に初期化されました。");
        }
        else
        {
            Console.WriteLine($"[BattleScene 警告] TileSet 設定のロードに失敗しました: {tileSetConfigResult.FirstError.Description}");
        }
    }

    /// <summary>
    /// シーンのフレーム更新処理を行います。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間（秒）</param>
    public void Update(float deltaTime)
    {
        HandleInput();

        if (_player != null)
        {
            _player.Update(deltaTime, ConfigManager.Player, ConfigManager.Arena, _platformQuery, _ecsWorld);
        }

        if (_currentBoss != null)
        {
            Vec2f playerPos = _player != null ? new(_player.X, _player.Y) : Vec2f.Zero;
            _currentBoss.Update(deltaTime, playerPos, _ecsWorld, _platformQuery);
        }

        _ecsWorld.Progress(deltaTime);
    }

    private void HandleInput()
    {
        bool restartJustPressed = InputService.IsPressed(InputAction.Restart);
        bool key1Down = GameEngine.CheckHitKey(0x02);
        bool key2Down = GameEngine.CheckHitKey(0x03);

        bool selectBoss1Pressed = key1Down && !_prevSelectBoss1;
        bool selectBoss2Pressed = key2Down && !_prevSelectBoss2;
        _prevSelectBoss1 = key1Down;
        _prevSelectBoss2 = key2Down;

        if (selectBoss1Pressed)
        {
            SwitchBoss("Boss1");
            ResetCombat();
        }
        else if (selectBoss2Pressed)
        {
            SwitchBoss("Boss2");
            ResetCombat();
        }
        else if (restartJustPressed)
        {
            ResetCurrentBoss();
            ResetCombat();
        }
    }

    private void ResetCombat()
    {
        CombatEffectHelper.ClearAllBossBullets(_ecsWorld);
        _playerBulletsQuery.Each(e => e.Destruct());
        _effectsQuery.Each(e => e.Destruct());
        _player?.Reset(ConfigManager.Player);
    }

    /// <summary>
    /// シーンの描画処理を行います。
    /// </summary>
    public void Render()
    {
        _worldRenderer.Render(_tileMap, _platformQuery, _player, _currentBoss);
        HudRenderSystem.Render(_player, _currentBoss);
        _debugRenderer.Render(_platformQuery, _tileMap);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            ConfigManager.Boss1Container.OnConfigReloaded -= OnBoss1ConfigReloaded;
            ConfigManager.Boss2Container.OnConfigReloaded -= OnBoss2ConfigReloaded;
            ConfigManager.PlayerContainer.OnConfigReloaded -= OnPlayerConfigReloaded;

            _worldRenderer.Dispose();
            _debugRenderer.Dispose();
            _playerBulletsQuery.Dispose();
            _effectsQuery.Dispose();
            _platformQuery.Dispose();
            _ecsWorld.Dispose();
            AnimalBattle.Ai.Boss2.Boss2PatrolChaseModule.NavGraphProvider = null;
            if (Current == this)
            {
                Current = null;
            }
            _isDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
