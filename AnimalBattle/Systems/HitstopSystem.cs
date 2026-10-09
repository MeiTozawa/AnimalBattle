using System;
using AnimalBattle.Components;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     ヒットストップ（時間凍結）およびスプライト微小振動（Jitter）の減衰を制御する ECS System。
/// </summary>
public static class HitstopSystem
{
    /// <summary>
    ///     ヒットストップシステムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World" /></returns>
    public static World AddHitstopSystem(this World world)
    {
        world.System<HitstopState>("HitstopStateSystem")
            .Kind(Ecs.PreUpdate)
            .Each((it, i, ref hitstop) =>
            {
                hitstop.RemainingSeconds -= it.DeltaTime();
                if (hitstop.RemainingSeconds <= 0.0f)
                {
                    var entity = it.Entity(i);
                    entity.Remove<HitstopState>();
                    if (entity.Has<SpriteJitter>())
                    {
                        entity.Remove<SpriteJitter>();
                    }
                }
            });

        world.System<SpriteJitter>("SpriteJitterSystem")
            .Kind(Ecs.PreUpdate)
            .Each((it, i, ref jitter) =>
            {
                jitter.Timer += it.DeltaTime();
                jitter.CurrentOffsetX = MathF.Sin(jitter.Timer * jitter.Frequency * MathF.Tau) * jitter.AmplitudeX;
                jitter.CurrentOffsetY = MathF.Cos(jitter.Timer * jitter.Frequency * MathF.Tau) * jitter.AmplitudeY;
            });

        return world;
    }
}
