using AnimalBattle.Features.Tutorial.Components;
using AnimalBattle.Features.Tutorial.Configs;
using AnimalBattle.Features.Tutorial.Handlers;
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
    /// <param name="initialPhaseIndex">初期フェーズ番号（負数の場合はボス扉フェーズを自動検索）</param>
    public static void Spawn(World world, CharacterType playerType, int initialPhaseIndex = 0)
    {
        Teardown(world);

        var root = world.Entity(RootName);
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

        // 3. カメラと照準ステートのリセット、OS カーソルの非表示化
        world.Set(new AimReticleState());
        CameraService.Reset();
        InputService.SetCursorVisible(false);

        // 4. 初期フェーズ番号の解決（負値の場合はボス扉フェーズを検索）
        var cfg = world.Get<TutorialConfig>();
        if (initialPhaseIndex < 0)
        {
            initialPhaseIndex = 0;
            for (var i = 0; i < cfg.Phases.Length; i++)
            {
                if (cfg.Phases[i].Type == TutorialPhaseType.BossDoor)
                {
                    initialPhaseIndex = i;
                    break;
                }
            }
        }

        // 5. チュートリアル状態の初期化
        world.Set(new TutorialState
        {
            CurrentPhaseIndex = initialPhaseIndex,
            TargetsRemaining = 0,
            IsPhaseCompleted = false,
            CompletionTimer = 0f,
            IsRetrying = false,
            RetryTimer = 0f
        });

        // 6. 初期フェーズ目標の生成
        SetupPhase(world, initialPhaseIndex);
    }

    /// <summary>
    ///     指定されたフェーズ番号の目標（ターゲットコイン、小怪、またはボス扉）をワールドに配置します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="phaseIndex">設定するフェーズ番号</param>
    public static void SetupPhase(World world, int phaseIndex)
    {
        var root = world.Lookup(RootName);
        if (!root.IsAlive()) return;

        ClearPhaseEntities(world);

        var cfg = world.Get<TutorialConfig>();

        ref var state = ref world.GetMut<TutorialState>();
        state.CurrentPhaseIndex = phaseIndex;
        state.IsPhaseCompleted = false;
        state.CompletionTimer = 0f;

        if ((uint)phaseIndex < (uint)cfg.Phases.Length)
        {
            var phaseCfg = cfg.Phases[phaseIndex];
            var handler = TutorialPhaseDispatcher.GetHandler(phaseCfg.Type);
            handler.Setup(world, root, phaseCfg, cfg);
        }
    }

    /// <summary>
    ///     現在のフェーズの目標・敵・扉をクリーンアップし再配置（リトライ）します。
    /// </summary>
    public static void ResetCurrentPhase(World world)
    {
        ref var state = ref world.GetMut<TutorialState>();
        SetupPhase(world, state.CurrentPhaseIndex);
    }

    /// <summary>
    ///     チュートリアル目標物、小怪、ボス扉、および残存弾幕をクリーンアップします。
    /// </summary>
    public static void ClearPhaseEntities(World world)
    {
        // ターゲットコインの破棄
        using var targetQuery = world.Query<TutorialTarget>();
        targetQuery.Each((e, ref _) => e.Destruct());

        // 扉エンティティの破棄
        using var doorQuery = world.Query<TutorialDoor>();
        doorQuery.Each((e, ref _) => e.Destruct());

        // 雑魚敵エンティティの破棄
        using var enemyQuery = world.QueryBuilder().With<EnemyTag>().Build();
        enemyQuery.Each(e => e.Destruct());

        // 雑魚敵およびプレイヤー弾幕の回収
        BulletSpawner.ClearAllEnemyBullets(world);
        MeleeSpawner.ClearAllEnemyMelee(world);

        using (var playerBullets = world.QueryBuilder().With<PlayerBulletTag>().Build())
        {
            playerBullets.Each(CombatPoolService.Recycle);
        }
    }

    /// <summary>
    ///     チュートリアルシーンのルートおよび全配下エンティティを一括破棄します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    public static void Teardown(World world)
    {
        world.Remove<GameOverTag>();

        var root = world.Lookup(RootName);
        if (root.IsAlive())
        {
            root.Destruct();
        }

        BulletSpawner.ClearAllEnemyBulletsImmediate(world);
        MeleeSpawner.ClearAllEnemyMeleeImmediate(world);
        MinionSpawner.ClearAllMinionsImmediate(world);

        using (var playerBullets = world.QueryBuilder().With<PlayerBulletTag>().Build())
        {
            playerBullets.Each(CombatPoolService.Recycle);
        }

        using (var vfx = world.QueryBuilder().With<VfxTag>().Build())
        {
            vfx.Each(CombatPoolService.Recycle);
        }

        using (var melees = world.QueryBuilder().With<PlayerMeleeTag>().Build())
        {
            melees.Each(e => e.Destruct());
        }

        InputService.SetCursorVisible(true);
    }
}
