namespace AnimalBattle.Features.Items;

using AnimalBattle.Features.Items.Systems;
using Flecs.NET.Core;

/// <summary>
///     アイテムシステム関連の各 ECS システムをワールドへ一括インポートする Flecs モジュール。
/// </summary>
public readonly struct ItemModule : IFlecsModule
{
    /// <summary>
    ///     モジュール初期化時にアイテムシステムの各システムをワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs ワールドインスタンス</param>
    public void InitModule(World world)
    {
        world.Module<ItemModule>();

        world.AddItemDirectorSystem();
        world.AddItemPhysicsSystem();
        world.AddItemPickupSystem();
    }
}
