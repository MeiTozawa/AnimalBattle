namespace AnimalBattle.Configs;

using AnimalBattle.Features.Items.Configs;
using AnimalBattle.Features.Tutorial.Configs;
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
        var catHolder = registry.RegisterToml<PlayerConfig>(ResourcePath.Config.Cat, (root, cfg) => root.Bind(cfg), PlayerConfigSync.SyncCat, injectToWorld: false);
        var bunnyHolder = registry.RegisterToml<PlayerConfig>(ResourcePath.Config.Bunny, (root, cfg) => root.Bind(cfg), PlayerConfigSync.SyncBunny, injectToWorld: false);

        var orbHolder = registry.RegisterToml<GiantOrbAbilityConfig>(ResourcePath.Config.Giantorb, (root, cfg) => root.Bind(cfg), PlayerConfigSync.SyncGiantOrb);
        var cleaveHolder = registry.RegisterToml<ScreenCleaveAbilityConfig>(ResourcePath.Config.Screencleave, (root, cfg) => root.Bind(cfg), PlayerConfigSync.SyncScreenCleave);

        var catWeaponHolder = registry.RegisterToml<RangedWeaponConfig>(ResourcePath.Config.Catweapon, (root, cfg) => root.Bind(cfg), PlayerConfigSync.SyncCatWeapon);
        var bunnyWeaponHolder = registry.RegisterToml<MeleeWeaponConfig>(ResourcePath.Config.Bunnyweapon, (root, cfg) => root.Bind(cfg), PlayerConfigSync.SyncBunnyWeapon);

        SpecialAbilityPrefabs.Register(world, orbHolder.Value, cleaveHolder.Value);
        WeaponPrefabs.Register(world, catWeaponHolder.Value, bunnyWeaponHolder.Value);
        PlayerPrefabs.Register(world, catHolder.Value, bunnyHolder.Value);

        registry.RegisterToml<BossSnakeConfig>(ResourcePath.Config.BossSnake, (root, cfg) => root.Bind(cfg), Bosses.Sync.BossConfigSync.SyncBossSnake);
        registry.RegisterToml<BossLionConfig>(ResourcePath.Config.BossLion, (root, cfg) => root.Bind(cfg), Bosses.Sync.BossConfigSync.SyncBossLion);
        registry.RegisterToml<BossSnakePatternConfig>(ResourcePath.Config.BossSnakePatterns, (root, cfg) => { root.Bind(cfg.Skills); cfg.PostProcess(); }, Bosses.Sync.BossConfigSync.SyncBossSnakePattern);
        registry.RegisterToml<BossLionPatternConfig>(ResourcePath.Config.BossLionPatterns, (root, cfg) => { root.Bind(cfg.Skills); cfg.PostProcess(); }, Bosses.Sync.BossConfigSync.SyncBossLionPattern);
        registry.RegisterToml<SpecialBulletConfig>(ResourcePath.Config.SpecialBullets, (root, cfg) => root.Bind(cfg), Combat.Sync.CombatConfigSync.SyncSpecialBullets);
        registry.RegisterToml<ArenaConfig>(ResourcePath.Config.Arena, (root, cfg) => root.Bind(cfg), Arena.Sync.ArenaConfigSync.SyncArena);

        registry.RegisterToml<BossRenderConfig>(ResourcePath.Config.BossRender, (root, cfg) => root.Bind(cfg), SyncBossRender);
        registry.RegisterToml<PlayerRenderConfig>(ResourcePath.Config.PlayerRender, (root, cfg) => root.Bind(cfg), SyncPlayerRender);
        registry.RegisterToml<CombatRenderConfig>(ResourcePath.Config.CombatRender, (root, cfg) => root.Bind(cfg), SyncCombatRender);
        registry.RegisterToml<VfxRenderConfig>(ResourcePath.Config.VfxRender, (root, cfg) => root.Bind(cfg), SyncVfxRender);
        registry.RegisterToml<UiRenderConfig>(ResourcePath.Config.UiRender, (root, cfg) => root.Bind(cfg), SyncUiRender);
        registry.RegisterToml<DebugRenderConfig>(ResourcePath.Config.DebugRender, (root, cfg) =>
        {
            var sec = root.GetSection("Debug");
            if (sec.Exists()) sec.Bind(cfg);
            else root.Bind(cfg);
        }, static (w, cfg) => w.Set(cfg));
        registry.RegisterToml<TutorialRenderConfig>(ResourcePath.Config.TutorialRender, (root, cfg) => root.Bind(cfg), SyncTutorialRender);
        registry.RegisterToml<CameraConfig>(ResourcePath.Config.Camera, (root, cfg) => root.Bind(cfg), SyncCamera);
        registry.RegisterToml<TileSetConfig>(ResourcePath.Config.Sheet, (root, cfg) => root.Bind(cfg), Arena.Sync.ArenaConfigSync.SyncTileSet);
        registry.RegisterToml<AudioConfig>(ResourcePath.Config.Audio, (root, cfg) => root.Bind(cfg), SyncAudio);
        registry.RegisterToml<CombatPrefabConfig>(ResourcePath.Config.CombatPrefab, (root, cfg) => root.Bind(cfg), Combat.Sync.CombatConfigSync.SyncCombatPrefabs);
        registry.RegisterToml<DummyConfig>(ResourcePath.Config.Dummy, (root, cfg) => root.Bind(cfg), Combat.Sync.DummyConfigSync.Sync);
        registry.RegisterToml<MinionConfig>(ResourcePath.Config.Minions, (root, cfg) => root.Bind(cfg), Enemies.Sync.MinionConfigSync.Sync);
        registry.RegisterToml<StartSceneConfig>(ResourcePath.Config.StartScene, (root, cfg) => root.Bind(cfg), StartSceneConfigSync.Sync);
        registry.RegisterToml<TutorialConfig>(ResourcePath.Config.Tutorial, (root, cfg) => root.Bind(cfg), Features.Tutorial.Sync.TutorialConfigSync.Sync);
        registry.RegisterToml<ItemConfig>(ResourcePath.Config.Items, (root, cfg) => root.Bind(cfg), static (w, cfg) => w.Set(cfg));
        registry.RegisterToml<ItemRenderConfig>(ResourcePath.Config.ItemsRender, (root, cfg) => root.Bind(cfg), SyncItemRender);

        registry.InjectAllToWorld(world);

        // 初期ロード時のサブコンポーネント・静的サービス同期（ホットリロードと同一ロジックを実行）
        SyncItemRender(world, registry.Get<ItemRenderConfig>());
        SyncBossRender(world, registry.Get<BossRenderConfig>());
        SyncPlayerRender(world, registry.Get<PlayerRenderConfig>());
        SyncCombatRender(world, registry.Get<CombatRenderConfig>());
        SyncVfxRender(world, registry.Get<VfxRenderConfig>());
        SyncUiRender(world, registry.Get<UiRenderConfig>());
        SyncTutorialRender(world, registry.Get<TutorialRenderConfig>());
        SyncCamera(world, registry.Get<CameraConfig>());
        SyncAudio(world, registry.Get<AudioConfig>());
    }

    private static void SyncBossRender(World w, BossRenderConfig cfg)
    {
        w.Set(cfg);
        w.Set(cfg.BossEffects);
        w.Set(cfg.BossHpBar);
        TelegraphRenderHelper.DefaultEffects = cfg.BossEffects;
    }

    private static void SyncPlayerRender(World w, PlayerRenderConfig cfg)
    {
        w.Set(cfg);
        w.Set(cfg.Player);
        w.Set(cfg.PlayerHpBadge);
        w.Set(cfg.SlashEffect);
    }

    private static void SyncCombatRender(World w, CombatRenderConfig cfg)
    {
        w.Set(cfg);
        w.Set(cfg.Bullets);
        w.Set(cfg.SpawnBuffer);
        BulletPalette.CurrentConfig = cfg.Bullets;
    }

    private static void SyncVfxRender(World w, VfxRenderConfig cfg)
    {
        w.Set(cfg);
        w.Set(cfg.VisualEffects);
    }

    private static void SyncUiRender(World w, UiRenderConfig cfg)
    {
        w.Set(cfg);
        w.Set(cfg.Viewport);
        w.Set(cfg.Hud);
        w.Set(cfg.AimReticle);
        w.Set(cfg.Modals);
    }

    private static void SyncTutorialRender(World w, TutorialRenderConfig cfg)
    {
        w.Set(cfg);
        w.Set(cfg.Colors);
        w.Set(cfg.Doors);
        w.Set(cfg.Layout);
        w.Set(cfg.TargetRender);
    }

    private static void SyncItemRender(World w, ItemRenderConfig cfg)
    {
        w.Set(cfg);
        w.Set(cfg.HealthPackRender);
    }

    private static void SyncCamera(World w, CameraConfig cfg)
    {
        w.Set(cfg);
        CameraService.CurrentConfig = cfg;
    }

    private static void SyncAudio(World w, AudioConfig cfg)
    {
        w.Set(cfg);
        SoundService.CurrentConfig = cfg;
    }
}
