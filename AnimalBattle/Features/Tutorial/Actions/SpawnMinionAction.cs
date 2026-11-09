using System;
using AnimalBattle.Enemies.Services;
using AnimalBattle.Features.Tutorial.Components;

namespace AnimalBattle.Features.Tutorial.Actions;

/// <summary>
///     訓練用ミニオン（小怪）をワールドへ生成するアクション。
/// </summary>
public sealed class SpawnMinionAction : ITutorialAction
{
    private readonly MinionType _minionType;
    private readonly int _count;

    public SpawnMinionAction(MinionType minionType, int count = 1)
    {
        _minionType = minionType;
        _count = Math.Max(1, count);
    }

    /// <inheritdoc />
    public void Execute(World world, Entity root)
    {
        var spawnPoints = MinionSpawnService.SelectBestSpawnPoints(world, _minionType, _count);
        for (var i = 0; i < spawnPoints.Count; i++)
        {
            MinionSpawner.SpawnMinion(world, root, _minionType, spawnPoints[i]);
        }

        ref var state = ref world.GetMut<TutorialState>();
        state.TargetsRemaining += spawnPoints.Count;
    }
}
