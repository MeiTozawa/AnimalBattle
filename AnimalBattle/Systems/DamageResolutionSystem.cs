using AnimalBattle.Modules;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     ダメージ裁定関連システムの後方互換用ヘルパークラス。
///     個別システムは <see cref="TimedTagSystem" />、<see cref="PlayerDamageResolutionSystem" />、
///     <see cref="BossDamageResolutionSystem" /> に分割され、<see cref="DamageResolutionModule" /> で集約管理されます。
/// </summary>
public static class DamageResolutionSystem
{
    /// <summary>
    ///     全ダメージ裁定関連の ECS システムを一括登録します（<see cref="DamageResolutionModule"/> のインポートを委譲）。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddDamageResolutionSystem(this World world)
    {
        world.Import<DamageResolutionModule>();
        return world;
    }
}
