using Flecs.NET.Core;

namespace AnimalBattle.Presentation.Vfx;

/// <summary>
///     フェード・ヒットフラッシュ・スラッシュ等の演出エフェクトシステム群および VfxPrefabs を一括構築するドメインモジュール。
/// </summary>
public readonly struct EffectModule : IFlecsModule {
    public void InitModule(World world)
    {
        world.Module<EffectModule>();

        VfxPrefabs.Register(world);

        world.AddEffectMovementSystem()
             .AddFadeEffectSystem()
             .AddHitFlashEffectSystem();
    }
}