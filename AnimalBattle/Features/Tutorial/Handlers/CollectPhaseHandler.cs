using AnimalBattle.Features.Tutorial.Components;
using AnimalBattle.Features.Tutorial.Configs;

namespace AnimalBattle.Features.Tutorial.Handlers;

/// <summary>
///     目標物（コイン／リング）収集フェーズの生成・達成判定ハンドラー。
/// </summary>
public sealed class CollectPhaseHandler : ITutorialPhaseHandler
{
    /// <inheritdoc />
    public void Setup(World world, Entity root, TutorialPhaseConfig config, TutorialConfig globalConfig)
    {
        Debug.Assert(config != null, "フェーズ設定が null です。");
        if (config == null) return;

        ref var state = ref world.GetMut<TutorialState>();
        state.TargetsRemaining = config.Targets.Length;

        for (var i = 0; i < config.Targets.Length; i++)
        {
            var item = config.Targets[i];
            world.Entity($"TutorialTarget_{i}_{item.Pos.X}_{item.Pos.Y}")
                .ChildOf(root)
                .Set(new Transform(item.Pos, Vec2f.One))
                .Set(new TutorialTarget
                {
                    Radius = item.Radius,
                    IsCollected = false,
                    FloatPhase = 0f
                });
        }
    }

    /// <inheritdoc />
    public bool UpdateAndCheckCompletion(World world, ref TutorialState state, TutorialPhaseConfig config, TutorialConfig globalConfig, float dt)
    {
        if (state.TargetsRemaining <= 0 && !state.IsPhaseCompleted)
        {
            state.IsPhaseCompleted = true;
            state.CompletionTimer = globalConfig.Timing.TargetCollectDelay;
        }

        if (state.IsPhaseCompleted)
        {
            state.CompletionTimer -= dt;
            if (state.CompletionTimer <= 0f)
            {
                return true;
            }
        }

        return false;
    }
}
