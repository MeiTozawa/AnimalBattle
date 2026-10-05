using System;
using AnimalBattle.Ai.Ports;

namespace AnimalBattle.Ai.Runtime;

/// <summary>
///     System.Random に委譲する標準的な疑似乱数生成器の実装クラス。
/// </summary>
public sealed class SystemRandomSource : IRandomSource
{
    private readonly Random _random = new();

    /// <inheritdoc />
    public int NextInt(int exclusiveMax) => _random.Next(exclusiveMax);

    /// <inheritdoc />
    public float NextSingle() => _random.NextSingle();
}
