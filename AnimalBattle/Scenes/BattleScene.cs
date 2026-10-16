using System.Diagnostics;
using AnimalBattle.Ai;
using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Boss2;
using AnimalBattle.Characters;
using AnimalBattle.Characters.Boss;
using AnimalBattle.Combat;
using AnimalBattle.CombatPool;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Events;
using AnimalBattle.Maps;
using AnimalBattle.Prefabs;
using AnimalBattle.Render;
using AnimalBattle.Renderer;
using Flecs.NET.Core;

namespace AnimalBattle.Scenes;

/// <summary>
///     バトル戦闘シーン。Flecs.NET ECS ワールド上で自機・首領・弾幕・タイルマップ物理・HUD 描画を統括します。
/// </summary>
public sealed class BattleScene : IDisposable {
    private const string Boss1Name = "Boss1";
    private const string Boss2Name = "Boss2";

    private World _ecsWorld = null!;
    private TileMap? _tileMap;
    private WorldRenderer _worldRenderer = null!;
    private DebugRenderer _debugRenderer = null!;
    private Query<Transform, CollisionBox> _platformQuery;
    private Query _playerBulletsQuery;
    private Query _vfxQuery;
    private bool _isDisposed;
    private bool _isActive;

    /// <summary>
    ///     タイトル・スタート画面への復帰要求コールバック。
    /// </summary>
    public Action? OnReturnToStart { get; set; }

    /// <summary>
    ///     現在アクティブな自機 Pawn。
    /// </summary>
    public PlayerPawn Player { get; private set; } = null!;

    /// <summary>
    ///     現在アクティブな自機コントローラー。
    /// </summary>
    public PlayerController PlayerController { get; private set; } = null!;

    /// <summary>
    ///     現在アクティブな首領 Agent。
    /// </summary>
    public BaseBossAgent Boss { get; private set; } = null!;

    /// <summary>
    ///     現在アクティブな Boss の種別。
    /// </summary>
    public BossType CurrentBossType { get; private set; } = BossType.Boss1;

    private string CurrentBossName => CurrentBossType switch {
        BossType.Boss1 => Boss1Name,
        BossType.Boss2 => Boss2Name,
        _ => AssertHelper.FailWithDefault("未知のBossタイプ", Boss1Name)
    };

    /// <summary>
    ///     戦闘シーンを初期化・構築します。
    /// </summary>
    /// <param name="ecsWorld">共有 ECS ワールド</param>
    /// <param name="playerType">選択されたプレイヤーキャラクター種別</param>
    /// <param name="bossType">選択された首領種別</param>
    public void Setup(World ecsWorld, CharacterType playerType, BossType bossType)
    {
        _ecsWorld = ecsWorld;
        _isActive = true;

        var root = _ecsWorld.Entity("BattleSceneRoot").Add<BattleSceneTag>();

        ConfigManager.ActiveCharacter = playerType;

        var hasPlayerHandle = AssetManager.TryGetCharacterSpriteHandle(playerType, "Idle", out var playerHandle);
        Debug.Assert(hasPlayerHandle, "プレイヤーの初期スプライトハンドルが見つかりません。");

        var playerCfg = ConfigManager.GetPlayerConfig(playerType);
        var playerPrefabKey = playerType == CharacterType.Bunny ? PrefabKey.BunnyPlayer : PrefabKey.CatPlayer;
        var playerEntity = _ecsWorld.Entity("Player1")
            .ChildOf(root)
            .IsA(PrefabRegistry.GetPrefab(playerPrefabKey));

        if (playerType == CharacterType.Bunny)
        {
            Player = new Bunny(playerEntity, playerCfg, playerHandle);
        }
        else
        {
            Player = new Cat(playerEntity, playerCfg, playerHandle);
        }
        PlayerController = new PlayerController(Player);

        SetupArena(root);

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

        CurrentBossType = bossType;
        var bossEntity = _ecsWorld.Entity(CurrentBossName)
            .ChildOf(root)
            .IsA(PrefabRegistry.GetPrefab(bossType.ToPrefabKey()));
        SpawnBoss(bossType, bossEntity);

        RegisterConfigReloadHooks();
        SoundService.PlayBgm();
    }

    private void SetupArena(Entity root)
    {
        var arenaCfg = ConfigManager.Arena;
        var tileMapCfg = arenaCfg.TileMap;

        var sheetHandle = AssetManager.LoadTexture(tileMapCfg.TileSheetPath);
        Debug.Assert(sheetHandle >= 0, $"[BattleScene 警告] {tileMapCfg.TileSheetPath}の画像は読み取りできませんでした。");
        TileSet tileSet = new(sheetHandle, ConfigManager.TileSet);
        _tileMap = TileMap.FromConfig(tileMapCfg, tileSet);
        _ecsWorld.Set(_tileMap);

        _tileMap.BuildPlatformColliders(_ecsWorld);
        Debug.WriteLine("[BattleScene] TileMap および TileSet プラットフォームコライダーが正常に初期化されました。");
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
            _ => AssertHelper.FailWithDefault($"[BattleScene 警告] 未知のBossType (cfg): {bossType}", ConfigManager.Boss1)
        };

        Boss = bossType switch {
            BossType.Boss1 => new Boss1Agent(bossEntity, handle),
            BossType.Boss2 => new Boss2Agent(bossEntity, handle, () => _tileMap?.NavGraph),
            _ => AssertHelper.FailWithDefault($"[BattleScene 警告] 未知のBossType (Agent): {bossType}", new Boss1Agent(bossEntity, handle))
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

    private void SwitchBoss(BossType bossType)
    {
        var otherBoss = _ecsWorld.Lookup(CurrentBossName);
        if (otherBoss.IsAlive()) otherBoss.Destruct();

        CurrentBossType = bossType;
        var root = _ecsWorld.Lookup("BattleSceneRoot");
        var bossEntity = _ecsWorld.Entity(CurrentBossName)
            .IsA(PrefabRegistry.GetPrefab(bossType.ToPrefabKey()));
        if (root.IsAlive()) bossEntity.ChildOf(root);

        SpawnBoss(bossType, bossEntity);
    }

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

        ConfigManager.ActiveCharacter = newChar;
        var pCfg = ConfigManager.GetPlayerConfig(newChar);
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

        Debug.WriteLine($"[BattleScene] プレイヤーキャラクターを切り替えました: {newChar} (Speed: {pCfg.MoveSpeed}, Jump: {pCfg.FirstJumpVelocity})");
    }

    private void RegisterConfigReloadHooks()
    {
        ConfigManager.Boss1Container.OnConfigReloaded += OnBoss1ConfigReloaded;
        ConfigManager.Boss2Container.OnConfigReloaded += OnBoss2ConfigReloaded;
        ConfigManager.CatContainer.OnConfigReloaded += OnCatConfigReloaded;
        ConfigManager.BunnyContainer.OnConfigReloaded += OnBunnyConfigReloaded;
        ConfigManager.CombatPrefabsContainer.OnConfigReloaded += OnCombatPrefabsReloaded;
    }

    private void UnregisterConfigReloadHooks()
    {
        ConfigManager.Boss1Container.OnConfigReloaded -= OnBoss1ConfigReloaded;
        ConfigManager.Boss2Container.OnConfigReloaded -= OnBoss2ConfigReloaded;
        ConfigManager.CatContainer.OnConfigReloaded -= OnCatConfigReloaded;
        ConfigManager.BunnyContainer.OnConfigReloaded -= OnBunnyConfigReloaded;
        ConfigManager.CombatPrefabsContainer.OnConfigReloaded -= OnCombatPrefabsReloaded;
    }

    private void OnCombatPrefabsReloaded()
    {
        PrefabRegistry.ReloadPrefabs();
        Debug.WriteLine("[BattleScene] CombatPrefabs のホットリロードを完了しました");
    }

    private void OnBoss1ConfigReloaded() => OnBossConfigReloaded(BossType.Boss1);
    private void OnBoss2ConfigReloaded() => OnBossConfigReloaded(BossType.Boss2);

    private void OnBossConfigReloaded(BossType bossType)
    {
        if (CurrentBossType != bossType) return;

        var (bossName, bCfg) = bossType switch {
            BossType.Boss1 => (Boss1Name, ConfigManager.Boss1),
            BossType.Boss2 => (Boss2Name, ConfigManager.Boss2),
            _ => AssertHelper.FailWithDefault($"[BattleScene 警告] 未知のBossType (Reload): {bossType}", (Boss1Name, (BaseBossConfig)ConfigManager.Boss1))
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

    private void OnCatConfigReloaded()
    {
        if (Player is Cat) ApplyPlayerConfig(ConfigManager.Cat);
    }

    private void OnBunnyConfigReloaded()
    {
        if (Player is Bunny) ApplyPlayerConfig(ConfigManager.Bunny);
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

        Debug.WriteLine($"[BattleScene] Player ({Player.CharacterType}) の設定をリアルタイム同期しました");
    }

    /// <summary>
    ///     戦闘フレームの入力および意思決定・動的カメラ更新を行います。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間秒数</param>
    public void Update(float deltaTime)
    {
        if (!_isActive) return;

        HandleInput();

        PlayerController.PollAndProcessInput();
        Boss.Update(deltaTime, new Vec2f(Player.X, Player.Y), new Vec2f(Player.Vx, Player.Vy), _ecsWorld, _platformQuery);

        CameraService.Update(deltaTime, Player.X, Player.Y, Player.Vx, Player.Vy, Boss.X, Boss.Y);
        AimReticleRenderer.Update(deltaTime);
    }

    private void HandleInput()
    {
        // ESC キーでタイトル／スタート画面へ復帰
        if (InputService.IsPressed(InputAction.Cancel))
        {
            OnReturnToStart?.Invoke();
            return;
        }

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
            Boss.Reset();
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
    ///     戦闘シーンの描画処理を行います。
    /// </summary>
    public void Render()
    {
        if (!_isActive) return;

        GameEngine.SetCameraOffset(CameraService.RenderOffsetX, CameraService.RenderOffsetY);
        _worldRenderer.Render(_tileMap, _platformQuery, Player, Boss);
        _debugRenderer.Render(_platformQuery, _tileMap);
        GameEngine.SetCameraOffset(0, 0);
        HudRenderer.Render(Player, Boss, Player.RemainingJumps);
        AimReticleRenderer.Render();
    }

    /// <summary>
    ///     戦闘シーンを終了し、環境エンティティとリソースを安全にクリーンアップします。
    /// </summary>
    public void Teardown()
    {
        if (!_isActive) return;
        _isActive = false;

        UnregisterConfigReloadHooks();
        SoundService.StopBgm();
        InputService.SetCursorVisible(true);
        AimReticleRenderer.Shutdown();

        // 弾幕と VFX を回収
        BulletSpawner.ClearAllBossBullets(_ecsWorld);
        _playerBulletsQuery.Each(e => e.Add<RecycleTag>());
        _vfxQuery.Each(e => e.Add<RecycleTag>());
        EventBus.Publish(new CombatResetEvent());

        _worldRenderer.Dispose();
        _debugRenderer.Dispose();
        _playerBulletsQuery.Dispose();
        _vfxQuery.Dispose();
        _platformQuery.Dispose();

        // Flecs 級聯廃棄で戦闘ルート配下の全エンティティ（自機、首領、足場等）を一括消滅
        var root = _ecsWorld.Lookup("BattleSceneRoot");
        if (root.IsAlive())
        {
            root.Destruct();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed) return;
        Teardown();
        _isDisposed = true;
    }
}
