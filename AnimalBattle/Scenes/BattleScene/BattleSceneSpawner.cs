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
using AnimalBattle.Framework.Pooling;
using Flecs.NET.Core;

namespace AnimalBattle.Scenes.Battle;

/// <summary>
///     戦闘シーンの全エンティティツリー（自機、Boss、足場・タイルマップ）の生成・破棄を行うスポナークラス。
/// </summary>
public static class BattleSceneSpawner {
    public const string RootName = "BattleSceneRoot";
    public const string Boss1Name = "Boss1";
    public const string Boss2Name = "Boss2";

    /// <summary>
    ///     戦闘シーンのルートエンティティおよび自機・Boss・アリーナを生成してワールドへ配置します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="playerType">選択されたプレイヤーキャラクター種別</param>
    /// <param name="bossType">選択された Boss 種別</param>
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

        if (playerType == CharacterType.Bunny)
        {
            playerEntity.Set(BunnyWeaponState.FromConfig(playerCfg));
            var melee = world.Entity("PlayerSlashHitbox")
                .ChildOf(playerEntity)
                .IsA(CombatPrefabs.PlayerSlashHitbox);
            melee.Disable();
            playerEntity.Add<EquippedWeapon>(melee);
        }
        else if (playerType == CharacterType.Cat)
        {
            playerEntity.Set(CatWeaponState.FromConfig(playerCfg));
        }

        // 2. アリーナ・タイルマップの生成
        var arenaCfg = world.Get<ArenaConfig>();
        var tileMapCfg = arenaCfg.TileMap;
        var sheetHandle = AssetManager.LoadTexture(tileMapCfg.TileSheetPath);
        Debug.Assert(sheetHandle >= 0, $"タイルシートが読み取れません: {tileMapCfg.TileSheetPath}");
        TileSet tileSet = new(sheetHandle, world.Get<TileSetConfig>());
        var tileMap = TileMap.FromConfig(tileMapCfg, tileSet);
        tileMap.BuildPlatformColliders(world, root);
        root.Set(tileMap);

        // 3. Boss エンティティの生成
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

        var (spawn, scale) = bossType switch {
            BossType.Boss1 => (world.Get<Boss1Config>().Spawn, world.Get<Boss1Config>().Scale),
            BossType.Boss2 => (world.Get<Boss2Config>().Spawn, world.Get<Boss2Config>().Scale),
            _ => (Vec2f.Zero, Vec2f.One)
        };

        IBossBrain brain = bossType switch {
            BossType.Boss1 => new Boss1Brain(world, spawn),
            BossType.Boss2 => new Boss2Brain(world, spawn, null, () => tileMap.NavGraph),
            _ => new Boss1Brain(world, spawn)
        };

        bossEntity
            .Set(new Transform(spawn, scale))
            .Set(new Sprite(bossHandle, false, true, new Rect(0, 0, 192, 192)))
            .Set(new BossCombatState(bossType, Phase.Phase1) { State = brain.CurrentState })
            .Set(new BossBrainComponent(brain));

        brain.AbilitySystem.OnTagsChanged += tags =>
        {
            if (bossEntity.IsAlive() && bossEntity.Has<GameplayTagState>())
            {
                bossEntity.GetMut<GameplayTagState>().ActionMask = tags;
            }
        };
        bossEntity.GetMut<GameplayTagState>().ActionMask = brain.AbilitySystem.Tags.RawTags;

        brain.OnScreenShake += (intensity, duration) =>
        {
            var camCfg = world.Get<CameraConfig>();
            var trauma = intensity > 1.0f ? intensity * camCfg.AbilityShakeTraumaScale : intensity;
            CameraService.AddTrauma(trauma);
        };

        brain.OnClearBullets += () =>
        {
            BulletSpawner.ClearAllBossBullets(world);
        };

        var emitterAngleOffset = 0.0f;
        brain.OnEmitBulletWave += (skill, origin, target) =>
        {
            if (bossType == BossType.Boss1)
            {
                BulletEmitter.EmitBoss1(world, skill, origin, target, ref emitterAngleOffset);
            }
            else if (bossType == BossType.Boss2)
            {
                var colHeight = bossEntity.Get<CollisionBox>().Size.Y;
                var forwardDir = brain.FacingRight ? 1.0f : -1.0f;
                BulletEmitter.EmitBoss2(world, skill, origin, target, forwardDir, colHeight);
            }
        };

        if (brain is Boss2Brain b2Brain)
        {
            b2Brain.OnCreateHitbox += (skill, offsetX, offsetY) =>
            {
                var patternCfg = world.Get<Boss2PatternConfig>();
                ref readonly var trans = ref bossEntity.Get<Transform>();
                var forwardDir = brain.FacingRight ? 1.0f : -1.0f;
                var colHeight = bossEntity.Get<CollisionBox>().Size.Y;
                var vx = b2Brain.Velocity.X;
                if (patternCfg.TryGetSkill(skill, out var pattern))
                {
                    pattern.SpawnHitbox(
                        world,
                        trans.Position,
                        forwardDir,
                        vx,
                        colHeight,
                        offsetX,
                        offsetY,
                        brain.TargetPosition);
                }
            };
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
        if (world.Has<GameOverTag>())
        {
            world.Remove<GameOverTag>();
        }

        // 1. サウンド・カーソル・カメラ・照準器の復元
        SoundService.StopBgm();
        InputService.SetCursorVisible(true);
        world.Set(new AimReticleState());
        CameraService.Reset();
        GameEngine.SetCameraOffset(Vec2i.Zero);

        // 2. 残存弾幕、近接判定および VFX の一括回収・プール返却
        BulletSpawner.ClearAllBossBulletsImmediate(world);
        using (var playerBullets = world.QueryBuilder().With<PlayerBulletTag>().Build())
        {
            playerBullets.Each(CombatPoolService.Recycle);
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
            vfx.Each(CombatPoolService.Recycle);
        }
        EventBus.Publish(new CombatResetEvent());

        // 3. 戦闘シーンルート配下の全エンティティ（自機、Boss、足場、タイルマップ等）を一括破棄
        var root = world.Lookup(RootName);
        if (root.IsAlive())
        {
            root.Destruct();
        }
    }
}
