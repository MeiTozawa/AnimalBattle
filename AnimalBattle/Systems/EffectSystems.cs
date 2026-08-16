using System;
using Flecs.NET.Core;
using AnimalBattle.Animations;
using AnimalBattle.Components;
using AnimalBattle.Core;

namespace AnimalBattle.Systems;
/// <summary>
/// スラッシュ斬撃エフェクト、フェードアウト、ヒットフラッシュ等の視覚演出ライフサイクルを管理する Flecs.NET ECS システム群。
/// </summary>
public static class EffectSystems
{
    /// <summary>
    /// 攻撃演出エフェクトのアニメーションタイマーを進行させ、完了時に自動消滅させます。
    /// </summary>
    public static readonly Action<World> SlashEffectSystem = (world) =>
    {
        world.QueryBuilder<Animation, Sprite>()
             .With<EffectTag>()
             .Build()
             .Each((Entity e, ref Animation anim, ref Sprite sprite) =>
             {
                 anim.Timer += 0.016f;
                 AnimationConfig? effConfig = AnimationManager.GetEffectConfig(EffectType.SwordShieldEffectSprites);
                 if (effConfig != null)
                 {
                     sprite.SrcRect = AnimationManager.CalculateSrcRect(effConfig, anim.CurrentClip, anim.Timer, out bool isFinished);
                     if (isFinished)
                     {
                         e.Destruct();
                     }
                 }
             });
    };

    /// <summary>
    /// フェード演出コンポーネントの透明度アルファ値をイージング補間により更新します。
    /// </summary>
    public static readonly Action<World> FadeEffectSystem = (world) =>
    {
        world.QueryBuilder<FadeEffect>()
             .Build()
             .Each((ref FadeEffect fade) =>
             {
                 fade.ElapsedTimer += 0.016f;
                 float progress = Math.Clamp(fade.ElapsedTimer / Math.Max(0.001f, fade.Duration), 0.0f, 1.0f);
                 float factor = EasingMath.Evaluate(fade.Ease, progress);
                 fade.CurrentAlpha = fade.StartAlpha + (fade.EndAlpha - fade.StartAlpha) * factor;
             });
    };

    /// <summary>
    /// 被弾時のヒットフラッシュ演出タイマーを更新し、完了時にフラグを解除します。
    /// </summary>
    public static readonly Action<World> HitFlashEffectSystem = (world) =>
    {
        world.QueryBuilder<HitFlashEffect>()
             .Build()
             .Each((ref HitFlashEffect flash) =>
             {
                 if (flash.IsFlashing)
                 {
                     flash.ElapsedTimer += 0.016f;
                     if (flash.ElapsedTimer >= flash.Duration)
                     {
                         flash.IsFlashing = false;
                     }
                 }
             });
    };
}
