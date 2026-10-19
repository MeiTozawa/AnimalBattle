using AnimalBattle.Systems;
using Flecs.NET.Core;

namespace AnimalBattle.Arena;

/// <summary>
///     アリーナ地形・常駐足場衝突判定システムを管理する Flecs モジュール。
/// </summary>
public readonly struct ArenaModule : IFlecsModule
{
    public void InitModule(World world)
    {
        world.Module<ArenaModule>();
    }
}
