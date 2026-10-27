namespace AnimalBattle.Configs;

using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using AnimalBattle.Core;
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

        // Debug / Release 共通：Color 型コンバータ登録
        TypeDescriptor.AddAttributes(typeof(Color), new TypeConverterAttribute(typeof(FlexibleColorConverter)));

        // TOML 設定ファイルからの自動バインド登録：.NET Configuration Binding Generator 駆動
        registry.RegisterToml<CatConfig>(ResourcePath.Config.Cat, (root, cfg) => root.Bind(cfg), (w, cfg) => AnimalBattle.Player.PlayerConfigSync.SyncCat(w, cfg));
        registry.RegisterToml<BunnyConfig>(ResourcePath.Config.Bunny, (root, cfg) => root.Bind(cfg), (w, cfg) => AnimalBattle.Player.PlayerConfigSync.SyncBunny(w, cfg));

        registry.RegisterToml<CatWeaponConfig>(ResourcePath.Config.Catweapon, (root, cfg) => root.Bind(cfg), (w, cfg) => AnimalBattle.Player.PlayerConfigSync.SyncCatWeapon(w, cfg));

        registry.RegisterToml<BunnyWeaponConfig>(ResourcePath.Config.Bunnyweapon, (root, cfg) => root.Bind(cfg), (w, cfg) => AnimalBattle.Player.PlayerConfigSync.SyncBunnyWeapon(w, cfg));
        registry.RegisterToml<Boss1Config>(ResourcePath.Config.Boss1, (root, cfg) => root.Bind(cfg), (w, cfg) => AnimalBattle.Characters.Boss.BossConfigSync.SyncBoss1(w, cfg));
        registry.RegisterToml<Boss2Config>(ResourcePath.Config.Boss2, (root, cfg) => root.Bind(cfg), (w, cfg) => AnimalBattle.Characters.Boss.BossConfigSync.SyncBoss2(w, cfg));
        registry.RegisterToml<Boss1PatternConfig>(ResourcePath.Config.Boss1Patterns, (root, cfg) => root.Bind(cfg));
        registry.RegisterToml<Boss2PatternConfig>(ResourcePath.Config.Boss2Patterns, (root, cfg) => root.Bind(cfg));
        registry.RegisterToml<SpecialBulletConfig>(ResourcePath.Config.SpecialBullets, (root, cfg) => root.Bind(cfg));
        registry.RegisterToml<ArenaConfig>(ResourcePath.Config.Arena, (root, cfg) => root.Bind(cfg), (w, cfg) => w.Set(cfg));
        registry.RegisterToml<RenderConfig>(ResourcePath.Config.Render, (root, cfg) => root.Bind(cfg), (w, cfg) => w.Set(cfg));
        registry.RegisterToml<CameraConfig>(ResourcePath.Config.Camera, (root, cfg) => root.Bind(cfg), (w, cfg) => { w.Set(cfg); AnimalBattle.Core.CameraService.CurrentConfig = cfg; });
        registry.RegisterToml<TileSetConfig>(ResourcePath.Config.Sheet, (root, cfg) => root.Bind(cfg));
        registry.RegisterToml<AudioConfig>(ResourcePath.Config.Audio, (root, cfg) => root.Bind(cfg), (w, cfg) => { w.Set(cfg); AnimalBattle.Core.SoundService.CurrentConfig = cfg; });
        registry.RegisterToml<CombatPrefabConfig>(ResourcePath.Config.CombatPrefab, (root, cfg) => root.Bind(cfg));
        registry.RegisterToml<StartSceneConfig>(ResourcePath.Config.StartScene, (root, cfg) => root.Bind(cfg));

        registry.InjectAllToWorld(world);

        // 静的サービスへの現在値反映
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
