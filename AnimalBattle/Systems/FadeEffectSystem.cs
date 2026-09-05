using System;
using AnimalBattle.Components;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// イージング関数に基づく透明度フェードイン・フェードアウトの進行を制御し、持続時間満了時にエンティティを自動破棄する ECS System。
/// 局所的かつ軽量な演出更新のため、余分なバッファ割当や P/Invoke を行わず単一パスで直接処理します。
/// </summary>
public static class FadeEffectSystem
{
    /// <summary>
    /// フェード演出および自動寿命破棄システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド。</param>
    /// <returns>メソッドチェーン用の <see cref="World"/>。</returns>
    public static World AddFadeEffectSystem(this World world)
    {
        world.System<FadeEffect>("FadeEffectSystem")
             .Kind(Ecs.OnUpdate)
             .Iter((Iter it, Field<FadeEffect> fade) =>
             {
                 int count = it.Count();
                 if (count <= 0)
                 {
                     return;
                 }

                 float dt = it.DeltaTime();
                 for (int i = 0; i < count; ++i)
                 {
                     fade[i].ElapsedTimer += dt;
                     if (fade[i].ElapsedTimer >= fade[i].Duration)
                     {
                         it.Entity(i).Destruct();
                         continue;
                     }

                     float progress = Math.Clamp(fade[i].ElapsedTimer / Math.Max(0.001f, fade[i].Duration), 0.0f, 1.0f);
                     float factor = EasingMath.Evaluate(fade[i].Ease, progress);
                     fade[i].CurrentAlpha = fade[i].StartAlpha + (fade[i].EndAlpha - fade[i].StartAlpha) * factor;
                 }
             });

        return world;
    }
}
