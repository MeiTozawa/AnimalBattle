using System.Diagnostics;
using AnimalBattle.Ai;
using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Boss2;
using AnimalBattle.Asset;
using AnimalBattle.Characters;
using AnimalBattle.Characters.Boss;
using AnimalBattle.Combat;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Events;
using AnimalBattle.Maps;
using AnimalBattle.Player;
using AnimalBattle.Bosses;
using Flecs.NET.Core;

namespace AnimalBattle.Scenes;

/// <summary>
///     戦闘シーンの全エンティティツリー（自機、首領、アリーナ足場・タイルマップ）を構築・解体するライフサイクル装配クラス。
/// </summary>
public static class BattleSceneSpawner {
    public const string RootName = "BattleSceneRoot";
    public const string Boss1Name = "Boss1";
    public const string Boss2Name = "Boss2";

    /// <summary>
    ///     戦闘シーンのルートエンティティおよび自機・首領・アリーナをワールドへ装配します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="playerType">選択されたプレイヤーキャラクター種別</param>
    /// <param name="bossType">選択された首領種別</param>
    public static void Spawn(World world, CharacterType playerType, BossType bossType)
    {
        Teardown(world);

        var root = world.Entity(RootName).Add<BattleSceneTag>();

        // 1. 自機エンティティの生成
        var hasPlayerHandle = AssetManager.TryGetCharacterSpriteHandle(playerType, "Idle", out var playerHandle);
        Debug.Assert(hasPlayerHandle, "プレイヤーの初期スプライトハンドルが見つかりません。");

        var playerCfg = world.GetPlayerConfig(playerType);
        var playerEntity = world.Entity("Player1")
            .ChildOf(root)
            .IsA(PlayerPrefabs.GetPrefab(playerType));

        PlayerPawn player = playerType switch
        {
            CharacterType.Bunny => new Bunny(playerEntity, playerCfg, playerHandle),
            CharacterType.Cat => new Cat(playerEntity, playerCfg, playerHandle),
            _ => AssertHelper.FailWithDefault($"未対応のキャラクター種別です: {playerType}", (PlayerPawn)new Cat(playerEntity, playerCfg, playerHandle))
        };
        player.Reset(playerCfg);
        PlayerController controller = new(player);

        playerEntity.Set(player);
        playerEntity.Set(controller);

        // 2. アリーナ・タイルマップの生成
        var arenaCfg = world.Get<ArenaConfig>();
        var tileMapCfg = arenaCfg.TileMap;
        var sheetHandle = AssetManager.LoadTexture(tileMapCfg.TileSheetPath);
        Debug.Assert(sheetHandle >= 0, $"タイルシートが読み取れません: {tileMapCfg.TileSheetPath}");
        TileSet tileSet = new(sheetHandle, world.Get<TileSetConfig>());
        var tileMap = TileMap.FromConfig(tileMapCfg, tileSet);
        tileMap.BuildPlatformColliders(world, root);
        root.Set(tileMap);

        // 3. 首領エンティティの生成
        var bossName = bossType == BossType.Boss2 ? Boss2Name : Boss1Name;
        var (texPath, _) = bossType switch {
            BossType.Boss1 => (ResourcePath.Characters.Character031_3Png, "Boss 1"),
            BossType.Boss2 => (ResourcePath.Characters.Character027_3Png, "Boss 2"),
            _ => (ResourcePath.Characters.Character031_3Png, "Boss 1")
        };

        var hasBossHandle = AssetManager.TryGetTexture(texPath, out var bossHandle);
        Debug.Assert(hasBossHandle, $"Boss テクスチャが見つかりません: {texPath}");

        var bossEntity = world.Entity(bossName)
            .ChildOf(root)
            .IsA(BossPrefabs.GetPrefab(bossType));

        BaseBossAgent boss = bossType switch {
            BossType.Boss1 => new Boss1Agent(bossEntity, bossHandle),
            BossType.Boss2 => new Boss2Agent(bossEntity, bossHandle, () => tileMap.NavGraph),
            _ => new Boss1Agent(bossEntity, bossHandle)
        };
        boss.Reset();

        boss.AbilitySystem.OnScreenShake += (intensity, duration) =>
        {
            var camCfg = world.Get<CameraConfig>();
            var trauma = intensity > 1.0f ? intensity * camCfg.AbilityShakeTraumaScale : intensity;
            CameraService.AddTrauma(trauma);
        };

        bossEntity.Set(boss);

        world.Set(new AimReticleState());
        CameraService.Reset();
        GameEngine.SetCameraOffset(0, 0);

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
        // 1. サウンド・カーソル・カメラ・照準器の復元
        SoundService.StopBgm();
        InputService.SetCursorVisible(true);
        world.Set(new AimReticleState());
        CameraService.Reset();
        GameEngine.SetCameraOffset(0, 0);

        // 2. 残存弾幕、近接判定および VFX の一括回収・プール返却
        BulletSpawner.ClearAllBossBullets(world);
        using (var playerBullets = world.QueryBuilder().With<PlayerBulletTag>().Build())
        {
            playerBullets.Each(e => e.Add<RecycleTag>());
        }
        using (var playerMelee = world.QueryBuilder().With<PlayerMeleeTag>().Build())
        {
            playerMelee.Each(e =>
            {
                e.Disable();
                e.Destruct();
            });
        }
        using (var vfx = world.QueryBuilder().With<VfxTag>().Build())
        {
            vfx.Each(e => e.Add<RecycleTag>());
        }
        EventBus.Publish(new CombatResetEvent());

        // 3. 戦闘シーンルート配下の全エンティティ（自機、首領、足場、タイルマップ等）を一括カスケード破棄
        var root = world.Lookup(RootName);
        if (root.IsAlive())
        {
            root.Destruct();
        }
    }
}
