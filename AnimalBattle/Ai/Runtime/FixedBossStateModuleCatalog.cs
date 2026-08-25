using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Boss2;
using AnimalBattle.Ai.Contracts;

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
    private static readonly FixedBossStateModule[] _entries =
    [
        Entry(BossState.Boss1_Hover, BossType.Boss1, nameof(HoverMovementModule)),
        Entry(BossState.Boss1_AttackSelection, BossType.Boss1, nameof(Boss1AttackSelectionModule)),
        Entry(BossState.Boss1_Telegraph, BossType.Boss1, nameof(Boss1TelegraphModule)),
        Entry(BossState.Boss1_Aimed3ShotExecute, BossType.Boss1, nameof(Aimed3ShotExecuteModule)),
        Entry(BossState.Boss1_RingBurstExecute, BossType.Boss1, nameof(RingBurstExecuteModule)),
        Entry(BossState.Boss1_DoubleSpiralExecute, BossType.Boss1, nameof(DoubleSpiralExecuteModule)),
        Entry(BossState.Boss1_RainDropExecute, BossType.Boss1, nameof(RainDropExecuteModule)),
        Entry(BossState.Boss1_CrossChaosExecute, BossType.Boss1, nameof(CrossChaosExecuteModule)),
        Entry(BossState.Boss1_FastSniperExecute, BossType.Boss1, nameof(FastSniperExecuteModule)),
        Entry(BossState.Boss1_DiveRamExecute, BossType.Boss1, nameof(DiveRamExecuteModule)),
        Entry(BossState.Boss1_Recovery, BossType.Boss1, nameof(Boss1RecoveryModule)),
        Entry(BossState.Boss1_PhaseTransition, BossType.Boss1, nameof(Boss1PhaseTransitionModule)),
        Entry(BossState.Boss1_Dead, BossType.Boss1, nameof(Boss1DeadModule)),

        Entry(BossState.Boss2_Idle, BossType.Boss2, nameof(Boss2IdleModule)),
        Entry(BossState.Boss2_PatrolChase, BossType.Boss2, nameof(Boss2PatrolChaseModule)),
        Entry(BossState.Boss2_MoveToTakeoff, BossType.Boss2, nameof(Boss2MoveToTakeoffModule)),
        Entry(BossState.Boss2_PlatformJump, BossType.Boss2, nameof(Boss2PlatformJumpModule)),
        Entry(BossState.Boss2_PlatformFall, BossType.Boss2, nameof(Boss2PlatformFallModule)),
        Entry(BossState.Boss2_LandingRecovery, BossType.Boss2, nameof(Boss2LandingRecoveryModule)),
        Entry(BossState.Boss2_HeavySlashTelegraph, BossType.Boss2, nameof(Boss2HeavySlashTelegraphModule)),
        Entry(BossState.Boss2_DoubleCleaveTelegraph, BossType.Boss2, nameof(Boss2DoubleCleaveTelegraphModule)),
        Entry(BossState.Boss2_GroundStompTelegraph, BossType.Boss2, nameof(Boss2GroundStompTelegraphModule)),
        Entry(BossState.Boss2_LeapSmashTelegraph, BossType.Boss2, nameof(Boss2LeapSmashTelegraphModule)),
        Entry(BossState.Boss2_FrenzyChargeTelegraph, BossType.Boss2, nameof(Boss2FrenzyChargeTelegraphModule)),
        Entry(BossState.Boss2_EarthquakeRoarTelegraph, BossType.Boss2, nameof(Boss2EarthquakeRoarTelegraphModule)),
        Entry(BossState.Boss2_HeavySlashExecute, BossType.Boss2, nameof(Boss2HeavySlashExecuteModule)),
        Entry(BossState.Boss2_DoubleCleaveExecute, BossType.Boss2, nameof(Boss2DoubleCleaveExecuteModule)),
        Entry(BossState.Boss2_GroundStompExecute, BossType.Boss2, nameof(Boss2GroundStompExecuteModule)),
        Entry(BossState.Boss2_LeapSmashAscent, BossType.Boss2, nameof(Boss2LeapSmashAscentModule)),
        Entry(BossState.Boss2_LeapSmashDescent, BossType.Boss2, nameof(Boss2LeapSmashDescentModule)),
        Entry(BossState.Boss2_FrenzyChargeExecute, BossType.Boss2, nameof(Boss2FrenzyChargeExecuteModule)),
        Entry(BossState.Boss2_EarthquakeRoarExecute, BossType.Boss2, nameof(Boss2EarthquakeRoarExecuteModule)),
        Entry(BossState.Boss2_WallCrashStun, BossType.Boss2, nameof(Boss2WallCrashStunModule)),
        Entry(BossState.Boss2_Recovery, BossType.Boss2, nameof(Boss2RecoveryModule)),
        Entry(BossState.Boss2_PhaseTransition, BossType.Boss2, nameof(Boss2PhaseTransitionModule)),
        Entry(BossState.Boss2_Dead, BossType.Boss2, nameof(Boss2DeadModule))
    ];

    /// <summary>Unknown を除く固定状態の全対応を取得します。</summary>
    public static IReadOnlyList<FixedBossStateModule> Entries => _entries;

    /// <summary>固定状態に対応する module 記述を取得します。</summary>
    public static bool TryGet(BossState state, out FixedBossStateModule entry)
    {
        for (int index = 0; index < _entries.Length; index++)
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
        if (_entries.Length != Enum.GetValues<BossState>().Length - 1)
        {
            diagnostic = "固定 BossState の対応数が一致しません。";
            return false;
        }

        for (int index = 0; index < _entries.Length; index++)
        {
            FixedBossStateModule entry = _entries[index];
            if (!entry.IsValid)
            {
                diagnostic = $"固定 BossState の module 記述が不正です: {entry.State}";
                return false;
            }

            for (int other = index + 1; other < _entries.Length; other++)
            {
                if (_entries[other].State == entry.State || _entries[other].ModuleType == entry.ModuleType)
                {
                    diagnostic = $"固定 BossState/module が重複しています: {entry.State}";
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
            BossState.Boss1_Telegraph => typeof(Boss1TelegraphModule),
            BossState.Boss1_Aimed3ShotExecute => typeof(Aimed3ShotExecuteModule),
            BossState.Boss1_RingBurstExecute => typeof(RingBurstExecuteModule),
            BossState.Boss1_DoubleSpiralExecute => typeof(DoubleSpiralExecuteModule),
            BossState.Boss1_RainDropExecute => typeof(RainDropExecuteModule),
            BossState.Boss1_CrossChaosExecute => typeof(CrossChaosExecuteModule),
            BossState.Boss1_FastSniperExecute => typeof(FastSniperExecuteModule),
            BossState.Boss1_DiveRamExecute => typeof(DiveRamExecuteModule),
            BossState.Boss1_Recovery => typeof(Boss1RecoveryModule),
            BossState.Boss1_PhaseTransition => typeof(Boss1PhaseTransitionModule),
            BossState.Boss1_Dead => typeof(Boss1DeadModule),
            BossState.Boss2_Idle => typeof(Boss2IdleModule),
            BossState.Boss2_PatrolChase => typeof(Boss2PatrolChaseModule),
            BossState.Boss2_MoveToTakeoff => typeof(Boss2MoveToTakeoffModule),
            BossState.Boss2_PlatformJump => typeof(Boss2PlatformJumpModule),
            BossState.Boss2_PlatformFall => typeof(Boss2PlatformFallModule),
            BossState.Boss2_LandingRecovery => typeof(Boss2LandingRecoveryModule),
            BossState.Boss2_HeavySlashTelegraph => typeof(Boss2HeavySlashTelegraphModule),
            BossState.Boss2_DoubleCleaveTelegraph => typeof(Boss2DoubleCleaveTelegraphModule),
            BossState.Boss2_GroundStompTelegraph => typeof(Boss2GroundStompTelegraphModule),
            BossState.Boss2_LeapSmashTelegraph => typeof(Boss2LeapSmashTelegraphModule),
            BossState.Boss2_FrenzyChargeTelegraph => typeof(Boss2FrenzyChargeTelegraphModule),
            BossState.Boss2_EarthquakeRoarTelegraph => typeof(Boss2EarthquakeRoarTelegraphModule),
            BossState.Boss2_HeavySlashExecute => typeof(Boss2HeavySlashExecuteModule),
            BossState.Boss2_DoubleCleaveExecute => typeof(Boss2DoubleCleaveExecuteModule),
            BossState.Boss2_GroundStompExecute => typeof(Boss2GroundStompExecuteModule),
            BossState.Boss2_LeapSmashAscent => typeof(Boss2LeapSmashAscentModule),
            BossState.Boss2_LeapSmashDescent => typeof(Boss2LeapSmashDescentModule),
            BossState.Boss2_FrenzyChargeExecute => typeof(Boss2FrenzyChargeExecuteModule),
            BossState.Boss2_EarthquakeRoarExecute => typeof(Boss2EarthquakeRoarExecuteModule),
            BossState.Boss2_WallCrashStun => typeof(Boss2WallCrashStunModule),
            BossState.Boss2_Recovery => typeof(Boss2RecoveryModule),
            BossState.Boss2_PhaseTransition => typeof(Boss2PhaseTransitionModule),
            BossState.Boss2_Dead => typeof(Boss2DeadModule),
            _ => typeof(object)
        };
    }
}
