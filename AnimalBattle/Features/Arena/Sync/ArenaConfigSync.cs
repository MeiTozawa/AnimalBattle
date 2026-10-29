namespace AnimalBattle.Arena.Sync;

using System.Diagnostics;
using AnimalBattle.Asset;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Maps;
using AnimalBattle.Scenes.Battle;
using Flecs.NET.Core;

/// <summary>
/// アリーナおよびタイルセット設定変更時に、Flecs.NET ワールドの単例および戦闘地形（TileMap・足場コライダー）を同期・再構築する同期機構。
/// </summary>
public static class ArenaConfigSync
{
    /// <summary>
    /// アリーナ設定の変更をワールドへ反映し、戦闘シーン中であれば地形および足場コライダーを再構築します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="newConfig">更新後の ArenaConfig 設定</param>
    public static void SyncArena(World world, ArenaConfig newConfig)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");
        Debug.Assert(newConfig != null, "ArenaConfig は null にできません。");

        // 1. ワールド単例の更新
        world.Set(newConfig);

        // 2. 活体戦闘シーンが存在する場合、地形と足場コライダーを再構築
        var root = world.Lookup(BattleSceneSpawner.RootName);
        if (root.IsAlive())
        {
            RebuildTileMap(world, root);
        }
    }

    /// <summary>
    /// タイルセット設定（sheet.toml）の変更をワールドへ反映し、戦闘シーン中であれば地形描画と足場コライダーを再構築します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="newConfig">更新後の TileSetConfig 設定</param>
    public static void SyncTileSet(World world, TileSetConfig newConfig)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");
        Debug.Assert(newConfig != null, "TileSetConfig は null にできません。");

        // 1. ワールド単例の更新
        world.Set(newConfig);

        // 2. 活体戦闘シーンが存在する場合、地形と足場コライダーを再構築
        var root = world.Lookup(BattleSceneSpawner.RootName);
        if (root.IsAlive())
        {
            RebuildTileMap(world, root);
        }
    }

    private static void RebuildTileMap(World world, Entity root)
    {
        // 既存の足場コライダーエンティティを安全に破棄
        using var platformQuery = world.QueryBuilder().With<PlatformTag>().Build();
        platformQuery.Each((Entity e) => e.Destruct());

        var arenaCfg = world.Get<ArenaConfig>();
        var tileSetCfg = world.Get<TileSetConfig>();
        var assets = world.Get<IAssetManager>();

        var tileMapCfg = arenaCfg.TileMap;
        var sheetHandle = assets.LoadTexture(tileMapCfg.TileSheetPath);
        Debug.Assert(sheetHandle >= 0, $"タイルシートが読み取れません: {tileMapCfg.TileSheetPath}");

        var tileSet = new TileSet(sheetHandle, tileSetCfg);
        var tileMap = TileMap.FromConfig(tileMapCfg, tileSet);
        tileMap.BuildPlatformColliders(world, root);
        root.Set(tileMap);
    }
}
