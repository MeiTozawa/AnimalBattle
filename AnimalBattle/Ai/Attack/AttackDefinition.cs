using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Attack;

/// <summary>攻撃の Telegraph、Execute、Recovery と component を保持する検証済み定義です。</summary>
public sealed class AttackDefinition {
    private readonly AttackComponent[] _components;

    /// <summary>攻撃定義を生成します。component 配列は初期化時にコピーされます。</summary>
    public AttackDefinition(
        BossSkill skill,
        float telegraphDuration,
        float executeDuration,
        float recoveryDuration,
        float range,
        IReadOnlyList<AttackComponent>? components = null,
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

        var componentCount = components?.Count ?? 0;
        _components = new AttackComponent[componentCount];
        if (components is not null)
            for (var index = 0; index < componentCount; index++)
                _components[index] = components[index];
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

    /// <summary>設定 component 数を取得します。</summary>
    public int ComponentCount => _components.Length;

    /// <summary>三段階と component の設定が実行可能かどうかを取得します。</summary>
    public bool IsValid => Skill.IsValid()
     && IsPositiveFinite(TelegraphDuration)
     && IsPositiveFinite(ExecuteDuration)
     && IsPositiveFinite(RecoveryDuration)
     && IsFinite(Range)
     && Range >= 0
     && PoiseThreshold >= 0.0f
     && IsFinite(PoiseThreshold)
     && IsAllComponentsValid();

    /// <summary>指定 index の component を取得します。</summary>
    public bool TryGetComponent(int index, out AttackComponent component)
    {
        if ((uint)index >= (uint)_components.Length)
        {
            component = default;
            return false;
        }

        component = _components[index];
        return true;
    }

    private bool IsAllComponentsValid()
    {
        for (var index = 0; index < _components.Length; index++)
        {
            if (!_components[index].IsValid) return false;

            for (var previous = 0; previous < index; previous++)
                if (_components[previous].Index == _components[index].Index)
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