using AnimalBattle.Enemies.Services;
using AnimalBattle.Features.Tutorial.Components;

namespace AnimalBattle.Features.Tutorial.Actions;

/// <summary>
///     訓練用ミニオン（小怪）をワールドへ生成するアクション。
/// </summary>
public sealed class SpawnMinionAction : ITutorialAction
{
    private readonly MinionType _minionType;

    public SpawnMinionAction(MinionType minionType)
    {
        _minionType = minionType;
    }

    /// <inheritdoc />
    public void Execute(World world, Entity root)
    {
        var pos = MinionSpawnService.SelectBestSpawnPoint(world, _minionType);
        MinionSpawner.SpawnMinion(world, root, _minionType, pos);

        ref var state = ref world.GetMut<TutorialState>();
        state.TargetsRemaining++;
    }
}
