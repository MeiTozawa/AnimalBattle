namespace AnimalBattle.Player;

/// <summary>
///     自機の入力・武器・移動システムを一括登録する Flecs モジュール。
/// </summary>
public readonly struct PlayerModule : IFlecsModule
{
    public void InitModule(World world)
    {
        world.Module<PlayerModule>();

        world.AddPlayerInputSystem()               // 1. 入力意図取得
             .AddPlayerSpecialAbilitySystem()      // 2. 特殊能力発動制御
             .AddScreenCleaveSequenceSystem()      // 3. 全画面斬撃エフェクトシーケンス
             .AddRangedWeaponSystem()              // 4. 遠距離射撃武器機構
             .AddMeleeWeaponSystem()               // 4. 近接判定展開武器機構
             .AddPlayerDashSystem()                // 5. 突進（ダッシュ）制御
             .AddPlayerJumpSystem()                // 6. 跳躍（ジャンプ）制御
             .AddLocomotionSystem()                // 7. 水平加減速および重力制御
             .AddMovementSystem()                  // 8. 統合物理積分・足場衝突判定・境界 Clamp
             .AddSpringScaleSystem()               // 9. スプリング拡縮変形積分
             .AddPlayerAnimationStateSystem();     // 10. 自機状態機アニメーション遷移
    }
}
