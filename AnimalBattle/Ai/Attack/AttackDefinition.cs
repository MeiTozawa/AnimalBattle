using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Attack;

/// <summary>攻撃の各実行 component の種類を定義します。</summary>
public enum AttackComponentKind {
    /// <summary>弾を発射する component です。</summary>
    Bullet = 1,

    /// <summary>攻撃判定を生成する component です。</summary>
    Hitbox = 2,

    /// <summary>攻撃に伴う速度を設定する component です。</summary>
    Movement = 3,

    /// <summary>対象頭上へイージング飛翔・滞空する component です。</summary>
    FlyToTarget = 4,

    /// <summary>真下へ高速急降下する component です。</summary>
    Dive = 5,

    /// <summary>接地時に判定を生成するイベント駆動 component です。</summary>
    OnGroundedHitbox = 6
}

/// <summary>設定から解決済みの攻撃 component を表す初期化時データです。</summary>
/// <remarks>攻撃 component を生成します。</remarks>
public readonly struct AttackComponent(
    AttackComponentKind kind,
    int index,
    float x = 0,
    float y = 0,
    float triggerTime = 0.0f) {
    /// <summary>component の種類を取得します。</summary>
    public AttackComponentKind Kind { get; } = kind;

    /// <summary>攻撃内で一意な component index を取得します。</summary>
    public int Index { get; } = index;

    /// <summary>component に渡す X 値を取得します。</summary>
    public float X { get; } = x;

    /// <summary>component に渡す Y 値を取得します。</summary>
    public float Y { get; } = y;

    /// <summary>Execute 開始からの発動遅延時間（秒）を取得します。</summary>
    public float TriggerTime { get; } = triggerTime;

    /// <summary>component の設定値が実行可能かどうかを取得します。</summary>
    public bool IsValid => Kind is AttackComponentKind.Bullet
            or AttackComponentKind.Hitbox
            or AttackComponentKind.Movement
            or AttackComponentKind.FlyToTarget
            or AttackComponentKind.Dive
            or AttackComponentKind.OnGroundedHitbox
     && Index >= 0
     && IsFinite(X)
     && IsFinite(Y)
     && TriggerTime >= 0.0f
     && IsFinite(TriggerTime);

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

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
        IReadOnlyList<AttackComponent>? components = null)
    {
        Skill = skill;
        TelegraphDuration = telegraphDuration;
        ExecuteDuration = executeDuration;
        RecoveryDuration = recoveryDuration;
        Range = range;

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

    /// <summary>設定 component 数を取得します。</summary>
    public int ComponentCount => _components.Length;

    /// <summary>三段階と component の設定が実行可能かどうかを取得します。</summary>
    public bool IsValid => Skill.IsValid()
     && IsPositiveFinite(TelegraphDuration)
     && IsPositiveFinite(ExecuteDuration)
     && IsPositiveFinite(RecoveryDuration)
     && IsFinite(Range)
     && Range >= 0
     && AllComponentsValid();

    /// <summary>指定 index の component を取得します。</summary>
    public bool GetComponent(int index, out AttackComponent component)
    {
        if ((uint)index >= (uint)_components.Length)
        {
            component = default;
            return false;
        }

        component = _components[index];
        return true;
    }

    private bool AllComponentsValid()
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