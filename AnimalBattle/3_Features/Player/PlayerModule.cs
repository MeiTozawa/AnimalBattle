using AnimalBattle.Systems;
using Flecs.NET.Core;

namespace AnimalBattle.Player;

/// <summary>
///     自機の Prefab 登録および入力・移動・武器・ダメージ判定システムを一括登録する Flecs モジュール。
/// </summary>
public readonly struct PlayerModule : IFlecsModule
{
    public void InitModule(World world)
    {
        world.Module<PlayerModule>();

        PlayerPrefabs.Register(world);

        world.AddPlayerInputSystem()
             .AddMovementSystem()
             .AddCatWeaponSystem()
             .AddBunnyWeaponSystem()
             .AddPlayerDamageResolutionSystem();
    }
}
