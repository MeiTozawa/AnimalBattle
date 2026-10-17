using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     ゲーム開始・選人画面の入力システムおよび UI 描画システムを一括登録するオーケストレーター。
/// </summary>
public static class StartSceneSystems {
    /// <summary>
    ///     開始選人画面システム群（入力判定および UI 描画）を Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddStartSceneSystems(this World world)
    {
        return world.AddStartUiInputSystem()
                    .AddStartUiRenderSystem();
    }
}
