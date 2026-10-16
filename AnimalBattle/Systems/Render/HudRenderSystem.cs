using AnimalBattle.Characters;
using AnimalBattle.Characters.Boss;
using AnimalBattle.Components;
using AnimalBattle.Renderer;
using Flecs.NET.Core;

namespace AnimalBattle.Systems.Render;

/// <summary>
///     自機ステータス（HP、二段ジャンプ残数）、Boss 大 HP バー、および操作ガイドを描画する ECS システム。
///     戦闘状態（GameState.Battle）時にスクリーン空間へ直接描画します。
/// </summary>
public static class HudRenderSystem {
    /// <summary>
    ///     HUD 描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddHudRenderSystem(this World world)
    {
        var playerQuery = world.QueryBuilder<PlayerPawn>().Build();
        var bossQuery = world.QueryBuilder<BaseBossAgent>().Build();

        world.System("HudRenderSystem")
            .Kind(Ecs.OnStore)
            .Iter((Iter it) =>
            {
                var w = it.World();
                ref readonly var flow = ref w.Get<GameFlow>();
                if (flow.State != GameState.Battle) return;

                PlayerPawn? player = null;
                playerQuery.Each((ref PlayerPawn p) => player = p);

                BaseBossAgent? boss = null;
                bossQuery.Each((ref BaseBossAgent b) => boss = b);

                var remainingJumps = player?.RemainingJumps ?? 0;

                HudRenderer.Render(player, boss, remainingJumps);
            });

        return world;
    }
}
