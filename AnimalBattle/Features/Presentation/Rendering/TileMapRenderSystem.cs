using AnimalBattle.Components;
using AnimalBattle.Maps;

namespace AnimalBattle.Presentation.Rendering;

/// <summary>
///     World 単位で独立して保持されるタイルマップ描画システム用コンテキスト。
/// </summary>
internal sealed class TileMapRenderContext : IDisposable
{
    public required Query<Transform, CollisionBox> PlatformQuery { get; init; }
    public required Query<TileMap> TileMapQuery { get; init; }

    public void Dispose()
    {
        PlatformQuery.Dispose();
        TileMapQuery.Dispose();
    }
}

/// <summary>
///     アリーナ足場およびタイルマップの視口カリング描画を行う ECS システム。
/// </summary>
public static class TileMapRenderSystem
{


    private static bool _renderedTileMap;
    private static ViewportRenderConfig _cachedViewport = null!;

    /// <summary>
    ///     タイルマップ描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddTileMapRenderSystem(this World world)
    {
        var platformQuery = world.QueryBuilder<Transform, CollisionBox>()
            .With<PlatformTag>()
            .Build();

        var tileMapQuery = world.QueryBuilder<TileMap>().Build();

        world.Set(new TileMapRenderContext
        {
            PlatformQuery = platformQuery,
            TileMapQuery = tileMapQuery
        });

        world.System("TileMapRenderSystem")
            .Kind(Ecs.OnStore)
            .Iter(ExecuteTileMapRender);

        return world;
    }

    /// <summary>
    ///     タイルマップ描画パイプライン：名前付き静的メソッド。
    /// </summary>
    public static void ExecuteTileMapRender(Iter it)
    {
        var w = it.World();
        if (w.Get<GameFlow>().State != GameState.Battle) return;

        var ctx = w.Get<TileMapRenderContext>();
        _cachedViewport = w.Get<RenderConfig>().Viewport;
        _renderedTileMap = false;
        ctx.TileMapQuery.Each(DrawTileMap);

        if (!_renderedTileMap)
        {
            ctx.PlatformQuery.Each(DrawFallbackPlatform);
        }
    }

    private static void DrawTileMap(ref TileMap tileMap)
    {
        _renderedTileMap = true;
        // カメラ移動・シェイク：CameraService.RenderOffsetを考慮してスクリーン視口をワールド座標系へ変換
        var camOffset = CameraService.RenderOffset;
        var worldViewportPos = _cachedViewport.Pos - (Vec2f)camOffset;
        tileMap.DrawCulled(worldViewportPos, _cachedViewport.Size);
    }

    private static void DrawFallbackPlatform(ref Transform trans, ref CollisionBox col)
    {
        var p1 = (trans.Position - col.Size * 0.5f).ToVec2i();
        var p2 = (trans.Position + col.Size * 0.5f).ToVec2i();

        GameEngine.DrawBox(p1, p2, _cachedViewport.PlatformFallbackFillColor, true);
        GameEngine.DrawBox(p1, p2, _cachedViewport.PlatformFallbackBorderColor, false);
    }
}
