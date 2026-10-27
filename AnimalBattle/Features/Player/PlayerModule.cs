using AnimalBattle.Player.Systems;
using Flecs.NET.Core;

namespace AnimalBattle.Player;

/// <summary>
///     自機の Prefab 登録および入力・武器・移動・ダメージ判定システムを一括登録する Flecs モジュール。
/// </summary>
public readonly struct PlayerModule : IFlecsModule
{
    public void InitModule(World world)
    {
        world.Module<PlayerModule>();

        PlayerPrefabs.Register(world);

        world.AddPlayerInputSystem()               // 1. 入力意図取得
             .AddRangedWeaponSystem()              // 2. 遠距離射撃武器機構
             .AddMeleeWeaponSystem()               // 3. 近接判定展開武器機構
             .AddMovementSystem()                  // 4. 統合物理積分・足場衝突判定・境界 Clamp
             .AddPlayerDamageResolutionSystem();   // 5. 被弾ダメージ・ヒットストップ処理
    }
}
