using System.Diagnostics;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Systems.Render;

/// <summary>
///     照準器（AimReticle）のスクリーン幾何学描画を制御する純粋 ECS 描画システム。
///     戦闘状態（GameState.Battle）時にのみマウス位置へタクティカルリングとクロスラインを描画します。
/// </summary>
public static class AimReticleRenderSystem {
    /// <summary>
    ///     照準器描画システム（および関連する物理更新システム）を Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddAimReticleRenderSystem(this World world)
    {
        // 物理更新システムを併せて登録
        world.AddAimReticleUpdateSystem();

        // レティクル画面描画 (OnStore)
        world.System("AimReticleRenderSystem")
            .Kind(Ecs.OnStore)
            .Iter((Iter it) =>
            {
                var w = it.World();
                ref readonly var flow = ref w.Get<GameFlow>();
                if (flow.State != GameState.Battle) return;

                ref readonly var state = ref w.Get<AimReticleState>();
                var mouseScreen = InputService.GetMousePosition();
                RenderReticle(mouseScreen.X, mouseScreen.Y, state.Expansion);
            });

        return world;
    }

    private static void RenderReticle(int x, int y, float expansion)
    {
        var cfg = ConfigManager.Render.AimReticle;
        var radius = Math.Clamp((int)MathF.Round(cfg.BaseRadius + expansion), cfg.MinRadius, cfg.MaxRadius);

        // 1. 中心高輝度ドット（照準中心コア）
        GameEngine.DrawCircle(x, y, cfg.CenterDotRadius, cfg.CenterDotColor, fill: true);

        // 2. 外周円環（タクティカルリング）
        GameEngine.DrawCircle(x, y, radius, cfg.RingColor, fill: false);

        // 3. 上下左右 4 方向の刻度ショートライン
        var inner = radius + cfg.TickGap;
        var outer = inner + cfg.TickLength;

        // 上
        GameEngine.DrawLine(x, y - inner, x, y - outer, cfg.TickColor);
        // 下
        GameEngine.DrawLine(x, y + inner, x, y + outer, cfg.TickColor);
        // 左
        GameEngine.DrawLine(x - inner, y, x - outer, y, cfg.TickColor);
        // 右
        GameEngine.DrawLine(x + inner, y, x + outer, y, cfg.TickColor);
    }
}
