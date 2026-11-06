using AnimalBattle.Features.Tutorial.Components;

namespace AnimalBattle.Features.Tutorial.Conditions;

/// <summary>
///     ボス挑戦扉へのインタラクション待機条件（TutorialInteractionSystem側でFキー押下により次シーンへ直接遷移するため、フロー側では完了しない）。
/// </summary>
public sealed class InteractDoorCondition : ITutorialCondition
{
    /// <inheritdoc />
    public bool IsSatisfied(World world, ref TutorialState state, float dt)
    {
        return false;
    }
}
