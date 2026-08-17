using System;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// イージング関数に基づく透明度フェードイン・フェードアウトの進行を制御する ECS System。
/// </summary>
public static class FadeEffectSystem
{
    /// <summary>
    /// フェード演出システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    public static void Register(World world)
    {
        world.System<FadeEffect>("FadeEffectSystem")
             .Kind(Ecs.OnUpdate)
             .Each((Iter it, int i, ref FadeEffect fade) =>
             {
                 fade.ElapsedTimer += it.DeltaTime();
                 float progress = Math.Clamp(fade.ElapsedTimer / Math.Max(0.001f, fade.Duration), 0.0f, 1.0f);
                 float factor = EasingMath.Evaluate(fade.Ease, progress);
                 fade.CurrentAlpha = fade.StartAlpha + (fade.EndAlpha - fade.StartAlpha) * factor;
             });
    }
}
