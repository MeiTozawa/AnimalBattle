using System;
using AnimalBattle.Components;
using Flecs.NET.Core;

namespace AnimalBattle.Presentation.Vfx;

/// <summary>
///     イージング関数に基づく透明度フェードイン・フェードアウトの進行を制御し、持続時間満了時にエンティティを自動破棄：または無効化する ECS System。
///     局所的かつ軽量な演出更新のため、余分なバッファ割当や P/Invoke を行わず単一パスで直接処理します。
/// </summary>
public static class FadeEffectSystem
{
    /// <summary>
    ///     フェード演出および自動寿命破棄システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド。</param>
    /// <returns>メソッドチェーン用の <see cref="World" />。</returns>
    public static World AddFadeEffectSystem(this World world)
    {
        world.System<FadeEffect>("FadeEffectSystem")
            .Kind(Ecs.OnUpdate)
            .Iter(ExecuteFadeEffect);

        return world;
    }

    /// <summary>
    ///     フェード演出更新パイプライン：名前付き静的メソッド。
    /// </summary>
    public static void ExecuteFadeEffect(Iter it, Field<FadeEffect> fade)
    {
        var count = it.Count();
        if (count <= 0) return;

        var dt = it.DeltaTime();
        for (var i = 0; i < count; ++i)
        {
            ref var f = ref fade[i];
            f.ElapsedTimer += dt;
            if (f.ElapsedTimer >= f.Duration)
            {
                f.CurrentAlpha = f.EndAlpha;
                it.Entity(i).Add<RecycleTag>();
                continue;
            }

            var progress = Math.Clamp(f.ElapsedTimer / Math.Max(0.001f, f.Duration), 0.0f, 1.0f);
            var factor = EasingMath.Evaluate(f.Ease, progress);
            f.CurrentAlpha = f.StartAlpha + (f.EndAlpha - f.StartAlpha) * factor;
        }
    }
}