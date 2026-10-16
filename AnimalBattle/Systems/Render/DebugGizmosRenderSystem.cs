using AnimalBattle.Components;
using AnimalBattle.Maps;
using AnimalBattle.Renderer;
using Flecs.NET.Core;

namespace AnimalBattle.Systems.Render;

/// <summary>
///     衝突枠（CollisionBox）、攻撃判定枠（Hitbox）、および被弾枠（Hurtbox）のデバッグ枠線を描画する ECS システム。
///     F1 または H キーによるトグル切り替えに対応します。
/// </summary>
public static class DebugGizmosRenderSystem {
    private static DebugRenderer? _renderer;

    /// <summary>
    ///     デバッグギズモ描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddDebugGizmosRenderSystem(this World world)
    {
        _renderer ??= new DebugRenderer(world);

        var platformQuery = world.QueryBuilder<Transform, CollisionBox>()
            .With<PlatformTag>()
            .Build();

        var tileMapQuery = world.QueryBuilder<TileMap>().Build();

        world.System("DebugGizmosRenderSystem")
            .Kind(Ecs.OnStore)
            .Iter((Iter it) =>
            {
                var w = it.World();
                if (w.Get<GameFlow>().State != GameState.Battle) return;

                TileMap? tileMap = null;
                tileMapQuery.Each((ref TileMap tm) => tileMap = tm);
                _renderer.Render(platformQuery, tileMap);
            });

        return world;
    }
}
