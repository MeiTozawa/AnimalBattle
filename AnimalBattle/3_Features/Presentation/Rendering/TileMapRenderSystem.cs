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
                    tileMap.DrawCulled(viewport.X, viewport.Y, viewport.Width, viewport.Height);
                });

                if (!renderedTileMap)
                {
                    platformQuery.Each((ref Transform trans, ref CollisionBox col) =>
                    {
                        var x1 = (int)(trans.X - col.Width / 2.0f);
                        var y1 = (int)(trans.Y - col.Height / 2.0f);
                        var x2 = (int)(trans.X + col.Width / 2.0f);
                        var y2 = (int)(trans.Y + col.Height / 2.0f);

                        GameEngine.DrawBox(x1, y1, x2, y2, viewport.PlatformFallbackFillColor, true);
                        GameEngine.DrawBox(x1, y1, x2, y2, viewport.PlatformFallbackBorderColor, false);
                    });
                }
            });

        return world;
    }
}
