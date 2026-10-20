using AnimalBattle.Systems;
using Flecs.NET.Core;

namespace AnimalBattle.Presentation.Animation;

/// <summary>
///     スプライトアニメーション、自機状態機アニメーション遷移、およびスプリング拡縮変形システムを一括管理するアニメーションモジュール。
/// </summary>
public readonly struct AnimationModule : IFlecsModule
{
    /// <summary>
    ///     アニメーション関連の ECS システム群をワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    public void InitModule(World world)
    {
        world.Module<AnimationModule>();

        world.AddSpringScaleSystem()
             .AddSpriteAnimationSystem()
             .AddPlayerAnimationStateSystem();
    }
}
