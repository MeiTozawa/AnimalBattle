using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Systems.Render;

/// <summary>
///     スクリーン空間描画（HUD、照準器、UI）直前にカメラオフセットを (0, 0) にリセットする純粋 ECS システム。
///     画面固定 UI へのカメラ振動・追従の波及を遮断します。
/// </summary>
public static class CameraOffsetResetSystem {
    /// <summary>
    ///     カメラオフセットリセットシステムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddCameraOffsetResetSystem(this World world)
    {
        world.System("CameraOffsetResetSystem")
             .Kind(Ecs.OnStore)
             .Iter((Iter it) =>
             {
                 GameEngine.SetCameraOffset(0, 0);
             });

        return world;
    }
}
