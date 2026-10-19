using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Maps;
using Flecs.NET.Core;

namespace AnimalBattle.Systems.Render;

/// <summary>
///     アリーナ足場およびタイルマップの視口カリング描画を行う ECS システム。
/// </summary>
public static class TileMapRenderSystem
{
    private static Query<Transform, CollisionBox> _platformQuery;
    private static Query<TileMap> _tileMapQuery;
    private static bool _renderedTileMap;
    private static ViewportRenderConfig _cachedViewport = null!;

    /// <summary>
    ///     タイルマップ描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddTileMapRenderSystem(this World world)
    {
        _platformQuery = world.QueryBuilder<Transform, CollisionBox>()
            .With<PlatformTag>()
            .Build();

        _tileMapQuery = world.QueryBuilder<TileMap>().Build();

        world.System("TileMapRenderSystem")
            .Kind(Ecs.OnStore)
            .Iter(ExecuteTileMapRender);

        return world;
    }

    /// <summary>
    ///     タイルマップ描画パイプライン（名前付き静的メソッド）。
    /// </summary>
    public static void ExecuteTileMapRender(Iter it)
    {
        var w = it.World();
        if (w.Get<GameFlow>().State != GameState.Battle) return;

        _cachedViewport = w.Get<RenderConfig>().Viewport;
        _renderedTileMap = false;
        _tileMapQuery.Each(DrawTileMap);

        if (!_renderedTileMap)
        {
            _platformQuery.Each(DrawFallbackPlatform);
        }
    }

    private static void DrawTileMap(ref TileMap tileMap)
    {
        _renderedTileMap = true;
        tileMap.DrawCulled(_cachedViewport.Pos, _cachedViewport.Size);
    }

    private static void DrawFallbackPlatform(ref Transform trans, ref CollisionBox col)
    {
        var p1 = (trans.Position - col.Size * 0.5f).ToVec2i();
        var p2 = (trans.Position + col.Size * 0.5f).ToVec2i();

        GameEngine.DrawBox(p1, p2, _cachedViewport.PlatformFallbackFillColor, true);
        GameEngine.DrawBox(p1, p2, _cachedViewport.PlatformFallbackBorderColor, false);
    }
}
