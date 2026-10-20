using System;
using AnimalBattle.Components;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Presentation.Vfx;

/// <summary>
///     受撃局所振動（<see cref="SpriteJitter" />）の変位オフセット計算を制御する ECS システム。
/// </summary>
public static class SpriteJitterSystem
{
    /// <summary>
    ///     スプライト局所振動システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddSpriteJitterSystem(this World world)
    {
        world.System<SpriteJitter, Sprite>("SpriteJitterSystem")
            .Kind(Ecs.PreUpdate)
            .Iter(ExecuteSpriteJitter);

        return world;
    }

    /// <summary>
    ///     スプライト局所振動オフセット計算パイプライン（名前付き静的メソッド）。
    /// </summary>
    public static void ExecuteSpriteJitter(Iter it, Field<SpriteJitter> jitters, Field<Sprite> sprites)
    {
        var dt = it.DeltaTime();
        var count = it.Count();
        for (var i = 0; i < count; i++)
        {
            ref var jitter = ref jitters[i];
            jitter.Timer += dt;
            var angle = jitter.Timer * jitter.Frequency * MathF.Tau;
            jitter.CurrentOffset = new Vec2f(
                MathF.Sin(angle) * jitter.Amplitude.X,
                MathF.Cos(angle) * jitter.Amplitude.Y);

            ref var sprite = ref sprites[i];
            sprite.JitterOffset = jitter.CurrentOffset;
        }
    }
}
