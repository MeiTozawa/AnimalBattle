using System;
using System.Runtime.CompilerServices;
using AnimalBattle.Bindings;
using AnimalBattle.Compute;

namespace AnimalBattle.Core;

/// <summary>
/// C++ 原生拡張 (AnimalBattleExtension.dll) へ計算を委譲し、
/// アロケーションフリーかつ SIMD 高スループットでイージング計算を行う静的数学クラス。
/// </summary>
public static class EasingMath
{
    /// <summary>
    /// 指定されたイージング型および進捗割合 <paramref name="progress"/> (0.0f～1.0f) から補間数値を評価・計算します。
    /// </summary>
    /// <param name="ease">イージング型。</param>
    /// <param name="progress">進捗率（0.0f～1.0f）。</param>
    /// <returns>イージングが適用された補間値（通常 0.0f～1.0f）。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Evaluate(EaseType ease, float progress)
    {
        return NativeExtensionBindings.Extension_EvaluateEase(ease, progress);
    }

    /// <summary>
    /// 複数の進捗値配列に対して、指定イージング関数による一括バッチ計算を C++ 原生拡張で並列実行します。
    /// </summary>
    /// <param name="ease">イージング型。</param>
    /// <param name="inProgress">進捗割合配列 (0.0f～1.0f)。</param>
    /// <param name="outValues">計算結果の出力先 Span。</param>
    public static void EvaluateBatch(EaseType ease, ReadOnlySpan<float> inProgress, Span<float> outValues)
    {
        int count = Math.Min(inProgress.Length, outValues.Length);
        if (count <= 0)
        {
            return;
        }

        NativeComputeQueue queue = NativeComputeQueue.Shared;
        queue.Clear();
        for (int i = 0; i < count; ++i)
        {
            queue.EnqueueEase(ease, inProgress[i]);
        }

        queue.Dispatch();

        for (int i = 0; i < count; ++i)
        {
            outValues[i] = queue.GetEaseResult(i);
        }
        queue.Clear();
    }
}
