using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Maps;
using Flecs.NET.Core;

namespace AnimalBattle.Systems.Render;

/// <summary>
///     アリーナ足場およびタイルマップの視口カリング描画を行う ECS システム。
/// </summary>
public static class TileMapRenderSystem {
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

        world.System("TileMapRenderSystem")
            .Kind(Ecs.OnStore)
            .Iter((Iter it) =>
            {
                var w = it.World();
                if (w.Get<GameFlow>().State != GameState.Battle) return;

                var viewport = w.Get<RenderConfig>().Viewport;

                var renderedTileMap = false;
                tileMapQuery.Each((ref TileMap tileMap) =>
                {
                    renderedTileMap = true;
                    tileMap.DrawCulled(viewport.Pos, viewport.Size);
                });

                if (!renderedTileMap)
                {
                    platformQuery.Each((ref Transform trans, ref CollisionBox col) =>
                    {
                        var p1 = (trans.Position - col.Size * 0.5f).ToVec2i();
                        var p2 = (trans.Position + col.Size * 0.5f).ToVec2i();

                        GameEngine.DrawBox(p1, p2, viewport.PlatformFallbackFillColor, true);
                        GameEngine.DrawBox(p1, p2, viewport.PlatformFallbackBorderColor, false);
                    });
                }
            });

        return world;
    }
}
