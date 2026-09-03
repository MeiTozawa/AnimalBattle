using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Attack;

/// <summary>BossSkill から固定 Telegraph/Execute State_Module へ解決した対応です。</summary>
public readonly record struct AttackStateModuleMapping(
    BossSkill Skill,
    BossType BossType,
    BossState TelegraphState,
    BossState ExecuteState)
{
    /// <summary>対応が固定実行状態を持つかどうかを取得します。</summary>
    public bool IsValid => Skill.IsValid()
        && BossType != BossType.Unknown
        && TelegraphState.IsValid()
        && ExecuteState.IsValid();
}

/// <summary>
/// 技能識別子と具体的な Telegraph/Execute State_Module の固定対応です。
/// </summary>
/// <remarks>
/// この map はコードに固定され、実行中の登録、削除、生成、差し替えを行いません。
/// 全技能が均一に対称的な Telegraph と Execute 状態へ対応します。
/// </remarks>
public static class AttackStateModuleMap
{
    private static readonly IReadOnlyList<AttackStateModuleMapping> _entries = new List<AttackStateModuleMapping>
    {
        new(BossSkill.Aimed3Shot, BossType.Boss1, BossState.Boss1_Telegraph, BossState.Boss1_Execute),
        new(BossSkill.RingBurst, BossType.Boss1, BossState.Boss1_Telegraph, BossState.Boss1_Execute),
        new(BossSkill.DoubleSpiral, BossType.Boss1, BossState.Boss1_Telegraph, BossState.Boss1_Execute),
        new(BossSkill.RainDrop, BossType.Boss1, BossState.Boss1_Telegraph, BossState.Boss1_Execute),
        new(BossSkill.CrossChaos, BossType.Boss1, BossState.Boss1_Telegraph, BossState.Boss1_Execute),
        new(BossSkill.FastSniper, BossType.Boss1, BossState.Boss1_Telegraph, BossState.Boss1_Execute),
        new(BossSkill.DiveRam, BossType.Boss1, BossState.Boss1_Telegraph, BossState.Boss1_Execute),
        new(BossSkill.HeavySlash, BossType.Boss2, BossState.Boss2_Telegraph, BossState.Boss2_Execute),
        new(BossSkill.DoubleCleave, BossType.Boss2, BossState.Boss2_Telegraph, BossState.Boss2_Execute),
        new(BossSkill.GroundStomp, BossType.Boss2, BossState.Boss2_Telegraph, BossState.Boss2_Execute),
        new(BossSkill.LeapSmash, BossType.Boss2, BossState.Boss2_Telegraph, BossState.Boss2_Execute),
        new(BossSkill.FrenzyCharge, BossType.Boss2, BossState.Boss2_Telegraph, BossState.Boss2_Execute),
        new(BossSkill.EarthquakeRoar, BossType.Boss2, BossState.Boss2_Telegraph, BossState.Boss2_Execute)
    }.AsReadOnly();


    /// <summary>固定された全技能対応を取得します。</summary>
    public static IReadOnlyList<AttackStateModuleMapping> Entries => _entries;

    /// <summary>BossSkill に対応する固定 Telegraph/Execute mapping を取得します。</summary>
    public static bool Get(BossSkill skill, out AttackStateModuleMapping mapping)
    {
        for (int index = 0; index < _entries.Count; index++)
        {
            if (_entries[index].Skill == skill)
            {
                mapping = _entries[index];
                return true;
            }
        }

        mapping = default;
        return false;
    }

    /// <summary>全技能が固定された具体的な module 状態へ対応するか検査します。</summary>
    public static bool IsComplete(out string diagnostic)
    {
        for (int index = 0; index < _entries.Count; index++)
        {
            AttackStateModuleMapping entry = _entries[index];
            if (!entry.IsValid)
            {
                diagnostic = $"BossSkill の固定 module 対応が不正です: {entry.Skill}";
                return false;
            }

            for (int other = index + 1; other < _entries.Count; other++)
            {
                if (_entries[other].Skill == entry.Skill)
                {
                    diagnostic = $"BossSkill の固定 module 対応が重複しています: {entry.Skill}";
                    return false;
                }
            }

        }

        diagnostic = "BossSkill と Telegraph/Execute の固定対応は完全です。";
        return true;
    }
}

