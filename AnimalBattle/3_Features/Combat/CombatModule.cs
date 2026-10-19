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
             .AddBulletPhysicsSystem()
             .AddSpecialBulletSystem()
             .AddOrbitSatelliteMovementSystem()
             .AddMeleeBulletCancelSystem()
             .AddHurtboxSpatialIndexSystem()
             .AddCombatCollisionSystem()
             .AddPersistentCollisionSystem()
             .AddCombatRecycleSystem();
    }
}
