using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Boss2;
using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Attack;

/// <summary>AttackId から固定 Telegraph/Execute State_Module へ解決した対応です。</summary>
public readonly record struct AttackStateModuleMapping(
    AttackId AttackId,
    BossType BossType,
    BossState TelegraphState,
    BossState ExecuteState,
    BossState SecondaryExecuteState = BossState.Unknown)
{
    /// <summary>対応が固定実行状態を持つかどうかを取得します。</summary>
    public bool IsValid => AttackId.IsValid
        && BossType != BossType.Unknown
        && TelegraphState.IsValid()
        && ExecuteState.IsValid()
        && (SecondaryExecuteState == BossState.Unknown || SecondaryExecuteState.IsValid());
}

/// <summary>
/// 攻撃識別子と具体的な Telegraph/Execute State_Module の固定対応です。
/// </summary>
/// <remarks>
/// この map はコードに固定され、実行中の登録、削除、生成、差し替えを行いません。
/// LeapSmash だけは Execute の上昇と落下を同じ AttackId の二段状態として保持します。
/// </remarks>
public static class AttackStateModuleMap
{
    private static readonly IReadOnlyList<AttackStateModuleMapping> _entries = new List<AttackStateModuleMapping>
    {
        new(Boss1AttackIds.Aimed3Shot, BossType.Boss1, Boss1StateIds.Telegraph, Boss1StateIds.Aimed3ShotExecute),
        new(Boss1AttackIds.RingBurst, BossType.Boss1, Boss1StateIds.Telegraph, Boss1StateIds.RingBurstExecute),
        new(Boss1AttackIds.DoubleSpiral, BossType.Boss1, Boss1StateIds.Telegraph, Boss1StateIds.DoubleSpiralExecute),
        new(Boss1AttackIds.RainDrop, BossType.Boss1, Boss1StateIds.Telegraph, Boss1StateIds.RainDropExecute),
        new(Boss1AttackIds.CrossChaos, BossType.Boss1, Boss1StateIds.Telegraph, Boss1StateIds.CrossChaosExecute),
        new(Boss1AttackIds.FastSniper, BossType.Boss1, Boss1StateIds.Telegraph, Boss1StateIds.FastSniperExecute),
        new(Boss1AttackIds.DiveRam, BossType.Boss1, Boss1StateIds.Telegraph, Boss1StateIds.DiveRamExecute),
        new(Boss2AttackIds.HeavySlash, BossType.Boss2, Boss2StateIds.HeavySlashTelegraph, Boss2StateIds.HeavySlashExecute),
        new(Boss2AttackIds.DoubleCleave, BossType.Boss2, Boss2StateIds.DoubleCleaveTelegraph, Boss2StateIds.DoubleCleaveExecute),
        new(Boss2AttackIds.GroundStomp, BossType.Boss2, Boss2StateIds.GroundStompTelegraph, Boss2StateIds.GroundStompExecute),
        new(Boss2AttackIds.LeapSmash, BossType.Boss2, Boss2StateIds.LeapSmashTelegraph, Boss2StateIds.LeapSmashAscent, Boss2StateIds.LeapSmashDescent),
        new(Boss2AttackIds.FrenzyCharge, BossType.Boss2, Boss2StateIds.FrenzyChargeTelegraph, Boss2StateIds.FrenzyChargeExecute),
        new(Boss2AttackIds.EarthquakeRoar, BossType.Boss2, Boss2StateIds.EarthquakeRoarTelegraph, Boss2StateIds.EarthquakeRoarExecute)
    }.AsReadOnly();

    /// <summary>固定された全攻撃対応を取得します。</summary>
    public static IReadOnlyList<AttackStateModuleMapping> Entries => _entries;

    /// <summary>AttackId に対応する固定 Telegraph/Execute mapping を取得します。</summary>
    public static bool TryGet(AttackId attackId, out AttackStateModuleMapping mapping)
    {
        for (int index = 0; index < _entries.Count; index++)
        {
            if (_entries[index].AttackId == attackId)
            {
                mapping = _entries[index];
                return true;
            }
        }

        mapping = default;
        return false;
    }

    /// <summary>全攻撃が固定された具体的な module 状態へ対応するか検査します。</summary>
    public static bool IsComplete(out string diagnostic)
    {
        for (int index = 0; index < _entries.Count; index++)
        {
            AttackStateModuleMapping entry = _entries[index];
            if (!entry.IsValid)
            {
                diagnostic = $"AttackId の固定 module 対応が不正です: {entry.AttackId.Value}";
                return false;
            }

            for (int other = index + 1; other < _entries.Count; other++)
            {
                if (_entries[other].AttackId == entry.AttackId)
                {
                    diagnostic = $"AttackId の固定 module 対応が重複しています: {entry.AttackId.Value}";
                    return false;
                }
            }
        }

        diagnostic = "AttackId と Telegraph/Execute の固定対応は完全です。";
        return true;
    }
}
