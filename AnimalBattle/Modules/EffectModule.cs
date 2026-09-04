using System;
using AnimalBattle.Systems;
using Flecs.NET.Core;

namespace AnimalBattle.Modules;

/// <summary>
/// フェード・ヒットフラッシュ・スラッシュ等の演出エフェクトシステム群を一括構築するドメインモジュール。
/// 各種単一責任の Effect System クラスを順次登録します。
/// </summary>
public readonly struct EffectModule : IFlecsModule
{
    /// <summary>
    /// スラッシュ演出・フェード・ヒットフラッシュのエフェクトシステムをワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    public void InitModule(World world)
    {
        world.Module<EffectModule>();

        world.AddEffectMovementSystem()
             .AddFadeEffectSystem()
             .AddHitFlashEffectSystem();
    }
}
