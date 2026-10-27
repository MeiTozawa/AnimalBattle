namespace AnimalBattle.Presentation.Rendering;

/// <summary>
///     スクリーン空間描画：HUD、照準器、UI直前にカメラオフセットを ゼロ座標 にリセットする純粋 ECS システム。
///     画面固定 UI へのカメラ振動・追従の波及を遮断します。
/// </summary>
public static class CameraOffsetResetSystem
{
    /// <summary>
    ///     カメラオフセットリセットシステムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddCameraOffsetResetSystem(this World world)
    {
        world.System("CameraOffsetResetSystem")
             .Kind(Ecs.OnStore)
             .Iter(ExecuteCameraOffsetReset);

        return world;
    }

    /// <summary>
    ///     カメラオフセットリセット処理：名前付き静的メソッド。
    /// </summary>
    public static void ExecuteCameraOffsetReset(Iter it)
    {
        GameEngine.SetCameraOffset(Vec2i.Zero);
    }
}
