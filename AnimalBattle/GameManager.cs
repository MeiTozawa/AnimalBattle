using AnimalBattle.Ai;
using AnimalBattle.Characters;
using AnimalBattle.Characters.Boss;
using AnimalBattle.Events;
using AnimalBattle.Maps;
using AnimalBattle.Prefabs;
using Flecs.NET.Core;

namespace AnimalBattle;

/// <summary>
///     Flecs.NET ワールドおよび多 Boss (Boss 1 / Boss 2) 戦パイプラインを統括するバトルシーンクラス。
///     TOML 設定ファイル（player.toml, boss1.toml, boss2.toml, arena.toml）から初期パラメータをロードし、
///     プレイヤー、Boss、プラットフォームへ Bevy スタイルの Hitbox / Hurtbox を付与します。
/// </summary>
public class GameManager : IDisposable {
    private readonly DebugRenderer _debugRenderer;
    private readonly WorldRenderer _worldRenderer;
    private const string Boss1Name =  "Boss1";
    private const string Boss2Name =  "Boss2";
    private World _ecsWorld;
    private Query _vfxQuery;
    private bool _isDisposed;
    private Query<Transform, CollisionBox> _platformQuery;
    private Query _playerBulletsQuery;
    private TileMap? _tileMap;

    /// <summary>
    ///     <see cref="GameManager" /> の新しいインスタンスを初期化し、全 ECS パイプラインを登録します。
    /// </summary>
    public GameManager()
    {
        _ecsWorld = World.Create();
        Current = this;

        ConfigManager.Initialize();
        AssetManager.Initialize();

        var hasPlayerHandle = AssetManager.TryGetCharacterSpriteHandle(CharacterType.Cat, "Idle", out var playerHandle);
        Debug.Assert(hasPlayerHandle, "プレイヤーの初期スプライトハンドルが見つかりません。");

        _ecsWorld.Import<CombatFeedbackModule>();
        _ecsWorld.Import<AnimationModule>();
        _ecsWorld.Import<CombatModule>();
        _ecsWorld.Import<BulletModule>();
        _ecsWorld.Import<DamageResolutionModule>();
        _ecsWorld.Import<EffectModule>();

        PrefabRegistry.Initialize(_ecsWorld);
        CombatPoolService.Initialize(_ecsWorld);

        var playerCfg = ConfigManager.Cat;

        var playerEntity = _ecsWorld.Entity("Player1").IsA(PrefabRegistry.GetPrefab(PrefabKey.CatPlayer));
        Player = new Cat(playerEntity, playerCfg, playerHandle);
        PlayerController = new PlayerController(Player);

        SetupArena();

        _platformQuery = _ecsWorld.QueryBuilder<Transform, CollisionBox>()
           .With<PlatformTag>()
           .Build();

        _worldRenderer = new WorldRenderer(_ecsWorld);
        _debugRenderer = new DebugRenderer(_ecsWorld);
        _playerBulletsQuery = _ecsWorld.QueryBuilder()
           .With<PlayerBulletTag>()
           .Build();
        _vfxQuery = _ecsWorld.QueryBuilder()
           .With<VfxTag>()
           .Build();

        InputService.SetCursorVisible(false);
        AimReticleRenderer.Initialize();

        SwitchBoss(CurrentBossType);
    }

    /// <summary>
    ///     現在アクティブな GameManager インスタンスを取得します。
    /// </summary>
    public static GameManager Current { get; private set; } = null!;

    /// <summary>
    ///     現在アクティブなプレイヤー自機インスタンスを取得します。
    /// </summary>
    public PlayerPawn Player { get; private set; } = null!;

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
        ConfigManager.CatContainer.OnConfigReloaded -= OnCatConfigReloaded;
        ConfigManager.BunnyContainer.OnConfigReloaded -= OnBunnyConfigReloaded;
        ConfigManager.CombatPrefabsContainer.OnConfigReloaded -= OnCombatPrefabsReloaded;

        CombatPoolService.Current.Dispose();

        _worldRenderer.Dispose();
        _debugRenderer.Dispose();
        _playerBulletsQuery.Dispose();
        _vfxQuery.Dispose();
        _platformQuery.Dispose();
        _ecsWorld.Dispose();
        SoundService.StopBgm();
        InputService.SetCursorVisible(true);
        AimReticleRenderer.Shutdown();
        EventBus.Publish(new CombatResetEvent());
        AssetManager.Shutdown();
        if (Current == this) Current = null!;
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    private void SpawnBoss(BossType bossType, Entity bossEntity)
    {
        var (texPath, bossTitle) = bossType switch {
            BossType.Boss1 => (ResourcePath.Characters.Character031_3Png, "Boss 1"),
            BossType.Boss2 => (ResourcePath.Characters.Character027_3Png, "Boss 2"),
            _ => AssertHelper.FailWithDefault("未知のBossType", (ResourcePath.Characters.Character031_3Png, "Boss 1"))
        };

        var hasHandle = AssetManager.TryGetTexture(texPath, out var handle);
        Debug.Assert(hasHandle, $"Boss テクスチャがロードされていません: {texPath}");

        BaseBossConfig cfg = bossType switch {
            BossType.Boss1 => ConfigManager.Boss1,
            BossType.Boss2 => ConfigManager.Boss2,
            _ => AssertHelper.FailWithDefault($"[GameManager 警告] 未知のBossType (cfg): {bossType}", ConfigManager.Boss1)
        };

        Boss = bossType switch {
            BossType.Boss1 => new Boss1Agent(bossEntity, handle),
            BossType.Boss2 => new Boss2Agent(bossEntity, handle, () => _tileMap?.NavGraph),
            _ => AssertHelper.FailWithDefault($"[GameManager 警告] 未知のBossType (Agent): {bossType}", new Boss1Agent(bossEntity, handle))
        };

        Boss.AbilitySystem.OnScreenShake += (intensity, duration) =>
        {
            var camCfg = ConfigManager.Camera;
            var trauma = intensity > 1.0f ? intensity * camCfg.AbilityShakeTraumaScale : intensity;
            CameraService.AddTrauma(trauma);
        };

        Debug.WriteLine(
            $"[GameManager] {bossTitle} を生成・リセットしました (HP: {cfg.MaxHp}, Pos: {cfg.SpawnX}, {cfg.SpawnY})");
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
        var bossEntity = _ecsWorld.Entity(CurrentBossName).IsA(PrefabRegistry.GetPrefab(bossType.ToPrefabKey()));
        SpawnBoss(bossType, bossEntity);
    }

    /// <summary>
    ///     TOML 設定ファイル変更時のホットリロードコールバックを登録し、Entity の衝突判定・スケールを即時再同期します。
    /// </summary>
    private void RegisterConfigReloadHooks()
    {
        ConfigManager.Boss1Container.OnConfigReloaded += OnBoss1ConfigReloaded;
        ConfigManager.Boss2Container.OnConfigReloaded += OnBoss2ConfigReloaded;
        ConfigManager.CatContainer.OnConfigReloaded += OnCatConfigReloaded;
        ConfigManager.BunnyContainer.OnConfigReloaded += OnBunnyConfigReloaded;
        ConfigManager.CombatPrefabsContainer.OnConfigReloaded += OnCombatPrefabsReloaded;
    }

    private void OnCombatPrefabsReloaded()
    {
        PrefabRegistry.ReloadPrefabs();
        Debug.WriteLine("[GameManager] CombatPrefabs のホットリロードを完了しました");
    }

    private void OnBoss1ConfigReloaded() => OnBossConfigReloaded(BossType.Boss1);
    private void OnBoss2ConfigReloaded() => OnBossConfigReloaded(BossType.Boss2);

    private void OnBossConfigReloaded(BossType bossType)
    {
        if (CurrentBossType != bossType) return;

        var (bossName, bCfg) = bossType switch {
            BossType.Boss1 => (Boss1Name, ConfigManager.Boss1),
            BossType.Boss2 => (Boss2Name, ConfigManager.Boss2),
            _ => AssertHelper.FailWithDefault($"[GameManager 警告] 未知のBossType (Reload): {bossType}", (Boss1Name, (BaseBossConfig)ConfigManager.Boss1))
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

        Debug.WriteLine($"[GameManager] {bossName} の Hurtbox / Hitbox / Scale を TOML からリアルタイム同期しました");
    }

    private void OnCatConfigReloaded()
    {
        if (Player is Cat)
        {
            ApplyPlayerConfig(ConfigManager.Cat);
        }
    }

    private void OnBunnyConfigReloaded()
    {
        if (Player is Bunny)
        {
            ApplyPlayerConfig(ConfigManager.Bunny);
        }
    }

    /// <summary>
    ///     プレイヤー自機キャラクター（Cat または Bunny）を動的に切り替えます。
    /// </summary>
    /// <param name="newChar">切り替え先のキャラクター種別。</param>
    public void SwitchPlayer(CharacterType newChar)
    {
        if (Player.CharacterType == newChar) return;

        if (Player is Bunny oldBunny && oldBunny.MeleeAttackEntity.IsAlive())
        {
            oldBunny.MeleeAttackEntity.Disable();
        }

        var playerEntity = Player.Entity;
        var oldPrefab = Player.CharacterType == CharacterType.Bunny
            ? PrefabRegistry.GetPrefab(PrefabKey.BunnyPlayer)
            : PrefabRegistry.GetPrefab(PrefabKey.CatPlayer);
        var newPrefab = PrefabRegistry.GetPrefab(newChar.ToPrefabKey());

        playerEntity.Remove(oldPrefab).IsA(newPrefab);

        var pCfg = newChar == CharacterType.Cat ? ConfigManager.Cat : ConfigManager.Bunny;
        AssetManager.TryGetCharacterSpriteHandle(newChar, "Idle", out var handle);

        if (newChar == CharacterType.Cat)
        {
            playerEntity.Remove<BunnyWeaponState>();
            playerEntity.Remove<PlayerMeleeWeapon>();
            Player = new Cat(playerEntity, pCfg, handle);
        }
        else
        {
            playerEntity.Remove<CatWeaponState>();
            Player = new Bunny(playerEntity, pCfg, handle);
        }

        PlayerController.Pawn = Player;

        ref var anim = ref playerEntity.GetMut<Animation>();
        anim.CharacterType = newChar;
        anim.Play("Idle");

        ref var sprite = ref playerEntity.GetMut<Sprite>();
        sprite.GraphHandle = handle;
        sprite.SrcRect = new Rect(0, 0, 32, 32);

        playerEntity.Set(JumpCapability.FromPlayerConfig(pCfg));
        playerEntity.Set(new Hurtbox(pCfg.GetHurtboxShape()));
        playerEntity.Set(new CollisionBox(pCfg.CollisionWidth, pCfg.CollisionHeight));

        ref var hp = ref playerEntity.GetMut<Health>();
        var oldMax = hp.Max;
        hp.Max = pCfg.MaxHp;
        if (hp.Current >= oldMax)
        {
            hp.Current = pCfg.MaxHp;
        }
        else
        {
            hp.Current = Math.Clamp(hp.Current, 0.0f, hp.Max);
        }

        ref var t = ref playerEntity.GetMut<Transform>();
        t.ScaleX = pCfg.ScaleX;
        t.ScaleY = pCfg.ScaleY;

        Debug.WriteLine($"[GameManager] プレイヤーキャラクターを切り替えました: {newChar} (Speed: {pCfg.MoveSpeed}, Jump: {pCfg.FirstJumpVelocity})");
    }

    private void ApplyPlayerConfig(PlayerConfig pCfg)
    {
        var playerEntity = _ecsWorld.Lookup("Player1");
        if (!playerEntity.IsAlive()) return;
        playerEntity.Set(new Hurtbox(pCfg.GetHurtboxShape()));
        playerEntity.Set(new CollisionBox(pCfg.CollisionWidth, pCfg.CollisionHeight));

        ref var t = ref playerEntity.GetMut<Transform>();
        t.ScaleX = pCfg.ScaleX;
        t.ScaleY = pCfg.ScaleY;

        playerEntity.Set(JumpCapability.FromPlayerConfig(pCfg));

        if (Player is Bunny)
        {
            playerEntity.Set(BunnyWeaponState.FromConfig(pCfg));
        }
        else
        {
            playerEntity.Set(CatWeaponState.FromConfig(pCfg));
        }

        Debug.WriteLine($"[GameManager] Player ({Player.CharacterType}) の Hurtbox / CollisionBox / Scale / Weapon / Jump を TOML からリアルタイム同期しました");
    }

    /// <summary>
    ///     arena.toml の TileMap 設定および TileSet の衝突領域に基づいてプラットフォームコライダーを生成します。
    /// </summary>
    private void SetupArena()
    {
        var arenaCfg = ConfigManager.Arena;
        var tileMapCfg = arenaCfg.TileMap;

        var sheetHandle = AssetManager.LoadTexture(tileMapCfg.TileSheetPath);
        Debug.Assert(sheetHandle >= 0, $"[GameManager 警告] {tileMapCfg.TileSheetPath}の画像は読み取りできませんでした。");
        TileSet tileSet = new(sheetHandle, ConfigManager.TileSet);
        _tileMap = TileMap.FromConfig(tileMapCfg, tileSet);
        _ecsWorld.Set(_tileMap);

        _tileMap.BuildPlatformColliders(_ecsWorld);
        Debug.WriteLine("[GameManager] TileMap および TileSet プラットフォームコライダーが正常に初期化されました。");
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

        // 4. マウス照準レティクル（准星）のスプリング反動拡縮更新
        AimReticleRenderer.Update(deltaTime);
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
        else if (InputService.IsPressed(InputAction.SelectCharacter1))
        {
            SwitchPlayer(CharacterType.Cat);
        }
        else if (InputService.IsPressed(InputAction.SelectCharacter2))
        {
            SwitchPlayer(CharacterType.Bunny);
        }
        else if (InputService.IsPressed(InputAction.Restart))
        {
            ResetCurrentBoss();
            ResetCombat();
        }
    }

    private void ResetCombat()
    {
        BulletSpawner.ClearAllBossBullets(_ecsWorld);
        _playerBulletsQuery.Each(e => e.Add<RecycleTag>());
        _vfxQuery.Each(e => e.Add<RecycleTag>());
        CameraService.Reset();
        AimReticleRenderer.Reset();
        Player.Reset(ConfigManager.GetPlayerConfig(Player.CurrentCharacter));
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
        HudRenderer.Render(Player, Boss, Player.RemainingJumps);
        AimReticleRenderer.Render();
    }
}