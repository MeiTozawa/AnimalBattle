using Flecs.NET.Core;
using AnimalBattle.Animations;
using AnimalBattle.Components;

namespace AnimalBattle.Systems;

/// <summary>
/// スラッシュ斬撃エフェクトのフレームアニメーション進行および再生完了時の自動破棄を制御する ECS System。
/// </summary>
public static class SlashEffectSystem
{
    /// <summary>
    /// スラッシュ演出システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    public static void Register(World world)
    {
        world.System<Animation, Sprite>("SlashEffectSystem")
             .With<EffectTag>()
             .Kind(Ecs.OnUpdate)
             .Each((Iter it, int i, ref Animation anim, ref Sprite sprite) =>
             {
                 anim.Timer += it.DeltaTime();
                 AnimationConfig? effConfig = AnimationManager.GetEffectConfig(EffectType.SwordShieldEffectSprites);
                 if (effConfig != null)
                 {
                     sprite.SrcRect = AnimationManager.CalculateSrcRect(effConfig, anim.CurrentClip, anim.Timer, out bool isFinished);
                     if (isFinished)
                     {
                         it.Entity(i).Destruct();
                     }
                 }
             });
    }
}
