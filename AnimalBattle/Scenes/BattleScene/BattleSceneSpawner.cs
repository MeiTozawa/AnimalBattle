using AnimalBattle.Logging;
using AnimalBattle.Ai;
using AnimalBattle.Combat.Spawners;
using AnimalBattle.Features.Items.Spawners;
using AnimalBattle.Maps;

namespace AnimalBattle.Scenes.Battle;

/// <summary>
///     戦闘シーンの全エンティティツリー：自機、Boss/木桩、足場・タイルマップの生成・破棄を行うスポナークラス。
/// </summary>
public static class BattleSceneSpawner
{
    public const string RootName = "BattleSceneRoot";
    public const string BossSnakeName = "BossSnake";
    public const string BossLionName = "BossLion";

    /// <summary>
    ///     戦闘シーンのルートエンティティおよび自機・敵対ユニット・アリーナを生成してワールドへ配置します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="playerType">選択されたプレイヤーキャラクター種別</param>
    /// <param name="bossType">選択された Boss 種別（Empty の場合は手触りテスト用木桩を生成）</param>
    public static void Spawn(World world, CharacterType playerType, BossType bossType)
    {
        Teardown(world);

        var root = world.Entity(RootName);
        var assets = world.Get<IAssetManager>();

        // 1. 自機エンティティの生成
        var hasPlayerHandle = assets.TryGetCharacterSpriteHandle(playerType, "Idle", out var playerHandle);
        Debug.Assert(hasPlayerHandle, "プレイヤーの初期スプライトハンドルが見つかりません。");
        if (!hasPlayerHandle) Log.Error("BattleSceneSpawner", $"プレイヤーの初期スプライトハンドルが見つかりません: {playerType}");

        var playerEntity = PlayerPrefabs.SpawnPlayer(world, playerType, root, "Player1", playerHandle);

        // 2. 地形・アリーナの生成
        var arenaCfg = world.Get<ArenaConfig>();
        var tileMapCfg = arenaCfg.TileMap;
        var sheetHandle = assets.LoadTexture(tileMapCfg.TileSheetPath);
        Debug.Assert(sheetHandle >= 0, $"タイルシートが読み取れません: {tileMapCfg.TileSheetPath}");
        TileSet tileSet = new(sheetHandle, world.Get<TileSetConfig>());
        var tileMap = TileMap.FromConfig(tileMapCfg, tileSet);
        tileMap.BuildPlatformColliders(world, root);
        root.Set(tileMap);

        // 3. 敵対エンティティ（Boss または 練習用木桩）の生成
        if (bossType == BossType.Empty)
        {
            // 空シーン：Boss 不在の環境で手触りテスト用木桩を配置
            DummySpawner.Spawn(world, root);
        }
        else
        {
            var cfg = BossPrefabs.GetConfig(world, bossType);
            var bossName = BossPrefabs.GetEntityName(bossType);
            var texPath = BossPrefabs.GetTexturePath(bossType);

            var hasBossHandle = assets.TryGetTexture(texPath, out var bossHandle);
            Debug.Assert(hasBossHandle, $"Boss テクスチャが見つかりません: {texPath}");
            if (!hasBossHandle) Log.Error("BattleSceneSpawner", $"Boss テクスチャが見つかりません: {texPath}");

            var bossEntity = world.Entity(bossName)
                .ChildOf(root)
                .IsA(BossPrefabs.GetPrefab(bossType));

            var brain = BossPrefabs.CreateBrain(world, bossType, cfg.Spawn, tileMap.Nodes);

            bossEntity
                .Set(new Transform(cfg.Spawn, cfg.Scale))
                .Set(new Sprite(bossHandle, false, true, new Rect(0, 0, 192, 192)))
                .Set(new BossCombatState(bossType, Phase.Phase1))
                .Set(new BossBrainComponent(brain));

            brain.AbilitySystem.OnTagsChanged += tags =>
            {
                bossEntity.GetMut<GameplayTagState>().ActionMask = tags;
            };
            bossEntity.GetMut<GameplayTagState>().ActionMask = brain.AbilitySystem.Tags.RawTags;

            brain.OnClearBullets += () =>
            {
                BulletSpawner.ClearAllEnemyBullets(world);
                MeleeSpawner.ClearAllEnemyMelee(world);
            };

            // 出現バッファ設定の適用：接触判定無効化および行動阻害タグの付与
            if (cfg.SpawnBufferDuration > 0.0f)
            {
                bossEntity.Set(new SpawnBufferState(cfg.SpawnBufferDuration));
                var hitbox = BossPrefabs.GetPrefab(bossType).Get<Hitbox>();
                hitbox.IsActive = false;
                bossEntity.Set(hitbox);
                bossEntity.Add<CannotAttackTag>();
                if (!cfg.SpawnBufferCanMove)
                {
                    bossEntity.Add<CannotMoveTag>();
                }
            }
        }

        world.Set(new AimReticleState());
        CameraService.Reset();
        GameEngine.SetCameraOffset(Vec2i.Zero);

        InputService.SetCursorVisible(false);
        SoundService.PlayBgm();
    }

    /// <summary>
    ///     戦闘シーンの全エンティティ、リソース、および弾幕プールを安全にクリーンアップします。
    ///     戦闘ルートエンティティ配下のエンティティ・コンポーネントは Flecs のカスケード破棄により一括自動解放されます。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    public static void Teardown(World world)
    {
        // ゲームオーバー状態の解除：Remove は冪等のため事前の Has チェック不要
        world.Remove<GameOverTag>();

        // 1. サウンド・カーソル・カメラ・照準器の復元
        SoundService.StopBgm();
        InputService.SetCursorVisible(true);
        world.Set(new AimReticleState());
        CameraService.Reset();
        GameEngine.SetCameraOffset(Vec2i.Zero);

        // 2. 残存弾幕、近接判定、雑魚敵、アイテムおよび VFX の一括回収・プール返却
        CombatCleanupService.ClearAllCombatEntitiesForTeardown(world);

        // 3. 戦闘シーンルート配下の全エンティティ：自機、Boss/木桩、足場、タイルマップ等を一括破棄
        var root = world.Lookup(RootName);
        if (root.IsAlive())
        {
            root.Destruct();
        }
    }
}
