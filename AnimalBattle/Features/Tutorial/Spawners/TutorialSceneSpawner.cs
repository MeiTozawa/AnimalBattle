using AnimalBattle.Ai;
using AnimalBattle.Features.Tutorial.Components;

namespace AnimalBattle.Features.Tutorial.Spawners;

/// <summary>
///     チュートリアルシーンの全エンティティの生成と破棄を管理するスポナークラス。
/// </summary>
public static class TutorialSceneSpawner
{
    public const string RootName = "TutorialSceneRoot";

    /// <summary>
    ///     チュートリアルシーンのルートおよび必要エンティティを生成します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="playerType">選択されたプレイヤーキャラクター種別</param>
    /// <param name="initialPhase">初期フェーズ</param>
    public static void Spawn(World world, CharacterType playerType, TutorialPhase initialPhase)
    {
        Teardown(world);
    }

    /// <summary>
    ///     チュートリアルシーンのルートおよび全配下エンティティを破棄します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    public static void Teardown(World world)
    {
        var root = world.Lookup(RootName);
        if (root.IsAlive())
        {
            root.Destruct();
        }
    }
}
