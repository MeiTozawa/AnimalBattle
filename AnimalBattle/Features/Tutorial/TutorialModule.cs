using AnimalBattle.Features.Tutorial.Systems;

namespace AnimalBattle.Features.Tutorial;

/// <summary>
///     チュートリアル関連の全 ECS システム（進行フロー・インタラクション・UI描画）を登録するモジュール構造体。
/// </summary>
public readonly struct TutorialModule : IFlecsModule
{
    /// <summary>
    ///     チュートリアルシステム群を Flecs ワールドへ一括バインドします。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    public void InitModule(World world)
    {
        world.Module<TutorialModule>();

        world.AddTutorialFlowSystem()
             .AddTutorialInteractionSystem();
    }
}
