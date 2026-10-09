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
        world.System<SpriteJitter>("SpriteJitterSystem")
            .Kind(Ecs.PreUpdate)
            .Each((Iter it, int i, ref SpriteJitter jitter) =>
            {
                jitter.Timer += it.DeltaTime();
                jitter.CurrentOffsetX = MathF.Sin(jitter.Timer * jitter.Frequency * MathF.Tau) * jitter.AmplitudeX;
                jitter.CurrentOffsetY = MathF.Cos(jitter.Timer * jitter.Frequency * MathF.Tau) * jitter.AmplitudeY;
            });

        return world;
    }
}
