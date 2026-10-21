using System;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Presentation.Rendering;

/// <summary>
///     照準器（AimReticle）のスクリーン幾何学描画を制御する純粋 ECS 描画システム。
///     戦闘状態（GameState.Battle）時にのみマウス位置へタクティカルリングとクロスラインを描画します。
/// </summary>
public static class AimReticleRenderSystem
{
    /// <summary>
    ///     照準器描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddAimReticleRenderSystem(this World world)
    {
        // レティクル画面描画 (OnStore)
        world.System("AimReticleRenderSystem")
            .Kind(Ecs.OnStore)
            .Iter(ExecuteAimReticleRender);

        return world;
    }

    /// <summary>
    ///     照準器描画パイプライン（名前付き静的メソッド）。
    /// </summary>
    public static void ExecuteAimReticleRender(Iter it)
    {
        var w = it.World();
        ref readonly var flow = ref w.Get<GameFlow>();
        if (flow.State != GameState.Battle) return;

        ref readonly var state = ref w.Get<AimReticleState>();
        var mouseScreen = InputService.GetMousePosition();
        RenderReticle(mouseScreen, state.Expansion, w.Get<RenderConfig>().AimReticle);
    }

    private static void RenderReticle(Vec2i center, float expansion, AimReticleRenderConfig cfg)
    {
        Debug.Assert(GameEngine.CameraOffset == Vec2i.Zero, "[AimReticleRenderSystem] 照準器はスクリーン空間固定で描画される必要があります。");
        var radius = Math.Clamp((int)MathF.Round(cfg.BaseRadius + expansion), cfg.MinRadius, cfg.MaxRadius);

        // 1. 中心高輝度ドット（照準中心コア）
        GameEngine.DrawCircle(center, cfg.CenterDotRadius, cfg.CenterDotColor, fill: true);

        // 2. 外周円環（タクティカルリング）
        GameEngine.DrawCircle(center, radius, cfg.RingColor, fill: false);

        // 3. 上下左右 4 方向の刻度ショートライン
        var inner = radius + cfg.TickGap;
        var outer = inner + cfg.TickLength;

        // 上
        GameEngine.DrawLine(new Vec2i(center.X, center.Y - inner), new Vec2i(center.X, center.Y - outer), cfg.TickColor);
        // 下
        GameEngine.DrawLine(new Vec2i(center.X, center.Y + inner), new Vec2i(center.X, center.Y + outer), cfg.TickColor);
        // 左
        GameEngine.DrawLine(new Vec2i(center.X - inner, center.Y), new Vec2i(center.X - outer, center.Y), cfg.TickColor);
        // 右
        GameEngine.DrawLine(new Vec2i(center.X + inner, center.Y), new Vec2i(center.X + outer, center.Y), cfg.TickColor);
    }
}
