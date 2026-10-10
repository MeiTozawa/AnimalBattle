using System.Diagnostics;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Characters;
using AnimalBattle.Characters.Boss;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Events;
using AnimalBattle.Maps;
using Flecs.NET.Core;

namespace AnimalBattle.Scenes;

/// <summary>
///     Flecs.NET ワールドおよび多 Boss (Boss 1 / Boss 2) 戦パイプラインを統括するバトルシーンクラス。
///     TOML 設定ファイル（player.toml, boss1.toml, boss2.toml, arena.toml）から初期パラメータをロードし、
///     プレイヤー、Boss、プラットフォームへ Bevy スタイルの Hitbox / Hurtbox を付与します。
/// </summary>
public class BattleScene : IDisposable {
    private readonly DebugRenderSystem _debugRenderer;
    private readonly WorldRenderSystem _worldRenderer;
    private const string Boss1Name =  "Boss1";
    private const string Boss2Name =  "Boss2";
    private World _ecsWorld;
    private Query _vfxQuery;
    private bool _isDisposed;
    private Query<Transform, CollisionBox> _platformQuery;
    private Query _playerBulletsQuery;
    private TileMap? _tileMap;

    /// <summary>
    ///     <see cref="BattleScene" /> の新しいインスタンスを初期化し、全 ECS パイプラインを登録します。
    /// </summary>
    public BattleScene()
    {
        _ecsWorld = World.Create();
        Current = this;

        ConfigManager.Initialize();
        AssetManager.Initialize();

        var hasPlayerHandle = AssetManager.TryGetCharacterSpriteHandle(CharacterType.Cat, "Idle", out var playerHandle);
        Debug.Assert(hasPlayerHandle, "プレイヤーの初期スプライトハンドルが見つかりません。");

        _ecsWorld.Import<ComponentRequirementsModule>();
        _ecsWorld.Import<CombatFeedbackModule>();
        _ecsWorld.Import<AnimationModule>();
        _ecsWorld.Import<CombatModule>();
        _ecsWorld.Import<BulletModule>();
        _ecsWorld.Import<DamageResolutionModule>();
        _ecsWorld.Import<EffectModule>();

        // 共有戦闘エンティティプールを事前確保・初期化（初弾発射時の遅延生成による Stage 競合および破棄漏れを根絶）
        _ecsWorld.GetCombatEntityPool();

        var playerCfg = ConfigManager.Player;

        var playerEntity = _ecsWorld.Entity("Player1");
        Player = new PlayerPawn(playerEntity, playerCfg, playerHandle);
        PlayerController = new PlayerController(Player);

        SetupArena();

        _platformQuery = _ecsWorld.QueryBuilder<Transform, CollisionBox>()
           .With<PlatformTag>()
           .Build();

        _worldRenderer = new WorldRenderSystem(_ecsWorld);
        _debugRenderer = new DebugRenderSystem(_ecsWorld);
        _playerBulletsQuery = _ecsWorld.QueryBuilder()
           .With<PlayerBulletTag>()
           .Build();
        _vfxQuery = _ecsWorld.QueryBuilder()
           .With<VfxTag>()
           .Build();

        // 3. アクティブな Boss (デフォルト: Boss 1) の生成
        SwitchBoss(CurrentBossType);
    }

    /// <summary>
    ///     現在アクティブな BattleScene インスタンスを取得します。
    /// </summary>
    public static BattleScene Current { get; private set; } = null!;

    /// <summary>
    ///     現在アクティブなプレイヤー自機インスタンスを取得します。
    /// </summary>
    public PlayerPawn Player { get; }

    /// <summary>
    ///     現在アクティブなプレイヤーコントローラーを取得します。
    /// </summary>
    public PlayerController PlayerController { get; }

    /// <summary>
    ///     現在アクティブな Boss インスタンスを取得します。
    /// </summary>
    public BaseBossAgent Boss { get; private set; } = null!;

    /// <summary>
    ///     現在アクティブな Boss の種別を取得または設定します。
    /// </summary>
    private BossType CurrentBossType { get; set; } = BossType.Boss1;

    /// <summary>
    ///     現在アクティブな Boss の識別名を取得します。
    /// </summary>
    private string CurrentBossName => CurrentBossType switch {
        BossType.Boss1 => Boss1Name,
        BossType.Boss2 => Boss2Name,
        _ => AssertHelper.FailWithDefault("未知のBossタイプ", Boss1Name)
    };

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed) return;
        ConfigManager.Boss1Container.OnConfigReloaded -= OnBoss1ConfigReloaded;
        ConfigManager.Boss2Container.OnConfigReloaded -= OnBoss2ConfigReloaded;
        ConfigManager.PlayerContainer.OnConfigReloaded -= OnPlayerConfigReloaded;

        if (_ecsWorld.Has<CombatEntityPoolComponent>()) _ecsWorld.Get<CombatEntityPoolComponent>().Dispose();

        _worldRenderer.Dispose();
        _debugRenderer.Dispose();
        _playerBulletsQuery.Dispose();
        _vfxQuery.Dispose();
        _platformQuery.Dispose();
        _ecsWorld.Dispose();
        SoundService.StopBgm();
        EventBus.Publish(new CombatResetEvent());
        AssetManager.Shutdown();
        if (Current == this) Current = null!;
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    private void SpawnBoss(BossType bossType, Entity bossEntity)
    {
        var (texPath, bossTitle) = bossType switch {
            BossType.Boss1 => (ResourcePath.Characters.Character031_3Png, "Boss 1 (Ancient Cat Overlord)"),
            BossType.Boss2 => (ResourcePath.Characters.Character027_3Png, "Boss 2 (Ground Crusher)"),
            _ => AssertHelper.FailWithDefault("未知のBossType", (ResourcePath.Characters.Character031_3Png, "Boss 1"))
        };

        var hasHandle = AssetManager.TryGetTexture(texPath, out var handle);
        Debug.Assert(hasHandle, $"Boss テクスチャがロードされていません: {texPath}");

        BaseBossConfig cfg = bossType switch {
            BossType.Boss1 => ConfigManager.Boss1,
            BossType.Boss2 => ConfigManager.Boss2,
            _ => ConfigManager.Boss1
        };

        Boss = bossType switch {
            BossType.Boss1 => new Boss1Agent(bossEntity, handle),
            BossType.Boss2 => new Boss2Agent(bossEntity, handle, () => _tileMap?.NavGraph),
            _ => new Boss1Agent(bossEntity, handle)
        };

        Boss.AbilitySystem.OnScreenShake += (intensity, duration) =>
        {
            var camCfg = ConfigManager.Camera;
            var trauma = intensity > 1.0f ? intensity * camCfg.AbilityShakeTraumaScale : intensity;
            CameraService.AddTrauma(trauma);
        };

        Debug.WriteLine(
            $"[BattleScene] {bossTitle} を生成・リセットしました (HP: {cfg.MaxHp}, Pos: {cfg.SpawnX}, {cfg.SpawnY})");
    }

    /// <summary>
    ///     シーンの追加初期化処理（設定ホットリロードフックの登録等）を行います。
    /// </summary>
    public void Initialize()
    {
        RegisterConfigReloadHooks();
        SoundService.PlayBgm();
    }

    /// <summary>
    ///     現在の Boss を初期状態にリセットして再生成します。
    /// </summary>
    private void ResetCurrentBoss()
    {
        Boss.Reset();
    }

    /// <summary>
    ///     対象の Boss へ切り替えます。
    /// </summary>
    /// <param name="bossType">切り替え先の Boss 種別</param>
    private void SwitchBoss(BossType bossType)
    {
        var otherBoss = _ecsWorld.Lookup(CurrentBossName);
        if (otherBoss.IsAlive()) otherBoss.Destruct();

        CurrentBossType = bossType;
        var bossEntity = _ecsWorld.Entity(CurrentBossName);
        SpawnBoss(bossType, bossEntity);
    }

    /// <summary>
    ///     TOML 設定ファイル変更時のホットリロードコールバックを登録し、Entity の衝突判定・スケールを即時再同期します。
    /// </summary>
    private void RegisterConfigReloadHooks()
    {
        ConfigManager.Boss1Container.OnConfigReloaded += OnBoss1ConfigReloaded;
        ConfigManager.Boss2Container.OnConfigReloaded += OnBoss2ConfigReloaded;
        ConfigManager.PlayerContainer.OnConfigReloaded += OnPlayerConfigReloaded;
    }

    private void OnBoss1ConfigReloaded() => OnBossConfigReloaded(BossType.Boss1);
    private void OnBoss2ConfigReloaded() => OnBossConfigReloaded(BossType.Boss2);

    private void OnBossConfigReloaded(BossType bossType)
    {
        if (CurrentBossType != bossType) return;

        var (bossName, bCfg) = bossType switch {
            BossType.Boss1 => (Boss1Name, (BaseBossConfig)ConfigManager.Boss1),
            BossType.Boss2 => (Boss2Name, (BaseBossConfig)ConfigManager.Boss2),
            _ => (Boss1Name, (BaseBossConfig)ConfigManager.Boss1)
        };

        var bossEntity = _ecsWorld.Lookup(bossName);
        if (!bossEntity.IsAlive()) return;

        bossEntity.Set(new Hurtbox(bCfg.GetHurtboxShape(), Faction.Hostile));
        bossEntity.Set(new Hitbox(bCfg.GetHitboxShape(), bCfg.BodyContactDamage, Faction.Hostile, false));

        if (bossType == BossType.Boss2)
        {
            var b2Cfg = ConfigManager.Boss2;
            bossEntity.Set(new CollisionBox(b2Cfg.CollisionWidth, b2Cfg.CollisionHeight));
        }

        if (bossEntity.Has<Transform>())
        {
            ref var t = ref bossEntity.GetMut<Transform>();
            t.ScaleX = bCfg.ScaleX;
            t.ScaleY = bCfg.ScaleY;
        }

        Debug.WriteLine($"[BattleScene] {bossName} の Hurtbox / Hitbox / Scale を TOML からリアルタイム同期しました");
    }

    private void OnPlayerConfigReloaded()
    {
        var playerEntity = _ecsWorld.Lookup("Player1");
        if (!playerEntity.IsAlive()) return;
        var pCfg = ConfigManager.Player;
        playerEntity.Set(new Hurtbox(pCfg.GetHurtboxShape()));
        playerEntity.Set(new CollisionBox(pCfg.HitboxWidth, pCfg.HitboxHeight));

        if (playerEntity.Has<Transform>())
        {
            ref var t = ref playerEntity.GetMut<Transform>();
            t.ScaleX = pCfg.ScaleX;
            t.ScaleY = pCfg.ScaleY;
        }

        Debug.WriteLine("[BattleScene] Player の Hurtbox / CollisionBox / Scale を TOML からリアルタイム同期しました");
    }

    /// <summary>
    ///     arena.toml の TileMap 設定および TileSet の衝突領域に基づいてプラットフォームコライダーを生成します。
    /// </summary>
    private void SetupArena()
    {
        var arenaCfg = ConfigManager.Arena;
        var tileMapCfg = arenaCfg.TileMap;

        var sheetHandleRes = AssetManager.LoadTexture(tileMapCfg.TileSheetPath);
        Debug.Assert(!sheetHandleRes.IsError, $"[BattleScene 警告] {tileMapCfg.TileSheetPath}の画像は読み取りできませんでした。");
        var sheetHandle = sheetHandleRes.Value;
        TileSet tileSet = new(sheetHandle, ConfigManager.TileSet);
        _tileMap = TileMap.FromConfig(tileMapCfg, tileSet);
        _ecsWorld.Set(_tileMap);

        _tileMap.BuildPlatformColliders(_ecsWorld);
        Debug.WriteLine("[BattleScene] TileMap および TileSet プラットフォームコライダーが正常に初期化されました。");
    }

    /// <summary>
    ///     シーンのフレーム更新処理を行います。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間秒数</param>
    public void Update(float deltaTime)
    {
        HandleInput();

        // 1. コントローラー意思決定フェーズ: ハードウェア入力および AI 状態機を解釈し Intent と Commands を注入
        PlayerController.PollAndProcessInput();
        Boss.Update(deltaTime, new Vec2f(Player.X, Player.Y), new Vec2f(Player.Vx, Player.Vy), _ecsWorld, _platformQuery);

        // 2. ECS 全量シミュレーションパイプライン（意図消費 -> 物理積分 -> 武器冷却 -> 衝突判定 -> ダメージ裁定 -> アニメーション歩進）
        _ecsWorld.Progress(deltaTime);

        // 3. カメラシェイク・振動減衰および動的構図更新
        CameraService.Update(deltaTime, Player.X, Player.Y, Player.Vx, Player.Vy, Boss.X, Boss.Y);
    }

    private void HandleInput()
    {
        if (InputService.IsPressed(InputAction.SelectBoss1))
        {
            SwitchBoss(BossType.Boss1);
            ResetCombat();
        }
        else if (InputService.IsPressed(InputAction.SelectBoss2))
        {
            SwitchBoss(BossType.Boss2);
            ResetCombat();
        }
        else if (InputService.IsPressed(InputAction.Restart))
        {
            ResetCurrentBoss();
            ResetCombat();
        }
    }

    private void ResetCombat()
    {
        CombatEffectHelper.ClearAllBossBullets(_ecsWorld);
        _playerBulletsQuery.Each(e => CombatEntityPoolHelper.Recycle(_ecsWorld, e));
        _vfxQuery.Each(e => CombatEntityPoolHelper.Recycle(_ecsWorld, e));
        CameraService.Reset();
        Player.Reset(ConfigManager.Player);
        EventBus.Publish(new CombatResetEvent());
    }

    /// <summary>
    ///     シーンの描画処理を行います。
    /// </summary>
    public void Render()
    {
        GameEngine.SetCameraOffset(CameraService.RenderOffsetX, CameraService.RenderOffsetY);
        _worldRenderer.Render(_tileMap, _platformQuery);
        _debugRenderer.Render(_platformQuery, _tileMap);
        GameEngine.SetCameraOffset(0, 0);
        HudRenderSystem.Render(Player, Boss, Player.RemainingJumps);
    }
}