using AnimalBattle.Components;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Systems.Render;

/// <summary>
///     ワールド空間描画直前にカメラオフセットを DxLib 描画コンテキストへ適用する純粋 ECS システム。
///     戦闘状態（GameState.Battle）時にのみカメラ座標を適用し、それ以外では (0, 0) を設定します。
/// </summary>
public static class CameraOffsetApplySystem {
    /// <summary>
    ///     カメラオフセット適用システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddCameraOffsetApplySystem(this World world)
    {
        world.System("CameraOffsetApplySystem")
             .Kind(Ecs.OnStore)
             .Iter((Iter it) =>
             {
                 var w = it.World();
                 if (w.Get<GameFlow>().State == GameState.Battle)
                 {
                     GameEngine.SetCameraOffset(CameraService.RenderOffset);
                 }
                 else
                 {
                     GameEngine.SetCameraOffset(Vec2i.Zero);
                 }
             });

        return world;
    }
}
