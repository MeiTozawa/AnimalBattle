using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Attack;

/// <summary>攻撃の各実行 component の種類を定義します。</summary>
public enum AttackComponentKind {
    /// <summary>弾を発射する component です。</summary>
    Bullet = 1,

    /// <summary>攻撃判定を生成する component です。</summary>
    Hitbox = 2,

    /// <summary>攻撃に伴う基本速度を設定する component です。</summary>
    Movement = 3,

    /// <summary>対象位置へのイージング平滑移動および滞空を行う汎用運動 component です。</summary>
    EasingMovement = 4,

    /// <summary>高速急降下・突進を行う汎用等速運動 component です。</summary>
    VelocityDive = 5,

    /// <summary>接地時に判定を生成するイベント駆動 component です。</summary>
    OnGroundedHitbox = 6,

    /// <summary>音効 Cue を発行する表現層 component です。</summary>
    AudioCue = 7,

    /// <summary>画面振動をトリガーする表現層 component です。</summary>
    ScreenShake = 8
}

/// <summary>設定から解決済みの攻撃 component を表す検証済み初期化データです。</summary>
/// <remarks>攻撃 component を生成します。</remarks>
public readonly struct AttackComponent(
    AttackComponentKind kind,
    int index,
    float param0 = 0.0f,
    float param1 = 0.0f,
    float param2 = 0.0f,
    float triggerTime = 0.0f) {
    /// <summary>component の種類を取得します。</summary>
    public AttackComponentKind Kind { get; } = kind;

    /// <summary>攻撃内で一意な component index を取得します。</summary>
    public int Index { get; } = index;

    /// <summary>主要パラメータ 0（X座標オフセット / 目標高度 / 弾速 等）を取得します。</summary>
    public float Param0 { get; } = param0;

    /// <summary>主要パラメータ 1（Y座標オフセット / 所要時間 / 弾数 等）を取得します。</summary>
    public float Param1 { get; } = param1;

    /// <summary>補助パラメータ 2（滞空時間 / 拡散角度 / 振動強度 等）を取得します。</summary>
    private float Param2 { get; } = param2;

    /// <summary>Execute 開始からの発動遅延時間（秒）を取得します。</summary>
    public float TriggerTime { get; } = triggerTime;

    /// <summary>X 座標オフセットまたは Param0 へのエイリアスです。</summary>
    public float X => Param0;

    /// <summary>Y 座標オフセットまたは Param1 へのエイリアスです。</summary>
    public float Y => Param1;

    /// <summary>component の設定値が実行可能かどうかを取得します。</summary>
    public bool IsValid => (int)Kind >= 1 && (int)Kind <= 8
     && Index >= 0
     && IsFinite(Param0)
     && IsFinite(Param1)
     && IsFinite(Param2)
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