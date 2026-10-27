using AnimalBattle.Combat.Events;

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
             .AddMeleeMovementSystem()             // 3. 移動を伴う近接攻撃判定の座標更新
             .AddOrbitSatelliteMovementSystem()    // 4. 更新された親弾の最新座標に基づいて衛星弾を公転配置
             .AddMeleeBulletCancelSystem()         // 5. 近接斬落判定
             .AddHurtboxSpatialIndexSystem()       // 6. 食らい判定空間ハッシュ構築
             .AddCombatCollisionSystem()           // 7. 単発弾幕の衝突判定
             .AddPersistentCollisionSystem()       // 8. 持続判定
             .AddMeleeLifetimeSystem()             // 9. 近接攻撃判定の持続時間減衰と回収タグ付与
             .AddCombatRecycleSystem()             // 10. 画面外・被弾等で RecycleTag が付与されたエンティティを一括回収
             .AddDamageInboxClearSystem();         // 11. 被弾イベントバッファのフレーム末尾クリア
    }
}

/// <summary>
///     被弾イベントバッファクリアシステムの拡張メソッド。
/// </summary>
public static class DamageInboxClearExtensions
{
    /// <summary>
    ///     被弾イベントバッファクリアシステムを Flecs.NET ワールドへ登録します。
    /// </summary>
    public static World AddDamageInboxClearSystem(this World world)
    {
        world.System("DamageInboxClearSystem")
            .Kind(Ecs.PostUpdate)
            .Iter(static it => it.World().GetMut<DamageInbox>().Clear());

        return world;
    }
}
