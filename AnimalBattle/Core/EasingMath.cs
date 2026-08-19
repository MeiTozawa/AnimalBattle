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
    /// <exception cref="ArgumentOutOfRangeException">未定義の <see cref="EaseType"/> が指定された場合にスローされます。</exception>
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
            EaseType.InCubic => progress * progress * progress,
            EaseType.OutCubic => 1.0f - MathF.Pow(1.0f - progress, 3.0f),
            EaseType.InOutCubic => progress < 0.5f ? 4.0f * progress * progress * progress : 1.0f - MathF.Pow(-2.0f * progress + 2.0f, 3.0f) * 0.5f,
            EaseType.InExpo => progress <= 0.0f ? 0.0f : MathF.Pow(2.0f, 10.0f * progress - 10.0f),
            EaseType.OutExpo => progress >= 1.0f ? 1.0f : 1.0f - MathF.Pow(2.0f, -10.0f * progress),
            EaseType.InOutExpo => EvaluateInOutExpo(progress),
            EaseType.InElastic => EvaluateInElastic(progress),
            EaseType.OutElastic => EvaluateOutElastic(progress),
            EaseType.InOutElastic => EvaluateInOutElastic(progress),
            EaseType.InBounce => 1.0f - EvaluateOutBounce(1.0f - progress),
            EaseType.OutBounce => EvaluateOutBounce(progress),
            EaseType.InOutBounce => progress < 0.5f
                ? (1.0f - EvaluateOutBounce(1.0f - 2.0f * progress)) * 0.5f
                : (1.0f + EvaluateOutBounce(2.0f * progress - 1.0f)) * 0.5f,
            _ => throw new ArgumentOutOfRangeException(nameof(ease), ease, $"未定義の EaseType です: {ease}")
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float EvaluateInOutExpo(float t)
    {
        if (t <= 0.0f)
        {
            return 0.0f;
        }
        if (t >= 1.0f)
        {
            return 1.0f;
        }

        return t < 0.5f
            ? MathF.Pow(2.0f, 20.0f * t - 10.0f) * 0.5f
            : (2.0f - MathF.Pow(2.0f, -20.0f * t + 10.0f)) * 0.5f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float EvaluateInElastic(float t)
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
        return -MathF.Pow(2.0f, 10.0f * t - 10.0f) * MathF.Sin((t * 10.0f - 10.75f) * c4);
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
    private static float EvaluateInOutElastic(float t)
    {
        if (t <= 0.0f)
        {
            return 0.0f;
        }
        if (t >= 1.0f)
        {
            return 1.0f;
        }

        const float c5 = (2.0f * _pi) / 4.5f;
        return t < 0.5f
            ? -(MathF.Pow(2.0f, 20.0f * t - 10.0f) * MathF.Sin((20.0f * t - 11.125f) * c5)) * 0.5f
            : (MathF.Pow(2.0f, -20.0f * t + 10.0f) * MathF.Sin((20.0f * t - 11.125f) * c5)) * 0.5f + 1.0f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float EvaluateOutBounce(float x)
    {
        // Robert Penner バウンスアルゴリズムの定数:
        // d1: 全 4 段階のバウンス時間比率の合計 (1.0 + 1.0 + 0.5 + 0.25 = 2.75)
        // n1: 第 1 段階終了時 (x = 1/d1) で到達位置 y = 1.0 となる放物線加速度係数 (d1^2 = 2.75^2 = 7.5625)
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
