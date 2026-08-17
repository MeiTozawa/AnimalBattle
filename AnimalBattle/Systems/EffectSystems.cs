using System;
using Flecs.NET.Core;
using AnimalBattle.Animations;
using AnimalBattle.Components;
using AnimalBattle.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// スラッシュ斬撃エフェクト、フェードアウト、ヒットフラッシュ等の視覚演出ライフサイクルを管理する Flecs.NET ECS システム群。
/// フレーム経過時間（delta_time）に基づいて滑らかに進行します。
/// </summary>
public static class EffectSystems
{
    /// <summary>
    /// エフェクト演出に関する全 ECS Routine を Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の ECS ワールド</param>
    public static void RegisterSystems(World world)
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

        world.System<FadeEffect>("FadeEffectSystem")
             .Kind(Ecs.OnUpdate)
             .Each((Iter it, int i, ref FadeEffect fade) =>
             {
                 fade.ElapsedTimer += it.DeltaTime();
                 float progress = Math.Clamp(fade.ElapsedTimer / Math.Max(0.001f, fade.Duration), 0.0f, 1.0f);
                 float factor = EasingMath.Evaluate(fade.Ease, progress);
                 fade.CurrentAlpha = fade.StartAlpha + (fade.EndAlpha - fade.StartAlpha) * factor;
             });

        world.System<HitFlashEffect>("HitFlashEffectSystem")
             .Kind(Ecs.OnUpdate)
             .Each((Iter it, int i, ref HitFlashEffect flash) =>
             {
                 if (flash.IsFlashing)
                 {
                     flash.ElapsedTimer += it.DeltaTime();
                     if (flash.ElapsedTimer >= flash.Duration)
                     {
                         flash.IsFlashing = false;
                     }
                 }
             });
    }
}
