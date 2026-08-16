using System;
using System.Runtime.CompilerServices;

namespace AnimalBattle.Core;
/// <summary>
/// アロケーションフリーでイージング計算を行う静的数学クラス。
/// </summary>
public static class EasingMath
{
    private const float _pi = MathF.PI;

    /// <summary>
    /// 指定されたイージング型および進捗割合 <paramref name="progress"/> (0.0f～1.0f) から補間数値を評価・計算します。
    /// </summary>
    /// <param name="ease">イージング型</param>
    /// <param name="progress">進捗率（0.0f～1.0f）</param>
    /// <returns>イージングが適用された補間値（通常 0.0f～1.0f）</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Evaluate(EaseType ease, float progress)
    {
        progress = Math.Clamp(progress, 0.0f, 1.0f);

        return ease switch
        {
            EaseType.Linear => progress,
            EaseType.InSine => 1.0f - MathF.Cos((progress * _pi) * 0.5f),
            EaseType.OutSine => MathF.Sin((progress * _pi) * 0.5f),
            EaseType.InOutSine => -(MathF.Cos(_pi * progress) - 1.0f) * 0.5f,
            EaseType.InQuad => progress * progress,
            EaseType.OutQuad => 1.0f - (1.0f - progress) * (1.0f - progress),
            EaseType.InOutQuad => progress < 0.5f ? 2.0f * progress * progress : 1.0f - MathF.Pow(-2.0f * progress + 2.0f, 2.0f) * 0.5f,
            EaseType.OutCubic => 1.0f - MathF.Pow(1.0f - progress, 3.0f),
            EaseType.OutElastic => EvaluateOutElastic(progress),
            EaseType.OutBounce => EvaluateOutBounce(progress),
            _ => progress
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float EvaluateOutElastic(float t)
    {
        if (t <= 0.0f)
        {
            return 0.0f;
        }
        if (t >= 1.0f)
        {
            return 1.0f;
        }

        const float c4 = (2.0f * _pi) / 3.0f;
        return MathF.Pow(2.0f, -10.0f * t) * MathF.Sin((t * 10.0f - 0.75f) * c4) + 1.0f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float EvaluateOutBounce(float x)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;

        if (x < 1.0f / d1)
        {
            return n1 * x * x;
        }
        if (x < 2.0f / d1)
        {
            x -= 1.5f / d1;
            return n1 * x * x + 0.75f;
        }
        if (x < 2.5f / d1)
        {
            x -= 2.25f / d1;
            return n1 * x * x + 0.9375f;
        }

        x -= 2.625f / d1;
        return n1 * x * x + 0.984375f;
    }
}
