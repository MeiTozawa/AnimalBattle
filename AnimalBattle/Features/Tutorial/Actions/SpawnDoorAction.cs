using AnimalBattle.Ai;
using AnimalBattle.Features.Tutorial.Components;
using AnimalBattle.Features.Tutorial.Configs;

namespace AnimalBattle.Features.Tutorial.Actions;

/// <summary>
///     ボス挑戦扉をワールドへ生成するアクション。
/// </summary>
public sealed class SpawnDoorAction : ITutorialAction
{
    private readonly Vec2f _pos;
    private readonly BossType _bossType;

    public SpawnDoorAction(Vec2f pos, BossType bossType)
    {
        _pos = pos;
        _bossType = bossType;
    }

    /// <inheritdoc />
    public void Execute(World world, Entity root)
    {
        var interactionRadius = world.Get<TutorialRenderConfig>().Doors.InteractionRadius;

        world.Entity($"TutorialDoor_{_bossType}_{_pos.X}_{_pos.Y}")
            .ChildOf(root)
            .Set(new Transform(_pos, Vec2f.One))
            .Set(new TutorialDoor
            {
                TargetBoss = _bossType,
                InteractionRadius = interactionRadius,
                IsPlayerInRange = false,
                AnimTimer = 0f
            });
    }
}
