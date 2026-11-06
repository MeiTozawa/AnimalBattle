using AnimalBattle.Features.Items.Configs;
using AnimalBattle.Features.Items.Spawners;

namespace AnimalBattle.Features.Tutorial.Actions;

/// <summary>
///     ワールドの指定位置へ回復血包（HealthPack）アイテムを生成するアクション。
/// </summary>
public sealed class SpawnHealthPackAction(Vec2f pos) : ITutorialAction {
    /// <inheritdoc />
    public void Execute(World world, Entity root)
    {
        var itemCfg = world.Get<ItemConfig>();
        var pack = ItemSpawner.SpawnHealthPack(world, pos, itemCfg.HealthPack);
        pack.ChildOf(root);
    }
}
