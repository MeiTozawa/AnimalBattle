using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Boss2;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Dsl.Modules;

namespace AnimalBattle.Ai.Runtime;

/// <summary>
/// 固定 BossState と具体的な State_Module ファイルの対応を表します。
/// </summary>
public readonly record struct FixedBossStateModule(
    BossState State,
    BossType BossType,
    string ModuleFileName,
    Type ModuleType)
{
    /// <summary>固定状態と module ファイルの記述が有効かどうかを取得します。</summary>
    public bool IsValid => State.IsValid()
        && BossType != BossType.Unknown
        && !string.IsNullOrWhiteSpace(ModuleFileName)
        && ModuleType is not null;
}

/// <summary>
/// 初期化時にだけ参照する固定 BossState/module 集合です。
/// </summary>
public static class FixedBossStateModuleCatalog
{
    private static readonly IReadOnlyList<FixedBossStateModule> _entries = Array.AsReadOnly(
    [
        Entry(BossState.Boss1_Hover, BossType.Boss1, nameof(HoverMovementModule)),
        Entry(BossState.Boss1_AttackSelection, BossType.Boss1, nameof(Boss1AttackSelectionModule)),
        Entry(BossState.Boss1_Telegraph, BossType.Boss1, nameof(DslBoss1TelegraphModule)),
        Entry(BossState.Boss1_Execute, BossType.Boss1, nameof(DslBoss1ExecuteModule)),
        Entry(BossState.Boss1_Recovery, BossType.Boss1, nameof(DslBoss1RecoveryModule)),
        Entry(BossState.Boss1_PhaseTransition, BossType.Boss1, nameof(Boss1PhaseTransitionModule)),
        Entry(BossState.Boss1_Dead, BossType.Boss1, nameof(Boss1DeadModule)),

        Entry(BossState.Boss2_Idle, BossType.Boss2, nameof(Boss2IdleModule)),
        Entry(BossState.Boss2_PatrolChase, BossType.Boss2, nameof(Boss2PatrolChaseModule)),
        Entry(BossState.Boss2_MoveToTakeoff, BossType.Boss2, nameof(Boss2MoveToTakeoffModule)),
        Entry(BossState.Boss2_PlatformTraverse, BossType.Boss2, nameof(DslPlatformTraverseModule)),
        Entry(BossState.Boss2_Telegraph, BossType.Boss2, nameof(DslTelegraphModule)),
        Entry(BossState.Boss2_Execute, BossType.Boss2, nameof(DslExecuteModule)),
        Entry(BossState.Boss2_WallCrashStun, BossType.Boss2, nameof(Boss2WallCrashStunModule)),
        Entry(BossState.Boss2_Recovery, BossType.Boss2, nameof(Boss2RecoveryModule)),
        Entry(BossState.Boss2_PhaseTransition, BossType.Boss2, nameof(Boss2PhaseTransitionModule)),
        Entry(BossState.Boss2_Dead, BossType.Boss2, nameof(Boss2DeadModule))
    ]);

    /// <summary>Unknown を除く固定状態の全対応を取得します。</summary>
    public static IReadOnlyList<FixedBossStateModule> Entries => _entries;

    /// <summary>固定状態に対応する module 記述を取得します。</summary>
    public static bool Get(BossState state, out FixedBossStateModule entry)
    {
        for (int index = 0; index < _entries.Count; index++)
        {
            if (_entries[index].State == state)
            {
                entry = _entries[index];
                return true;
            }
        }

        entry = default;
        return false;
    }

    /// <summary>固定集合の完全性と一対一対応を検査します。</summary>
    public static bool IsComplete(out string diagnostic)
    {
        if (_entries.Count != Enum.GetValues<BossState>().Length - 1)
        {
            diagnostic = "固定 BossState の対応数が一致しません。";
            return false;
        }

        for (int index = 0; index < _entries.Count; index++)
        {
            FixedBossStateModule entry = _entries[index];
            if (!entry.IsValid)
            {
                diagnostic = $"固定 BossState の module 記述が不正です: {entry.State}";
                return false;
            }

            for (int other = index + 1; other < _entries.Count; other++)
            {
                if (_entries[other].State == entry.State)
                {
                    diagnostic = $"固定 BossState が重複しています: {entry.State}";
                    return false;
                }
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
        return state switch
        {
            BossState.Boss1_Hover => typeof(HoverMovementModule),
            BossState.Boss1_AttackSelection => typeof(Boss1AttackSelectionModule),
            BossState.Boss1_Telegraph => typeof(DslBoss1TelegraphModule),
            BossState.Boss1_Execute => typeof(DslBoss1ExecuteModule),
            BossState.Boss1_Recovery => typeof(DslBoss1RecoveryModule),
            BossState.Boss1_PhaseTransition => typeof(Boss1PhaseTransitionModule),
            BossState.Boss1_Dead => typeof(Boss1DeadModule),

            BossState.Boss2_Idle => typeof(Boss2IdleModule),
            BossState.Boss2_PatrolChase => typeof(Boss2PatrolChaseModule),
            BossState.Boss2_MoveToTakeoff => typeof(Boss2MoveToTakeoffModule),
            BossState.Boss2_PlatformTraverse => typeof(DslPlatformTraverseModule),
            BossState.Boss2_Telegraph => typeof(DslTelegraphModule),
            BossState.Boss2_Execute => typeof(DslExecuteModule),
            BossState.Boss2_WallCrashStun => typeof(Boss2WallCrashStunModule),
            BossState.Boss2_Recovery => typeof(Boss2RecoveryModule),
            BossState.Boss2_PhaseTransition => typeof(Boss2PhaseTransitionModule),
            BossState.Boss2_Dead => typeof(Boss2DeadModule),
            _ => typeof(object)
        };
    }
}
