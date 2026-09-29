using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Boss2;
using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Runtime;

/// <summary>
///     固定 BossState と具体的な State_Module ファイルの対応を表します。
/// </summary>
public readonly record struct FixedBossStateModule(
    BossState State,
    BossType BossType,
    string ModuleFileName,
    Type ModuleType) {
    /// <summary>固定状態と module ファイルの記述が有効かどうかを取得します。</summary>
    public bool IsValid => State.IsValid()
     && BossType != BossType.Unknown
     && !string.IsNullOrWhiteSpace(ModuleFileName)
     && ModuleType is not null;
}

/// <summary>
///     初期化時にだけ参照する固定 BossState/module 集合です。
/// </summary>
public static class FixedBossStateModuleCatalog {
    /// <summary>Unknown を除く固定状態の全対応を取得します。</summary>
    public static IReadOnlyList<FixedBossStateModule> Entries { get; } = Array.AsReadOnly(
    [
        Entry(BossState.Boss1_Hover, BossType.Boss1, nameof(HoverMovementModule)),
        Entry(BossState.Boss1_AttackSelection, BossType.Boss1, nameof(Boss1AttackSelectionModule)),
        Entry(BossState.Boss1_Telegraph, BossType.Boss1, nameof(Boss1TelegraphModule)),
        Entry(BossState.Boss1_Execute, BossType.Boss1, nameof(Boss1ExecuteModule)),
        Entry(BossState.Boss1_Recovery, BossType.Boss1, nameof(Boss1RecoveryModule)),
        Entry(BossState.Boss1_PhaseTransition, BossType.Boss1, nameof(Boss1PhaseTransitionModule)),
        Entry(BossState.Boss1_Dead, BossType.Boss1, nameof(Boss1DeadModule)),

        Entry(BossState.Boss2_Idle, BossType.Boss2, nameof(Boss2IdleModule)),
        Entry(BossState.Boss2_PatrolChase, BossType.Boss2, nameof(Boss2PatrolChaseModule)),
        Entry(BossState.Boss2_MoveToTakeoff, BossType.Boss2, nameof(Boss2MoveToTakeoffModule)),
        Entry(BossState.Boss2_PlatformTraverse, BossType.Boss2, nameof(Boss2PlatformTraverseModule)),
        Entry(BossState.Boss2_Telegraph, BossType.Boss2, nameof(Boss2TelegraphModule)),
        Entry(BossState.Boss2_Execute, BossType.Boss2, nameof(Boss2ExecuteModule)),
        Entry(BossState.Boss2_WallCrashStun, BossType.Boss2, nameof(Boss2WallCrashStunModule)),
        Entry(BossState.Boss2_Recovery, BossType.Boss2, nameof(Boss2RecoveryModule)),
        Entry(BossState.Boss2_PhaseTransition, BossType.Boss2, nameof(Boss2PhaseTransitionModule)),
        Entry(BossState.Boss2_Dead, BossType.Boss2, nameof(Boss2DeadModule))
    ]);

    /// <summary>指定された BossType に属する固定状態の対応を取得します。</summary>
    public static IEnumerable<FixedBossStateModule> GetEntriesFor(BossType bossType)
    {
        for (var index = 0; index < Entries.Count; index++)
            if (Entries[index].BossType == bossType)
                yield return Entries[index];
    }

    /// <summary>固定状態に対応する module 記述を取得します。</summary>
    public static bool Get(BossState state, out FixedBossStateModule entry)
    {
        for (var index = 0; index < Entries.Count; index++)
            if (Entries[index].State == state)
            {
                entry = Entries[index];
                return true;
            }

        entry = default;
        return false;
    }

    /// <summary>固定集合の完全性と一対一対応を検査します。</summary>
    public static bool IsComplete(out string diagnostic)
    {
        var expectedCount = Enum.GetValues<Boss1State>().Length - 1 + (Enum.GetValues<Boss2State>().Length - 1);
        if (Entries.Count != expectedCount)
        {
            diagnostic = "固定 BossState の対応数が一致しません。";
            return false;
        }

        for (var index = 0; index < Entries.Count; index++)
        {
            var entry = Entries[index];
            if (!entry.IsValid)
            {
                diagnostic = $"固定 BossState の module 記述が不正です: {entry.State}";
                return false;
            }

            for (var other = index + 1; other < Entries.Count; other++)
                if (Entries[other].State == entry.State)
                {
                    diagnostic = $"固定 BossState が重複しています: {entry.State}";
                    return false;
                }
        }

        diagnostic = "固定 BossState と State_Module の対応は完全です。";
        return true;
    }

    private static FixedBossStateModule Entry(BossState state, BossType bossType, string moduleName)
    {
        return new FixedBossStateModule(state, bossType, $"{moduleName}.cs", ResolveType(state));
    }

    private static Type ResolveType(BossState state)
    {
        return state.BossType switch {
            BossType.Boss1 => state.AsBoss1() switch {
                Boss1State.Hover => typeof(HoverMovementModule),
                Boss1State.AttackSelection => typeof(Boss1AttackSelectionModule),
                Boss1State.Telegraph => typeof(Boss1TelegraphModule),
                Boss1State.Execute => typeof(Boss1ExecuteModule),
                Boss1State.Recovery => typeof(Boss1RecoveryModule),
                Boss1State.PhaseTransition => typeof(Boss1PhaseTransitionModule),
                Boss1State.Dead => typeof(Boss1DeadModule),
                _ => typeof(object)
            },
            BossType.Boss2 => state.AsBoss2() switch {
                Boss2State.Idle => typeof(Boss2IdleModule),
                Boss2State.PatrolChase => typeof(Boss2PatrolChaseModule),
                Boss2State.MoveToTakeoff => typeof(Boss2MoveToTakeoffModule),
                Boss2State.PlatformTraverse => typeof(Boss2PlatformTraverseModule),
                Boss2State.Telegraph => typeof(Boss2TelegraphModule),
                Boss2State.Execute => typeof(Boss2ExecuteModule),
                Boss2State.WallCrashStun => typeof(Boss2WallCrashStunModule),
                Boss2State.Recovery => typeof(Boss2RecoveryModule),
                Boss2State.PhaseTransition => typeof(Boss2PhaseTransitionModule),
                Boss2State.Dead => typeof(Boss2DeadModule),
                _ => typeof(object)
            },
            _ => typeof(object)
        };
    }
}