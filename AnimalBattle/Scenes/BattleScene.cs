using System;

using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Bindings;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Core.Asset;
using AnimalBattle.Maps;
using AnimalBattle.Plugins;
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
    private TileMap? _tileMap;
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

        App app = new App(_ecsWorld)
            .AddPluginGroup(new DefaultPlugins());
        app.FinishSetup();

        CombatEffectHelper.Initialize(_ecsWorld);

        PlayerConfig playerCfg = ConfigManager.Player;
        ArenaConfig arenaCfg = ConfigManager.Arena;

        // 1. プレイヤーの生成 (地形 CollisionBox + TOML 設定駆動 Hurtbox)
        _ecsWorld.Entity("Player1")
            .Set(new Transform(playerCfg.SpawnX, playerCfg.SpawnY, scaleX: playerCfg.ScaleX, scaleY: playerCfg.ScaleY))
            .Set(new Velocity())
            .Set(new CollisionBox(playerCfg.HitboxWidth, playerCfg.HitboxHeight))
            .Set(new Hurtbox(playerCfg.GetHurtboxShape(), Faction.Friendly))
            .Set(new Health(playerCfg.MaxHp))
            .Set(new DoubleJump(playerCfg.MaxJumps))
            .Set(new PlayerState())
            .Set(new Sprite(playerHandle, flipX: false, isVisible: true, srcRect: new Rect<int>(0, 0, 64, 64)))
            .Set(new Animation("Idle", CharacterType.Cat, isPlaying: true))
            .Add<PlayerTag>();

        // 2. タイルマップとプラットフォームの生成
        SetupArena();

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

    private static void SpawnBoss1(Entity bossEntity)
    {
        Boss1Config b1Cfg = ConfigManager.Boss1;
        ErrorOr<int> boss1HandleRes = AssetManager.GetTexture(ResourcePath.Characters.Character031_3Png);
        int boss1Handle = !boss1HandleRes.IsError ? boss1HandleRes.Value : -1;

        Boss1Stats stats = new Boss1Stats(baseX: b1Cfg.BaseX, baseY: b1Cfg.BaseY);
        Boss1StateMachine stateMachine = new Boss1StateMachine();

        bossEntity
            .Set(new Transform(b1Cfg.BaseX, b1Cfg.BaseY, scaleX: b1Cfg.ScaleX, scaleY: b1Cfg.ScaleY))
            .Set(new Velocity())
            .Set(new Hurtbox(b1Cfg.GetHurtboxShape(), Faction.Hostile))
            .Set(new Hitbox(b1Cfg.GetHitboxShape(), damage: b1Cfg.BodyContactDamage, faction: Faction.Hostile, destroyOnHit: false))
            .Set(new Health(b1Cfg.MaxHp))
            .Set(stats)
            .Set(stateMachine)
            .Set(new Sprite(boss1Handle, flipX: false, isVisible: true, srcRect: new Rect<int>(0, 0, 192, 192)))
            .Add<BossTag>()
            .Add<Boss1Tag>();

        MigrationResult migration = MigrationAdapter.ConvertBoss1(in stateMachine, in stats);
        ApplyMigrationResult(bossEntity, in migration);

        Console.WriteLine($"[BattleScene] Boss 1 (Ancient Cat Overlord) を生成・リセットしました (HP: {b1Cfg.MaxHp}, Pos: {b1Cfg.BaseX}, {b1Cfg.BaseY})");
    }

    private static void SpawnBoss2(Entity bossEntity)
    {
        Boss2Config b2Cfg = ConfigManager.Boss2;
        ErrorOr<int> boss2HandleRes = AssetManager.GetTexture(ResourcePath.Characters.Character027_3Png);
        int boss2Handle = !boss2HandleRes.IsError ? boss2HandleRes.Value : -1;

        Boss2Stats stats = new Boss2Stats(spawnX: b2Cfg.SpawnX, spawnY: b2Cfg.SpawnY);
        Boss2StateMachine stateMachine = new Boss2StateMachine();

        bossEntity
            .Set(new Transform(b2Cfg.SpawnX, b2Cfg.SpawnY, scaleX: b2Cfg.ScaleX, scaleY: b2Cfg.ScaleY))
            .Set(new Velocity())
            .Set(new CollisionBox(b2Cfg.CollisionWidth, b2Cfg.CollisionHeight))
            .Set(new Hurtbox(b2Cfg.GetHurtboxShape(), Faction.Hostile))
            .Set(new Hitbox(b2Cfg.GetHitboxShape(), damage: b2Cfg.BodyContactDamage, faction: Faction.Hostile, destroyOnHit: false))
            .Set(new Health(b2Cfg.MaxHp))
            .Set(stats)
            .Set(stateMachine)
            .Set(new Sprite(boss2Handle, flipX: false, isVisible: true, srcRect: new Rect<int>(0, 0, 192, 192)))
            .Add<BossTag>()
            .Add<Boss2Tag>();

        MigrationResult migration = MigrationAdapter.ConvertBoss2(in stateMachine, in stats);
        ApplyMigrationResult(bossEntity, in migration);

        Console.WriteLine($"[BattleScene] Boss 2 (Ground Crusher) を生成・リセットしました (HP: {b2Cfg.MaxHp}, Pos: {b2Cfg.SpawnX}, {b2Cfg.SpawnY})");
    }

    private static void ApplyMigrationResult(Entity bossEntity, in MigrationResult migration)
    {
        if (!migration.IsSuccess)
        {
            Console.WriteLine($"[BattleScene 警告] Boss 移行データを生成できませんでした: {migration.Diagnostic.Message}");
            return;
        }

        bossEntity.Set(migration.AiData);
        bossEntity.Set(migration.MovementData);
        bossEntity.Set(migration.AttackData);
        bossEntity.Set(new BossPhaseTransitionData());
        bossEntity.Set(new BossTerminalData());
        bossEntity.Set(migration.CompatibilityData);
        bossEntity.Remove<BossDebugData>();
    }

    private static void RefreshMigrationCompatibilityView(Entity bossEntity)
    {
        if (!bossEntity.Has<BossAiData>()
            || !bossEntity.Has<BossMovementData>()
            || !bossEntity.Has<BossAttackData>()
            || !bossEntity.Has<BossCompatibilityData>())
        {
            return;
        }

        ref readonly BossAiData aiData = ref bossEntity.Get<BossAiData>();
        ref readonly BossMovementData movementData = ref bossEntity.Get<BossMovementData>();
        ref readonly BossAttackData attackData = ref bossEntity.Get<BossAttackData>();
        ref readonly BossCompatibilityData compatibilityData = ref bossEntity.Get<BossCompatibilityData>();
        BossCompatibilityData refreshed = MigrationAdapter.CreateCompatibilityView(
            in aiData,
            in movementData,
            in attackData,
            in compatibilityData);
        bossEntity.Set(refreshed);
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
        SpawnBoss(CurrentBossType);
    }

    /// <summary>
    /// compatibility view と終端 data を確認して現在の Boss を初期状態へ戻します。
    /// </summary>
    /// <param name="compatibility">新 runtime が公開した現在の compatibility view。</param>
    /// <param name="terminal">死亡後も保持する終端 data。</param>
    public void ResetCurrentBoss(in BossCompatibilityData compatibility, in BossTerminalData terminal)
    {
        if (terminal.IsTerminal)
        {
            Console.WriteLine($"[BattleScene] 終端状態 {terminal.DeadState} を確認して Boss を再生成します。");
        }
        else if (compatibility.BossState.IsValid())
        {
            Console.WriteLine($"[BattleScene] 互換状態 {compatibility.BossState} から Boss を再生成します。");
        }

        SpawnBoss(CurrentBossType);
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

                RefreshMigrationCompatibilityView(bossEntity);
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

                RefreshMigrationCompatibilityView(bossEntity);
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
            TileSet tileSet = new TileSet(sheetHandle, tileSetConfigResult.Value.Config);
            _tileMap = TileMap.FromConfig(tileMapCfg, tileSet);
            _ecsWorld.Set(_tileMap);
            CurrentTileMap = _tileMap;

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
        _ecsWorld.Progress(deltaTime);
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
            ConfigManager.Boss1Container.OnConfigReloaded -= OnBoss1ConfigReloaded;
            ConfigManager.Boss2Container.OnConfigReloaded -= OnBoss2ConfigReloaded;
            ConfigManager.PlayerContainer.OnConfigReloaded -= OnPlayerConfigReloaded;

            _ecsWorld.Dispose();
            if (Current == this)
            {
                Current = null;
            }
            _isDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
