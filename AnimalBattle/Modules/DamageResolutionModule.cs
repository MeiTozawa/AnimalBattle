using AnimalBattle.Systems;
using Flecs.NET.Core;

namespace AnimalBattle.Modules;

/// <summary>
///     無敵タイマー減衰、自機被弾裁定、および Boss 被弾裁定システムを一括管理するダメージ裁定モジュール。
///     単一責任の <see cref="HealthInvincibilitySystem" />、<see cref="PlayerDamageResolutionSystem" />、
///     <see cref="BossDamageResolutionSystem" /> を順次 Flecs.NET ワールドへ登録します。
/// </summary>
public readonly struct DamageResolutionModule : IFlecsModule
{
    /// <summary>
    ///     ダメージ裁定関連の ECS システム群をワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    public void InitModule(World world)
    {
        world.Module<DamageResolutionModule>();

        world.AddHealthInvincibilitySystem()
             .AddPlayerDamageResolutionSystem()
             .AddBossDamageResolutionSystem();
    }
}
