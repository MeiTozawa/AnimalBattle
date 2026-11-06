using AnimalBattle.Features.Tutorial.Components;

namespace AnimalBattle.Features.Tutorial.Conditions;

/// <summary>
///     指定時間の経過を待つタイマー条件。
/// </summary>
public sealed class TimerCondition : ITutorialCondition
{
    private readonly float _duration;

    public TimerCondition(float duration)
    {
        _duration = duration;
    }

    /// <inheritdoc />
    public bool IsSatisfied(World world, ref TutorialState state, float dt)
    {
        return state.StepTimer >= _duration;
    }
}
