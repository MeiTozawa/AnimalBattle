using AnimalBattle.Systems;
using Flecs.NET.Core;

namespace AnimalBattle.Modules;

/// <summary>
///     弾物理（等速直線移動・画面外消滅判定）、特殊弾幕変調（正弦波・脈動）、および周回衛星弾幕システムを
///     一括管理する弾幕モジュール。
/// </summary>
public readonly struct BulletModule : IFlecsModule {
    /// <summary>
    ///     全弾幕物理・特殊変調・周回衛星関連の ECS システムをワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    public void InitModule(World world)
    {
        world.Module<BulletModule>();

        world.AddBulletPhysicsSystem()
             .AddSpecialBulletSystem()
             .AddOrbitSatelliteMovementSystem();
    }
}