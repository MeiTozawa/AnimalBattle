namespace AnimalBattle.Configs;

using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using AnimalBattle.Core;
using Flecs.NET.Core;

/// <summary>
/// 游戏业务层全量配置的集中注册与依赖装配器。
/// </summary>
public static class GameConfigRegistrar
{
    /// <summary>
    /// 向通用的 <see cref="IConfigRegistry"/> 注册游戏所有领域的配置，并统一注入到 Flecs World。
    /// </summary>
    public static void RegisterAll(IConfigRegistry registry, World world)
    {
        Debug.Assert(registry != null, "IConfigRegistry は null にできません。");
        Debug.Assert(world != null, "World は null にできません。");

#if DEBUG
        TypeDescriptor.AddAttributes(typeof(Color), new TypeConverterAttribute(typeof(FlexibleColorConverter)));

        registry.RegisterToml<CatConfig>(ResourcePath.Config.Cat, (w, cfg) => AnimalBattle.Player.PlayerConfigSync.SyncCat(w, cfg));
        registry.RegisterToml<BunnyConfig>(ResourcePath.Config.Bunny, (w, cfg) => AnimalBattle.Player.PlayerConfigSync.SyncBunny(w, cfg));
        registry.RegisterToml<CatPrefabConfig>(ResourcePath.Config.CatPrefab, (w, cfg) => { w.Set(cfg); AnimalBattle.Player.PlayerPrefabs.Register(w); });
        registry.RegisterToml<BunnyPrefabConfig>(ResourcePath.Config.BunnyPrefab, (w, cfg) => { w.Set(cfg); AnimalBattle.Player.PlayerPrefabs.Register(w); });
        registry.RegisterToml<Boss1Config>(ResourcePath.Config.Boss1, (w, cfg) => AnimalBattle.Characters.Boss.BossConfigSync.SyncBoss1(w, cfg));
        registry.RegisterToml<Boss2Config>(ResourcePath.Config.Boss2, (w, cfg) => AnimalBattle.Characters.Boss.BossConfigSync.SyncBoss2(w, cfg));
        registry.RegisterToml<Boss1PatternConfig>(ResourcePath.Config.Boss1Patterns);
        registry.RegisterToml<Boss2PatternConfig>(ResourcePath.Config.Boss2Patterns);
        registry.RegisterToml<Boss1PrefabConfig>(ResourcePath.Config.Boss1Prefab, (w, cfg) => { w.Set(cfg); AnimalBattle.Bosses.BossPrefabs.Register(w); });
        registry.RegisterToml<Boss2PrefabConfig>(ResourcePath.Config.Boss2Prefab, (w, cfg) => { w.Set(cfg); AnimalBattle.Bosses.BossPrefabs.Register(w); });
        registry.RegisterToml<SpecialBulletConfig>(ResourcePath.Config.SpecialBullets);
        registry.RegisterToml<ArenaConfig>(ResourcePath.Config.Arena, (w, cfg) => { w.Set(cfg); TelegraphRenderHelper.CurrentArena = cfg; });
        registry.RegisterToml<RenderConfig>(ResourcePath.Config.Render, (w, cfg) => { w.Set(cfg); TelegraphRenderHelper.CurrentEffects = cfg.BossEffects; });
        registry.RegisterToml<CameraConfig>(ResourcePath.Config.Camera, (w, cfg) => { w.Set(cfg); AnimalBattle.Core.CameraService.CurrentConfig = cfg; });
        registry.RegisterToml<TileSetConfig>(ResourcePath.Platform.SheetToml);
        registry.RegisterToml<AudioConfig>(ResourcePath.Config.Audio, (w, cfg) => { w.Set(cfg); AnimalBattle.Core.SoundService.CurrentConfig = cfg; });
        registry.RegisterToml<CombatPrefabConfig>(ResourcePath.Config.CombatPrefab);
        registry.RegisterToml<StartSceneConfig>(ResourcePath.Config.StartScene);
#else
        var loadResult = BinaryConfigLoader.Load();
        Debug.Assert(!loadResult.IsError, $"Release バイナリ設定の読み込みに失敗しました: {loadResult.FirstError.Description}");
        var b = loadResult.Value;

        registry.RegisterInstance(b.Cat);
        registry.RegisterInstance(b.Bunny);
        registry.RegisterInstance(b.CatPrefab);
        registry.RegisterInstance(b.BunnyPrefab);
        registry.RegisterInstance(b.Boss1);
        registry.RegisterInstance(b.Boss2);
        registry.RegisterInstance(b.Boss1Pattern);
        registry.RegisterInstance(b.Boss2Pattern);
        registry.RegisterInstance(b.Boss1Prefab);
        registry.RegisterInstance(b.Boss2Prefab);
        registry.RegisterInstance(b.SpecialBullet);
        registry.RegisterInstance(b.Arena);
        registry.RegisterInstance(b.Render);
        registry.RegisterInstance(b.Camera);
        registry.RegisterInstance(b.TileSet);
        registry.RegisterInstance(new AudioConfig());
        registry.RegisterInstance(b.CombatPrefab);
        registry.RegisterInstance(b.StartScene ?? new StartSceneConfig());
#endif

        registry.InjectAllToWorld(world);

        // 静的サービスへの現在値反映
        if (registry.Contains<ArenaConfig>())
        {
            TelegraphRenderHelper.CurrentArena = registry.Get<ArenaConfig>();
        }
        if (registry.Contains<RenderConfig>())
        {
            TelegraphRenderHelper.CurrentEffects = registry.Get<RenderConfig>().BossEffects;
        }
        if (registry.Contains<CameraConfig>())
        {
            AnimalBattle.Core.CameraService.CurrentConfig = registry.Get<CameraConfig>();
        }
        if (registry.Contains<AudioConfig>())
        {
            AnimalBattle.Core.SoundService.CurrentConfig = registry.Get<AudioConfig>();
        }
    }
}
