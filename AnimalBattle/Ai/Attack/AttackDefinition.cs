using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Attack;

/// <summary>攻撃の Telegraph、Execute、Recovery と各アクションステップを保持する検証済み定義です。</summary>
public sealed class AttackDefinition {
    private readonly AttackActionStep[] _actions;

    /// <summary>攻撃定義を生成します。アクション配列は初期化時にコピーされます。</summary>
    public AttackDefinition(
        BossSkill skill,
        float telegraphDuration,
        float executeDuration,
        float recoveryDuration,
        float range,
        IReadOnlyList<AttackActionStep>? actions = null,
        ArmorType armor = ArmorType.None,
        float poiseThreshold = 0.0f)
    {
        Skill = skill;
        TelegraphDuration = telegraphDuration;
        ExecuteDuration = executeDuration;
        RecoveryDuration = recoveryDuration;
        Range = range;
        Armor = armor;
        PoiseThreshold = poiseThreshold;

        var actionCount = actions?.Count ?? 0;
        _actions = new AttackActionStep[actionCount];
        if (actions is not null)
            for (var index = 0; index < actionCount; index++)
                _actions[index] = actions[index];
    }

    /// <summary>技能識別子を取得します。</summary>
    public BossSkill Skill { get; }

    /// <summary>Telegraph の設定時間を取得します。</summary>
    public float TelegraphDuration { get; }

    /// <summary>Execute の設定時間を取得します。</summary>
    public float ExecuteDuration { get; }

    /// <summary>Recovery の設定時間を取得します。</summary>
    public float RecoveryDuration { get; }

    /// <summary>攻撃範囲を取得します。</summary>
    public float Range { get; }

    /// <summary>出招実行時のアーマー・耐性種別を取得します。</summary>
    public ArmorType Armor { get; }

    /// <summary>破防に必要な靭性ダメージ閾値を取得します。</summary>
    public float PoiseThreshold { get; }

    /// <summary>設定アクションステップ数を取得します。</summary>
    public int ActionCount => _actions.Length;

    /// <summary>三段階とアクションステップの設定が実行可能かどうかを取得します。</summary>
    public bool IsValid => Skill.IsValid()
     && IsPositiveFinite(TelegraphDuration)
     && IsPositiveFinite(ExecuteDuration)
     && IsPositiveFinite(RecoveryDuration)
     && IsFinite(Range)
     && Range >= 0
     && PoiseThreshold >= 0.0f
     && IsFinite(PoiseThreshold)
     && IsAllActionsValid();

    /// <summary>指定 index のアクションステップを取得します。</summary>
    public bool TryGetAction(int index, out AttackActionStep action)
    {
        if ((uint)index >= (uint)_actions.Length)
        {
            action = default;
            return false;
        }

        action = _actions[index];
        return true;
    }

    private bool IsAllActionsValid()
    {
        for (var index = 0; index < _actions.Length; index++)
        {
            if (!_actions[index].IsValid) return false;

            for (var previous = 0; previous < index; previous++)
                if (_actions[previous].Index == _actions[index].Index)
                    return false;
        }

        return true;
    }

    private static bool IsPositiveFinite(float value)
    {
        return IsFinite(value) && value > 0;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}