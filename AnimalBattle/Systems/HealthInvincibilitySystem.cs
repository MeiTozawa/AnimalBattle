using System;
using AnimalBattle.Components;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     無敵タイマー（<see cref="Health.InvincibleTimer" />）の自然減衰を制御する単一責任 ECS システム。
/// </summary>
public static class HealthInvincibilitySystem
{
    /// <summary>
    ///     無敵タイマー自然減衰システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World" /></returns>
    public static World AddHealthInvincibilitySystem(this World world)
    {
        world.System<Health>("HealthInvincibilitySystem")
            .Kind(Ecs.PreUpdate)
            .Iter((Iter it, Field<Health> hps) =>
            {
                var dt = it.DeltaTime();
                var count = it.Count();
                for (var i = 0; i < count; i++)
                {
                    ref var hp = ref hps[i];
                    if (hp.InvincibleTimer > 0.0f)
                    {
                        hp.InvincibleTimer = MathF.Max(0.0f, hp.InvincibleTimer - dt);
                    }
                }
            });

        return world;
    }
}
