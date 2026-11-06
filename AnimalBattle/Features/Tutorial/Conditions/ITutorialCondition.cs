using AnimalBattle.Features.Tutorial.Components;

namespace AnimalBattle.Features.Tutorial.Conditions;

/// <summary>
///     チュートリアルステップの達成条件判定インターフェース（毎フレーム評価、ゼロアロケーション）。
/// </summary>
public interface ITutorialCondition
{
    /// <summary>
    ///     ステップの条件が満たされたかを判定します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="state">チュートリアル進行状態（可変参照）</param>
    /// <param name="dt">前フレームからの経過時間（秒）</param>
    /// <returns>条件が満たされた場合は true</returns>
    bool IsSatisfied(World world, ref TutorialState state, float dt);
}
