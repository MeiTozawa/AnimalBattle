namespace AnimalBattle.Scenes.Start;

/// <summary>
///     ゲーム開始・キャラクター選択画面の入力判定および UI 描画システムを一括管理する Flecs シーンモジュール。
/// </summary>
public readonly struct StartSceneModule : IFlecsModule {
    /// <summary>
    ///     キャラクター選択画面の ECS システム群：入力処理および UI 描画を Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    public void InitModule(World world)
    {
        world.Module<StartSceneModule>();

        world.AddStartUiInputSystem()
             .AddStartUiRenderSystem();
    }
}
