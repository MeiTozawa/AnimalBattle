using AnimalBattle.Features.Tutorial.Components;

namespace AnimalBattle.Features.Tutorial.Actions;

/// <summary>
///     目標物（コイン／リング）をワールドへ生成するアクション。
/// </summary>
public sealed class SpawnTargetAction : ITutorialAction
{
    private readonly Vec2f _pos;
    private readonly float _radius;

    public SpawnTargetAction(Vec2f pos, float radius)
    {
        _pos = pos;
        _radius = radius;
    }

    /// <inheritdoc />
    public void Execute(World world, Entity root)
    {
        world.Entity($"TutorialTarget_{_pos.X}_{_pos.Y}")
            .ChildOf(root)
            .Set(new Transform(_pos, Vec2f.One))
            .Set(new TutorialTarget
            {
                Radius = _radius,
                IsCollected = false,
                FloatPhase = 0f
            });

        ref var state = ref world.GetMut<TutorialState>();
        state.TargetsRemaining++;
    }
}
