using AnimalBattle.Features.Tutorial.Components;
using AnimalBattle.Features.Tutorial.Configs;
using AnimalBattle.Features.Tutorial.Systems;

namespace AnimalBattle.Features.Tutorial.Conditions;

/// <summary>
///     訓練用ミニオン（小怪）が全滅したかを判定する条件。
/// </summary>
public sealed class DefeatMinionsCondition : ITutorialCondition
{
    /// <inheritdoc />
    public bool IsSatisfied(World world, ref TutorialState state, float dt)
    {
        var ctx = world.Get<TutorialSystemContext>();
        var aliveMinions = ctx.MinionQuery.Count();

        state.TargetsRemaining = aliveMinions;

        if (aliveMinions == 0 && state.StepCompletionTimer <= 0f)
        {
            var cfg = world.Get<TutorialConfig>();
            state.StepCompletionTimer = cfg.Timing.MinionDefeatDelay;
        }

        if (aliveMinions == 0 && state.StepCompletionTimer > 0f)
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
