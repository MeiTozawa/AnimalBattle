using AnimalBattle.Components;
using AnimalBattle.Renderer;
using Flecs.NET.Core;

namespace AnimalBattle.Systems.Render;

/// <summary>
///     照準器（AimReticle）のスプリング拡縮状態更新およびスクリーン描画を制御する ECS システム。
///     戦闘状態（GameState.Battle）時にのみ照準レティクルを描画します。
/// </summary>
public static class AimReticleRenderSystem {
    /// <summary>
    ///     照準器システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddAimReticleRenderSystem(this World world)
    {
        AimReticleRenderer.Initialize();

        // 1. レティクル拡縮スプリング物理更新 (PreUpdate)
        world.System("AimReticleUpdateSystem")
            .Kind(Ecs.PreUpdate)
            .Iter((Iter it) =>
            {
                AimReticleRenderer.Update(it.DeltaTime());
            });

        // 2. レティクル画面描画 (OnStore)
        world.System("AimReticleRenderSystem")
            .Kind(Ecs.OnStore)
            .Iter((Iter it) =>
            {
                var w = it.World();
                ref readonly var flow = ref w.Get<GameFlow>();
                if (flow.State != GameState.Battle) return;

                AimReticleRenderer.Render();
            });

        return world;
    }
}
