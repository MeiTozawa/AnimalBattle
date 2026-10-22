using AnimalBattle.Combat.Systems;
using AnimalBattle.Systems;
using Flecs.NET.Core;

namespace AnimalBattle.Combat;

/// <summary>
///     戦闘判定・弾幕物理・近接相殺・時限タグおよび回収システムを一括管理する Flecs モジュール。
/// </summary>
public readonly struct CombatModule : IFlecsModule
{
    public void InitModule(World world)
    {
        world.Module<CombatModule>();

        CombatPrefabs.Register(world);

        world.AddTimedTagSystem()
             .AddSpecialBulletSystem()            // 1. 特殊弾幕の変調：法線正弦波速度・判定拡縮を先行計算
             .AddBulletPhysicsSystem()             // 2. 最新の変調速度を用いて座標を更新し、画面外判定を実施
             .AddOrbitSatelliteMovementSystem()    // 3. 更新された親弾の最新座標に基づいて衛星弾を公転配置
             .AddMeleeBulletCancelSystem()         // 4. 近接斬落判定
             .AddHurtboxSpatialIndexSystem()       // 5. 食らい判定空間ハッシュ構築
             .AddCombatCollisionSystem()           // 6. 単発弾幕の衝突判定
             .AddPersistentCollisionSystem()       // 7. 持続判定
             .AddMeleeLifetimeSystem()             // 8. 近接攻撃判定の持続時間減衰と回収タグ付与
             .AddCombatRecycleSystem();            // 9. 画面外・被弾等で RecycleTag が付与されたエンティティを一括回収
    }
}
