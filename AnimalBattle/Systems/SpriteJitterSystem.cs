using System;
using AnimalBattle.Components;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

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
            .Iter((Iter it, Field<SpriteJitter> jitters, Field<Sprite> sprites) =>
            {
                var dt = it.DeltaTime();
                var count = it.Count();
                for (var i = 0; i < count; i++)
                {
                    ref var jitter = ref jitters[i];
                    jitter.Timer += dt;
                    jitter.CurrentOffsetX = MathF.Sin(jitter.Timer * jitter.Frequency * MathF.Tau) * jitter.AmplitudeX;
                    jitter.CurrentOffsetY = MathF.Cos(jitter.Timer * jitter.Frequency * MathF.Tau) * jitter.AmplitudeY;

                    ref var sprite = ref sprites[i];
                    sprite.JitterOffsetX = jitter.CurrentOffsetX;
                    sprite.JitterOffsetY = jitter.CurrentOffsetY;
                }
            });

        return world;
    }
}
