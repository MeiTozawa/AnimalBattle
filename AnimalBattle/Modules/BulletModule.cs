using System;
using AnimalBattle.Systems;
using Flecs.NET.Core;

namespace AnimalBattle.Modules;

/// <summary>
/// 弾幕 Boss 戦におけるスプライトアニメーション、弾物理（SIMD 計算委譲）および
/// 戦闘幾何衝突判定システムを Flecs.NET ECS 常駐 Routine として一括登録するネイティブモジュール。
/// 表現層（描画・UI）や入力制御を排除し、純粋な物理・衝突シミュレーションに特化します。
/// </summary>
public readonly struct BulletModule : IFlecsModule
{
    /// <summary>
    /// 全戦闘物理・衝突関連の ECS 常駐 Routine (System) をワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    public void InitModule(World world)
    {
        world.Module<BulletModule>();

        world.AddSpriteAnimationSystem()
             .AddBulletPhysicsSystem()
             .AddSpecialBulletSystem()
             .AddCombatCollisionSystem();
    }
}
