namespace AnimalBattle.Player;

/// <summary>
///     自機の Prefab 登録および入力・武器・移動システムを一括登録する Flecs モジュール。
/// </summary>
public readonly struct PlayerModule : IFlecsModule
{
    public void InitModule(World world)
    {
        world.Module<PlayerModule>();

        PlayerPrefabs.Register(world);

        world.AddPlayerInputSystem()               // 1. 入力意図取得
             .AddPlayerSpecialAbilitySystem()      // 2. 特殊能力発動制御
             .AddRangedWeaponSystem()              // 3. 遠距離射撃武器機構
             .AddMeleeWeaponSystem()               // 4. 近接判定展開武器機構
             .AddMovementSystem()                  // 5. 統合物理積分・足場衝突判定・境界 Clamp
             .AddSpringScaleSystem()               // 6. スプリング拡縮変形積分
             .AddPlayerAnimationStateSystem();     // 7. 自機状態機アニメーション遷移
    }
}
