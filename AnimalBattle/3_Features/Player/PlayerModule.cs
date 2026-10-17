using AnimalBattle.Systems;
using Flecs.NET.Core;

namespace AnimalBattle.Player;

/// <summary>
///     プレイヤー自機の Prefab 原型および各種制御・兵装・アニメーション・被弾裁定システムを一括管理する Flecs モジュール。
/// </summary>
public readonly struct PlayerModule : IFlecsModule
{
    public void InitModule(World world)
    {
        world.Module<PlayerModule>();

        PlayerPrefabs.Register(world);

        world.AddMovementSystem()
             .AddCatWeaponSystem()
             .AddBunnyWeaponSystem()
             .AddPlayerDamageResolutionSystem();
    }
}
