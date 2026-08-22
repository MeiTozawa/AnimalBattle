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
    /// 現在戦闘中の Boss 識別名（"Boss1" または "Boss2"）を取得します。
    /// </summary>
    public string CurrentBossId { get; private set; } = "Boss1";

    /// <summary>
    /// <see cref="BattleScene"/> の新しいインスタンスを初期化します。
    /// </summary>
    public BattleScene()
    {
        _ecsWorld = World.Create();
        Current = this;
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

        int playerHandle = AssetManager.GetCharacterSpriteHandle(CharacterType.Cat, "Idle").ValueOrDefault(-1);

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
            .Set(new Sprite(playerHandle, flipX: false, isVisible: true, srcRect: new Rect<int>(0, 0, 32, 32)))
            .Set(new Animation("Idle", CharacterType.Cat, isPlaying: true))
            .Add<PlayerTag>();

        // 2. arena.toml で指定された初期 Boss の生成
        CurrentBossId = string.IsNullOrWhiteSpace(arenaCfg.TargetBoss) ? "Boss1" : arenaCfg.TargetBoss;
        SpawnBoss(CurrentBossId);

        // 3. アリーナ足場および TileMap
        SetupArena();

        // 4. TOML 設定ホットリロード時の Entity コンポーネント自動再同期
        RegisterConfigReloadHooks();

        Console.WriteLine($"[初期化完了] Boss バトルシーンが正常に初期化されました！(初期ターゲット: {CurrentBossId})");
    }

    /// <summary>
    /// 指定された Boss 識別名（"Boss1" または "Boss2"）に対応する Boss エンティティを生成・切り替えます。
    /// </summary>
    /// <param name="bossId">対象 Boss 識別名</param>
    public void SpawnBoss(string bossId)
    {
        CurrentBossId = bossId;
        Entity bossEntity = GetOrCreateCleanBossEntity();
        CombatEffectHelper.ClearAllBossBullets(_ecsWorld);

        if (bossId.Equals("Boss2", StringComparison.OrdinalIgnoreCase))
        {
            SpawnBoss2(bossEntity);
        }
        else
        {
            SpawnBoss1(bossEntity);
        }
    }

    private Entity GetOrCreateCleanBossEntity()
    {
        Entity bossEntity = _ecsWorld.Lookup("Boss");
        if (!bossEntity.IsAlive())
        {
            return _ecsWorld.Entity("Boss");
        }

        bossEntity.Remove<Boss1Tag>()
                  .Remove<Boss1Stats>()
                  .Remove<Boss1StateMachine>()
                  .Remove<Boss2Tag>()
                  .Remove<Boss2Stats>()
                  .Remove<Boss2StateMachine>()
                  .Remove<CollisionBox>();

        return bossEntity;
    }

    private static void SpawnBoss1(Entity bossEntity)
    {
        Boss1Config b1Cfg = ConfigManager.Boss1;
        int boss1Handle = AssetManager.GetTexture(ResourcePath.Characters.Character031_3Png).ValueOrDefault(-1);

        bossEntity
            .Set(new Transform(b1Cfg.BaseX, b1Cfg.BaseY, scaleX: b1Cfg.ScaleX, scaleY: b1Cfg.ScaleY))
            .Set(new Velocity())
            .Set(new Hurtbox(b1Cfg.GetHurtboxShape(), Faction.Hostile))
            .Set(new Hitbox(b1Cfg.GetHitboxShape(), damage: b1Cfg.BodyContactDamage, faction: Faction.Hostile, destroyOnHit: false))
            .Set(new Health(b1Cfg.MaxHp))
            .Set(new Boss1Stats(baseX: b1Cfg.BaseX, baseY: b1Cfg.BaseY))
            .Set(new Boss1StateMachine())
            .Set(new Sprite(boss1Handle, flipX: false, isVisible: true, srcRect: new Rect<int>(0, 0, 192, 192)))
            .Add<BossTag>()
            .Add<Boss1Tag>();

        Console.WriteLine($"[BattleScene] Boss 1 (Ancient Cat Overlord) を生成・リセットしました (HP: {b1Cfg.MaxHp}, Pos: {b1Cfg.BaseX}, {b1Cfg.BaseY})");
    }

    private static void SpawnBoss2(Entity bossEntity)
    {
        Boss2Config b2Cfg = ConfigManager.Boss2;
        int boss2Handle = AssetManager.GetTexture(ResourcePath.Characters.Character027_3Png).ValueOrDefault(-1);

        bossEntity
            .Set(new Transform(b2Cfg.SpawnX, b2Cfg.SpawnY, scaleX: b2Cfg.ScaleX, scaleY: b2Cfg.ScaleY))
            .Set(new Velocity())
            .Set(new CollisionBox(b2Cfg.CollisionWidth, b2Cfg.CollisionHeight))
            .Set(new Hurtbox(b2Cfg.GetHurtboxShape(), Faction.Hostile))
            .Set(new Hitbox(b2Cfg.GetHitboxShape(), damage: b2Cfg.BodyContactDamage, faction: Faction.Hostile, destroyOnHit: false))
            .Set(new Health(b2Cfg.MaxHp))
            .Set(new Boss2Stats(spawnX: b2Cfg.SpawnX, spawnY: b2Cfg.SpawnY))
            .Set(new Boss2StateMachine())
            .Set(new Sprite(boss2Handle, flipX: false, isVisible: true, srcRect: new Rect<int>(0, 0, 192, 192)))
            .Add<BossTag>()
            .Add<Boss2Tag>();

        Console.WriteLine($"[BattleScene] Boss 2 (Ground Crusher) を生成・リセットしました (HP: {b2Cfg.MaxHp}, Pos: {b2Cfg.SpawnX}, {b2Cfg.SpawnY})");
    }

    /// <summary>
    /// 現在の Boss を初期状態にリセットして再生成します。
    /// </summary>
    public void ResetCurrentBoss()
    {
        SpawnBoss(CurrentBossId);
    }

    /// <summary>
    /// 対象の Boss へ切り替えます。
    /// </summary>
    /// <param name="bossId">切り替え先の Boss 識別名 ("Boss1" または "Boss2")</param>
    public void SwitchBoss(string bossId)
    {
        SpawnBoss(bossId);
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
            Entity bossEntity = _ecsWorld.Lookup("Boss");
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
            Entity bossEntity = _ecsWorld.Lookup("Boss");
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
        Result<ConfigBase<TileSetConfig>> tileSetConfigResult = TileSetConfig.LoadFromFile(tileMapCfg.TileConfig);
        if (tileSetConfigResult.IsOk && tileSetConfigResult.Value != null)
        {
            int sheetHandle = AssetManager.LoadTexture(tileMapCfg.TileSheet).ValueOrDefault(-1);
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
