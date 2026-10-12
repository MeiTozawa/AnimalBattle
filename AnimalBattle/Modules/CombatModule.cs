using AnimalBattle.Systems;
using Flecs.NET.Core;

namespace AnimalBattle.Modules;

/// <summary>
///     キャラクターキネマティック運動、武器射撃、および戦闘幾何衝突判定システムを一括管理する戦闘基盤モジュール。
/// </summary>
public readonly struct CombatModule : IFlecsModule
{
    /// <summary>
    ///     戦闘基盤関連の ECS システム群をワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    public void InitModule(World world)
    {
        world.Module<CombatModule>();

        world.AddMovementSystem()
             .AddWeaponSystem()
             .AddCombatCollisionSystem()
             .AddCombatRecycleSystem();
    }
}
