using AnimalBattle.Ai;
using AnimalBattle.Combat.Spawners;
using AnimalBattle.Configs;
using AnimalBattle.Enemies;
using AnimalBattle.Enemies.Services;
using AnimalBattle.Features.Tutorial.Components;
using AnimalBattle.Features.Tutorial.Configs;
using AnimalBattle.Logging;
using AnimalBattle.Maps;

namespace AnimalBattle.Features.Tutorial.Spawners;

/// <summary>
///     チュートリアルシーンの全エンティティ（自機・地形・訓練目標・小怪・ボス扉）の生成と破棄を管理するスポナークラス。
/// </summary>
public static class TutorialSceneSpawner
{
    public const string RootName = "TutorialSceneRoot";

    /// <summary>
    ///     チュートリアルシーンのルートおよび必要エンティティを生成します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="playerType">選択されたプレイヤーキャラクター種別</param>
    /// <param name="initialPhase">初期フェーズ（通常は Movement、スキップ時は BossSelect）</param>
    public static void Spawn(World world, CharacterType playerType, TutorialPhase initialPhase)
    {
        Teardown(world);

        var root = world.Entity(RootName).Add<TutorialSceneTag>();
        var assets = world.Get<IAssetManager>();

        // 1. 自機エンティティの生成
        var hasPlayerHandle = assets.TryGetCharacterSpriteHandle(playerType, "Idle", out var playerHandle);
        Debug.Assert(hasPlayerHandle, "プレイヤーの初期スプライトハンドルが見つかりません。");
        if (!hasPlayerHandle) Log.Error("TutorialSceneSpawner", $"プレイヤーの初期スプライトハンドルが見つかりません: {playerType}");

        var playerCfg = world.GetPlayerConfig(playerType);
        var playerEntity = world.Entity("TutorialPlayer")
            .ChildOf(root)
            .IsA(PlayerPrefabs.GetPrefab(playerType))
            .Set(new Transform(playerCfg.Spawn, playerCfg.Scale))
            .Set(new Velocity(Vec2f.Zero))
            .Set(new Health(playerCfg.MaxHp))
            .Set(new MovementIntent())
            .Set(new WeaponIntent())
            .Set(new DashState())
            .Set(JumpCapability.FromPlayerConfig(playerCfg))
            .Set(new Animation(playerType, "Idle"))
            .Set(new Sprite(playerHandle, false, true, new Rect(0, 0, 32, 32)))
            .Set(new CharacterTypeComponent(playerType));

        var weaponCfg = world.GetWeaponConfig(playerCfg.Weapon);
        if (weaponCfg is MeleeWeaponConfig)
        {
            var melee = world.Entity("PlayerSlashHitbox")
                .ChildOf(playerEntity)
                .IsA(CombatPrefabs.PlayerSlashHitbox);
            melee.Disable();
            playerEntity.Add<EquippedWeapon>(melee);
        }

        // 2. 地形・訓練場アリーナの生成
        var arenaCfg = world.Get<ArenaConfig>();
        var tileMapCfg = arenaCfg.TileMap;
        var sheetHandle = assets.LoadTexture(tileMapCfg.TileSheetPath);
        Debug.Assert(sheetHandle >= 0, $"タイルシートが読み取れません: {tileMapCfg.TileSheetPath}");
        TileSet tileSet = new(sheetHandle, world.Get<TileSetConfig>());
        var tileMap = TileMap.FromConfig(tileMapCfg, tileSet);
        tileMap.BuildPlatformColliders(world, root);
        root.Set(tileMap);

        // 3. カメラと照準ステートのリセット
        world.Set(new AimReticleState());
        CameraService.Reset();

        // 4. チュートリアル状態の初期化
        world.Set(new TutorialState
        {
            CurrentPhase = initialPhase,
            PhaseTimer = 0f,
            TargetsRemaining = 0,
            IsPhaseCompleted = false,
            CompletionTimer = 0f,
            IsRetrying = false,
            RetryTimer = 0f
        });

        // 5. 初期フェーズに応じた目標の生成
        SetupPhase(world, initialPhase);
    }

    /// <summary>
    ///     指定されたフェーズの目標（ターゲットコイン、小怪、またはボス扉）をワールドに配置します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="phase">設定するフェーズ</param>
    public static void SetupPhase(World world, TutorialPhase phase)
    {
        var root = world.Lookup(RootName);
        if (!root.IsAlive()) return;

        ClearPhaseEntities(world);

        var cfg = world.Get<TutorialConfig>() ?? new TutorialConfig();

        ref var state = ref world.GetMut<TutorialState>();
        state.CurrentPhase = phase;
        state.PhaseTimer = 0f;
        state.IsPhaseCompleted = false;
        state.CompletionTimer = 0f;

        switch (phase)
        {
            case TutorialPhase.Movement:
                SpawnTarget(world, root, cfg.Targets.Movement.Target1Pos, cfg.Targets.Movement.Radius, phase);
                SpawnTarget(world, root, cfg.Targets.Movement.Target2Pos, cfg.Targets.Movement.Radius, phase);
                state.TargetsRemaining = 2;
                break;

            case TutorialPhase.Jump:
                SpawnTarget(world, root, cfg.Targets.Jump.Pos, cfg.Targets.Jump.Radius, phase);
                state.TargetsRemaining = 1;
                break;

            case TutorialPhase.DoubleJump:
                SpawnTarget(world, root, cfg.Targets.DoubleJump.Pos, cfg.Targets.DoubleJump.Radius, phase);
                state.TargetsRemaining = 1;
                break;

            case TutorialPhase.Dash:
                SpawnTarget(world, root, cfg.Targets.Dash.Pos, cfg.Targets.Dash.Radius, phase);
                state.TargetsRemaining = 1;
                break;

            case TutorialPhase.FlyingMinion:
                // 本編と同様に MinionSpawnService で安全距離・配置を動的に選定して生成
                var flyingSpawnPos = MinionSpawnService.SelectBestSpawnPoint(world, MinionType.Flying);
                MinionSpawner.SpawnFlyingMinion(world, root, flyingSpawnPos);
                state.TargetsRemaining = 1;
                break;

            case TutorialPhase.GroundMinion:
                // 本編と同様に MinionSpawnService で安全距離・配置を動的に選定して生成
                var groundSpawnPos = MinionSpawnService.SelectBestSpawnPoint(world, MinionType.Ground);
                MinionSpawner.SpawnGroundMinion(world, root, groundSpawnPos);
                state.TargetsRemaining = 1;
                break;

            case TutorialPhase.BossSelect:
                SpawnDoor(world, root, cfg.Doors.Snake.Pos, BossType.BossSnake, cfg.Doors.InteractionRadius);
                SpawnDoor(world, root, cfg.Doors.Lion.Pos, BossType.BossLion, cfg.Doors.InteractionRadius);
                state.TargetsRemaining = 0;
                break;
        }
    }

    /// <summary>
    ///     チュートリアル目標物（コイン／リング）を生成します。
    /// </summary>
    public static Entity SpawnTarget(World world, Entity root, Vec2f pos, float radius, TutorialPhase phase)
    {
        return world.Entity($"TutorialTarget_{phase}_{pos.X}_{pos.Y}")
            .ChildOf(root)
            .Set(new Transform(pos, Vec2f.One))
            .Set(new TutorialTarget
            {
                Radius = radius,
                IsCollected = false,
                Phase = phase,
                FloatPhase = 0f
            });
    }

    /// <summary>
    ///     ボス挑戦扉エンティティを生成します。
    /// </summary>
    public static Entity SpawnDoor(World world, Entity root, Vec2f pos, BossType bossType, float interactionRadius = 75f)
    {
        return world.Entity($"TutorialDoor_{bossType}")
            .ChildOf(root)
            .Set(new Transform(pos, Vec2f.One))
            .Set(new TutorialDoor
            {
                TargetBoss = bossType,
                InteractionRadius = interactionRadius,
                IsPlayerInRange = false,
                AnimTimer = 0f
            });
    }

    /// <summary>
    ///     チュートリアル目標物、小怪、ボス扉、および残存弾幕をクリーンアップします。
    /// </summary>
    public static void ClearPhaseEntities(World world)
    {
        // ターゲットコインの破棄
        using var targetQuery = world.Query<TutorialTarget>();
        targetQuery.Each((Entity e, ref TutorialTarget _) => e.Destruct());

        // 扉エンティティの破棄
        using var doorQuery = world.Query<TutorialDoor>();
        doorQuery.Each((Entity e, ref TutorialDoor _) => e.Destruct());

        // 雑魚敵エンティティの破棄
        using var enemyQuery = world.QueryBuilder().With<EnemyTag>().Build();
        enemyQuery.Each((Entity e) => e.Destruct());

        // 雑魚敵およびプレイヤー弾幕の回収
        BulletSpawner.ClearAllBossBullets(world);
    }

    /// <summary>
    ///     チュートリアルシーンのルートおよび全配下エンティティを一括破棄します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    public static void Teardown(World world)
    {
        var root = world.Lookup(RootName);
        if (root.IsAlive())
        {
            root.Destruct();
        }
    }
}
