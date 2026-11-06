using AnimalBattle.Features.Tutorial.Components;
using AnimalBattle.Features.Tutorial.Configs;
using AnimalBattle.Features.Tutorial.Systems;

namespace AnimalBattle.Features.Tutorial.Conditions;

/// <summary>
///     ワールド内の全目標物（コイン／リング）が回収されたかを判定する条件。
/// </summary>
public sealed class CollectTargetsCondition : ITutorialCondition
{
    /// <inheritdoc />
    public bool IsSatisfied(World world, ref TutorialState state, float dt)
    {
        var ctx = world.Get<TutorialSystemContext>();
        var aliveCount = ctx.TargetQuery.Count();

        state.TargetsRemaining = aliveCount;

        if (aliveCount == 0 && state.StepCompletionTimer <= 0f)
        {
            var cfg = world.Get<TutorialConfig>();
            state.StepCompletionTimer = cfg.Timing.TargetCollectDelay;
        }

        if (aliveCount == 0 && state.StepCompletionTimer > 0f)
        {
            state.StepCompletionTimer -= dt;
            if (state.StepCompletionTimer <= 0f)
            {
                return true;
            }
        }

        return false;
    }
}
