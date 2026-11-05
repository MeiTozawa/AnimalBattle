namespace AnimalBattle.Configs;

using System.Diagnostics;
using AnimalBattle.Features.Items.Configs;
using AnimalBattle.Features.Tutorial.Configs;
using Flecs.NET.Core;
using Microsoft.Extensions.Configuration;

/// <summary>
///     ゲーム全体の設定登録および Flecs World への依存性注入を統括するレジストラ。
/// </summary>
public static class GameConfigRegistrar
{
    /// <summary>
    ///     全設定を <see cref="IConfigRegistry"/> に登録し、Flecs World へ一括注入します。
    /// </summary>
    public static void RegisterAll(IConfigRegistry registry, World world)
    {
        Debug.Assert(registry != null, "IConfigRegistry は null にできません。");
        Debug.Assert(world != null, "World は null にできません。");


        // TOML 設定ファイルからの自動バインド登録：.NET Configuration Binding Generator 駆動
        registry.RegisterToml<CatConfig>(ResourcePath.Config.Cat, (root, cfg) => root.Bind(cfg), (w, cfg) => AnimalBattle.Player.PlayerConfigSync.SyncCat(w, cfg));
        registry.RegisterToml<BunnyConfig>(ResourcePath.Config.Bunny, (root, cfg) => root.Bind(cfg), (w, cfg) => AnimalBattle.Player.PlayerConfigSync.SyncBunny(w, cfg));

        registry.RegisterToml<CatWeaponConfig>(ResourcePath.Config.Catweapon, (root, cfg) => root.Bind(cfg), (w, cfg) => AnimalBattle.Player.PlayerConfigSync.SyncCatWeapon(w, cfg));

        registry.RegisterToml<BunnyWeaponConfig>(ResourcePath.Config.Bunnyweapon, (root, cfg) => root.Bind(cfg), (w, cfg) => AnimalBattle.Player.PlayerConfigSync.SyncBunnyWeapon(w, cfg));
        registry.RegisterToml<BossSnakeConfig>(ResourcePath.Config.BossSnake, (root, cfg) => root.Bind(cfg), (w, cfg) => AnimalBattle.Bosses.Sync.BossConfigSync.SyncBossSnake(w, cfg));
        registry.RegisterToml<BossLionConfig>(ResourcePath.Config.BossLion, (root, cfg) => root.Bind(cfg), (w, cfg) => AnimalBattle.Bosses.Sync.BossConfigSync.SyncBossLion(w, cfg));
        registry.RegisterToml<BossSnakePatternConfig>(ResourcePath.Config.BossSnakePatterns, (root, cfg) => { root.Bind(cfg.Skills); cfg.PostProcess(); }, (w, cfg) => AnimalBattle.Bosses.Sync.BossConfigSync.SyncBossSnakePattern(w, cfg));
        registry.RegisterToml<BossLionPatternConfig>(ResourcePath.Config.BossLionPatterns, (root, cfg) => { root.Bind(cfg.Skills); cfg.PostProcess(); }, (w, cfg) => AnimalBattle.Bosses.Sync.BossConfigSync.SyncBossLionPattern(w, cfg));
        registry.RegisterToml<SpecialBulletConfig>(ResourcePath.Config.SpecialBullets, (root, cfg) => root.Bind(cfg), (w, cfg) => AnimalBattle.Combat.Sync.CombatConfigSync.SyncSpecialBullets(w, cfg));
        registry.RegisterToml<ArenaConfig>(ResourcePath.Config.Arena, (root, cfg) => root.Bind(cfg), (w, cfg) => AnimalBattle.Arena.Sync.ArenaConfigSync.SyncArena(w, cfg));
        registry.RegisterToml<BossRenderConfig>(ResourcePath.Config.BossRender, (root, cfg) => root.Bind(cfg), (w, cfg) => {
            w.Set(cfg);
            w.Set(cfg.BossEffects);
            w.Set(cfg.BossHpBar);
            TelegraphRenderHelper.DefaultEffects = cfg.BossEffects;
        });
        registry.RegisterToml<PlayerRenderConfig>(ResourcePath.Config.PlayerRender, (root, cfg) => root.Bind(cfg), (w, cfg) => {
            w.Set(cfg);
            w.Set(cfg.Player);
            w.Set(cfg.PlayerHpBadge);
            w.Set(cfg.SlashEffect);
        });
        registry.RegisterToml<CombatRenderConfig>(ResourcePath.Config.CombatRender, (root, cfg) => root.Bind(cfg), (w, cfg) => {
            w.Set(cfg);
            w.Set(cfg.Bullets);
            w.Set(cfg.SpawnBuffer);
            AnimalBattle.Combat.BulletPalette.CurrentConfig = cfg.Bullets;
        });
        registry.RegisterToml<VfxRenderConfig>(ResourcePath.Config.VfxRender, (root, cfg) => root.Bind(cfg), (w, cfg) => {
            w.Set(cfg);
            w.Set(cfg.VisualEffects);
        });
        registry.RegisterToml<UiRenderConfig>(ResourcePath.Config.UiRender, (root, cfg) => root.Bind(cfg), (w, cfg) => {
            w.Set(cfg);
            w.Set(cfg.Viewport);
            w.Set(cfg.Hud);
            w.Set(cfg.AimReticle);
            w.Set(cfg.Modals);
        });
        registry.RegisterToml<DebugRenderConfig>(ResourcePath.Config.DebugRender, (root, cfg) => {
            var sec = root.GetSection("Debug");
            if (sec.Exists()) sec.Bind(cfg);
            else root.Bind(cfg);
        }, (w, cfg) => {
            w.Set(cfg);
        });
        registry.RegisterToml<TutorialRenderConfig>(ResourcePath.Config.TutorialRender, (root, cfg) => root.Bind(cfg), (w, cfg) => {
            w.Set(cfg);
            w.Set(cfg.Colors);
            w.Set(cfg.Doors);
            w.Set(cfg.Layout);
            w.Set(cfg.TargetRender);
        });
        registry.RegisterToml<CameraConfig>(ResourcePath.Config.Camera, (root, cfg) => root.Bind(cfg), (w, cfg) => { w.Set(cfg); AnimalBattle.Core.CameraService.CurrentConfig = cfg; });
        registry.RegisterToml<TileSetConfig>(ResourcePath.Config.Sheet, (root, cfg) => root.Bind(cfg), (w, cfg) => AnimalBattle.Arena.Sync.ArenaConfigSync.SyncTileSet(w, cfg));
        registry.RegisterToml<AudioConfig>(ResourcePath.Config.Audio, (root, cfg) => root.Bind(cfg), (w, cfg) => { w.Set(cfg); AnimalBattle.Core.SoundService.CurrentConfig = cfg; });
        registry.RegisterToml<CombatPrefabConfig>(ResourcePath.Config.CombatPrefab, (root, cfg) => root.Bind(cfg), (w, cfg) => AnimalBattle.Combat.Sync.CombatConfigSync.SyncCombatPrefabs(w, cfg));
        registry.RegisterToml<DummyConfig>(ResourcePath.Config.Dummy, (root, cfg) => root.Bind(cfg), (w, cfg) => AnimalBattle.Combat.Sync.DummyConfigSync.Sync(w, cfg));
        registry.RegisterToml<MinionConfig>(ResourcePath.Config.Minions, (root, cfg) => root.Bind(cfg), (w, cfg) => AnimalBattle.Enemies.Sync.MinionConfigSync.Sync(w, cfg));
        registry.RegisterToml<StartSceneConfig>(ResourcePath.Config.StartScene, (root, cfg) => root.Bind(cfg), (w, cfg) => AnimalBattle.Scenes.Start.StartSceneConfigSync.Sync(w, cfg));
        registry.RegisterToml<TutorialConfig>(ResourcePath.Config.Tutorial, (root, cfg) => root.Bind(cfg), (w, cfg) => AnimalBattle.Features.Tutorial.Sync.TutorialConfigSync.Sync(w, cfg));
        registry.RegisterToml<ItemConfig>(ResourcePath.Config.Items, (root, cfg) => root.Bind(cfg), (w, cfg) => w.Set(cfg));
        registry.RegisterToml<ItemRenderConfig>(ResourcePath.Config.ItemsRender, (root, cfg) => root.Bind(cfg), (w, cfg) => {
            w.Set(cfg);
            w.Set(cfg.HealthPackRender);
        });

        registry.InjectAllToWorld(world);

        // 各描画領域のサブコンポーネントを Flecs World に単一インスタンスとして登録
        var itemRenderCfg = registry.Get<ItemRenderConfig>();
        world.Set(itemRenderCfg.HealthPackRender);
        var bCfg = registry.Get<BossRenderConfig>();
        world.Set(bCfg.BossEffects);
        world.Set(bCfg.BossHpBar);
        TelegraphRenderHelper.DefaultEffects = bCfg.BossEffects;

        var pCfg = registry.Get<PlayerRenderConfig>();
        world.Set(pCfg.Player);
        world.Set(pCfg.PlayerHpBadge);
        world.Set(pCfg.SlashEffect);

        var cCfg = registry.Get<CombatRenderConfig>();
        world.Set(cCfg.Bullets);
        world.Set(cCfg.SpawnBuffer);
        AnimalBattle.Combat.BulletPalette.CurrentConfig = cCfg.Bullets;

        var vCfg = registry.Get<VfxRenderConfig>();
        world.Set(vCfg.VisualEffects);

        var uCfg = registry.Get<UiRenderConfig>();
        world.Set(uCfg.Viewport);
        world.Set(uCfg.Hud);
        world.Set(uCfg.AimReticle);
        world.Set(uCfg.Modals);

        var tCfg = registry.Get<TutorialRenderConfig>();
        world.Set(tCfg.Colors);
        world.Set(tCfg.Doors);
        world.Set(tCfg.Layout);
        world.Set(tCfg.TargetRender);

        // 静的サービスへの現在値反映
        CameraService.CurrentConfig = registry.Get<CameraConfig>();
        SoundService.CurrentConfig = registry.Get<AudioConfig>();
    }
}
