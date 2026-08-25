using AnimalBattle.Ai.Contracts;
using AnimalBattle.Components;
using Flecs.NET.Core;

namespace AnimalBattle.Ai.Ecs;

/// <summary>Boss AI の必須 ECS data を Query 内だけで読み書きする callback です。</summary>
public delegate void BossAiEcsUpdateCallback(
    Entity entity,
    ref BossAiData ai,
    ref BossMovementData movement,
    ref BossAttackData attack,
    ref BossPhaseTransitionData phaseTransition,
    ref BossTerminalData terminal);

/// <summary>ECS 更新の gate 状態です。</summary>
public readonly record struct BossEcsUpdateGate(bool InitializationComplete, bool RuntimeReady)
{
    /// <summary>両方の更新 gate が成立しているかどうかです。</summary>
    public bool IsOpen => InitializationComplete && RuntimeReady;
}

/// <summary>Flecs.NET Query 境界の実行結果です。</summary>
public readonly record struct BossEcsBoundaryResult(
    bool IsAccepted,
    int UpdatedEntityCount,
    ErrorCode ErrorCode,
    long FrameUpdateNumber)
{
    /// <summary>更新が成功したかどうかです。</summary>
    public bool IsSuccess => IsAccepted;
}

/// <summary>Boss AI data component の Query と gate を所有する ECS 境界です。</summary>
public sealed class BossAiEcsBoundary
{
    private readonly Query<BossAiData, BossMovementData, BossAttackData, BossPhaseTransitionData, BossTerminalData> _query;

    /// <summary>指定 World に必須 Boss AI Query を構築します。</summary>
    public BossAiEcsBoundary(World world)
    {
        _query = world.QueryBuilder<BossAiData, BossMovementData, BossAttackData, BossPhaseTransitionData, BossTerminalData>().Build();
    }

    /// <summary>gate 後にだけ Query の読み書きを行います。</summary>
    public BossEcsBoundaryResult TryUpdate(in BossEcsUpdateGate gate, long frameUpdateNumber, BossAiEcsUpdateCallback callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        if (!gate.IsOpen || frameUpdateNumber < 0)
        {
            return new BossEcsBoundaryResult(false, 0, ErrorCode.MissingDependency, frameUpdateNumber);
        }

        int updatedCount = 0;
        try
        {
            _query.Each((Entity entity, ref BossAiData ai, ref BossMovementData movement, ref BossAttackData attack, ref BossPhaseTransitionData phaseTransition, ref BossTerminalData terminal) =>
            {
                callback(entity, ref ai, ref movement, ref attack, ref phaseTransition, ref terminal);
                updatedCount++;
            });
            return new BossEcsBoundaryResult(true, updatedCount, ErrorCode.None, frameUpdateNumber);
        }
        catch
        {
            return new BossEcsBoundaryResult(false, updatedCount, ErrorCode.ExecutionFailed, frameUpdateNumber);
        }
    }
}