using AnimalBattle.Systems;
using Flecs.NET.Core;

namespace AnimalBattle.Bosses;

/// <summary>
///     Boss の Prefab 原型、被弾裁定および攻撃予告描画システムを一括管理する Flecs モジュール。
/// </summary>
public readonly struct BossModule : IFlecsModule
{
    public void InitModule(World world)
    {
        world.Module<BossModule>();

        BossPrefabs.Register(world);

        world.AddBossDamageResolutionSystem();
    }
}
