using AnimalBattle.Components;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     二階減衰調和振動スプリング（解析解 / 閉形式解）に基づくスケール変形更新システム。
///     明示的オイラー法の数値発散を完全に排除し、任意のフレームレート・デルタタイムで無条件に安定して滑らかに目標値へ収束します。
///     ヒットストップ（<see cref="HitstopState" />）中のエンティティは更新をスキップします。
/// </summary>
public static class SpringScaleSystem
{
    /// <summary>
    ///     ECS ワールドにスプリングスケール更新システムを登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddSpringScaleSystem(this World world)
    {
        world.System<SpringScale, Sprite, Transform>("SpringScaleSystem")
            .Without<HitstopState>()
            .Iter((Iter it, Field<SpringScale> springs, Field<Sprite> sprites, Field<Transform> transforms) =>
            {
                var dt = it.DeltaTime();
                if (dt <= 0.0f) return;

                var safeDt = MathF.Min(dt, 0.1f);
                var count = it.Count();

                for (var i = 0; i < count; i++)
                {
                    ref var spring = ref springs[i];

                    UpdateDampedSpring(
                        ref spring.CurrentX,
                        ref spring.VelX,
                        spring.TargetX,
                        spring.Frequency,
                        spring.DampingRatio,
                        safeDt);

                    UpdateDampedSpring(
                        ref spring.CurrentY,
                        ref spring.VelY,
                        spring.TargetY,
                        spring.Frequency,
                        spring.DampingRatio,
                        safeDt);

                    ref var sprite = ref sprites[i];
                    ref var trans = ref transforms[i];

                    sprite.ScaleX = spring.CurrentX;
                    sprite.ScaleY = spring.CurrentY;

                    if (sprite.SrcRect.Height > 0)
                    {
                        var halfH = sprite.SrcRect.Height * trans.ScaleY * 0.5f;
                        sprite.SpringOffsetY = halfH * (1.0f - spring.CurrentY);
                    }
                }
            });

        return world;
    }

    /// <summary>
    ///     二階減衰調和振動子の厳密解析解（Ryan Juckett 方式）を用いて位置と速度を無条件安定更新します。
    /// </summary>
    /// <param name="current">現在の変位値</param>
    /// <param name="velocity">現在の速度</param>
    /// <param name="target">目標静止値</param>
    /// <param name="frequency">固有振動数 (Hz)</param>
    /// <param name="dampingRatio">減衰比</param>
    /// <param name="dt">経過時間 (秒)</param>
    private static void UpdateDampedSpring(
        ref float current,
        ref float velocity,
        float target,
        float frequency,
        float dampingRatio,
        float dt)
    {
        if (frequency <= 0.0f)
        {
            current = target;
            velocity = 0.0f;
            return;
        }

        var omega = MathF.Tau * frequency;
        var p = current - target;

        if (dampingRatio < 1.0f)
        {
            // 減衰不足（Underdamped: 振動しながら目標値へ収束）
            var omegaD = omega * MathF.Sqrt(1.0f - dampingRatio * dampingRatio);
            var gamma = dampingRatio * omega;
            var exp = MathF.Exp(-gamma * dt);
            var cos = MathF.Cos(omegaD * dt);
            var sin = MathF.Sin(omegaD * dt);

            var newP = exp * (p * cos + (velocity + gamma * p) / omegaD * sin);
            var newV = exp * (velocity * cos - (gamma * velocity + omega * omega * p) / omegaD * sin);

            current = target + newP;
            velocity = newV;
        }
        else if (MathF.Abs(dampingRatio - 1.0f) < 0.0001f)
        {
            // 臨界減衰（Critically Damped: 振動せず最短時間で目標値へ収束）
            var exp = MathF.Exp(-omega * dt);
            var newP = exp * (p + (velocity + omega * p) * dt);
            var newV = exp * (velocity - omega * (velocity + omega * p) * dt);

            current = target + newP;
            velocity = newV;
        }
        else
        {
            // 過減衰（Overdamped: 抵抗が強く緩やかに目標値へ収束）
            var omegaD = omega * MathF.Sqrt(dampingRatio * dampingRatio - 1.0f);
            var gamma1 = -dampingRatio * omega + omegaD;
            var gamma2 = -dampingRatio * omega - omegaD;
            var exp1 = MathF.Exp(gamma1 * dt);
            var exp2 = MathF.Exp(gamma2 * dt);

            var c2 = (velocity - gamma1 * p) / (gamma2 - gamma1);
            var c1 = p - c2;

            current = target + (c1 * exp1 + c2 * exp2);
            velocity = c1 * gamma1 * exp1 + c2 * gamma2 * exp2;
        }

        // 描画異常やオーバーフローを防ぐ安全防護クランプ
        current = Math.Clamp(current, 0.1f, 3.0f);
    }
}
