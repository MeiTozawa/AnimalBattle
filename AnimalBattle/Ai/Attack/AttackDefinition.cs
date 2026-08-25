using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Attack;

/// <summary>攻撃の各実行 component の種類を定義します。</summary>
public enum AttackComponentKind
{
    /// <summary>弾を発射する component です。</summary>
    Bullet = 1,
    /// <summary>攻撃判定を生成する component です。</summary>
    Hitbox = 2,
    /// <summary>攻撃に伴う速度を設定する component です。</summary>
    Movement = 3
}

/// <summary>設定から解決済みの攻撃 component を表す初期化時データです。</summary>
public readonly struct AttackComponent
{
    /// <summary>component の種類を取得します。</summary>
    public AttackComponentKind Kind { get; }
    /// <summary>攻撃内で一意な component index を取得します。</summary>
    public int Index { get; }
    /// <summary>component に渡す X 値を取得します。</summary>
    public float X { get; }
    /// <summary>component に渡す Y 値を取得します。</summary>
    public float Y { get; }

    /// <summary>攻撃 component を生成します。</summary>
    public AttackComponent(AttackComponentKind kind, int index, float x = 0, float y = 0)
    {
        Kind = kind;
        Index = index;
        X = x;
        Y = y;
    }

    /// <summary>component の設定値が実行可能かどうかを取得します。</summary>
    public bool IsValid => (Kind is AttackComponentKind.Bullet
        or AttackComponentKind.Hitbox
        or AttackComponentKind.Movement)
        && Index >= 0
        && IsFinite(X)
        && IsFinite(Y);

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}

/// <summary>攻撃の Telegraph、Execute、Recovery と component を保持する検証済み定義です。</summary>
public sealed class AttackDefinition
{
    private readonly AttackComponent[] _components;

    /// <summary>攻撃識別子を取得します。</summary>
    public AttackId AttackId { get; }
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
    public bool IsValid => AttackId.IsValid
        && IsPositiveFinite(TelegraphDuration)
        && IsPositiveFinite(ExecuteDuration)
        && IsPositiveFinite(RecoveryDuration)
        && IsFinite(Range)
        && Range >= 0
        && AllComponentsValid();

    /// <summary>攻撃定義を生成します。component 配列は初期化時にコピーされます。</summary>
    public AttackDefinition(
        AttackId attackId,
        float telegraphDuration,
        float executeDuration,
        float recoveryDuration,
        float range,
        IReadOnlyList<AttackComponent>? components = null)
    {
        AttackId = attackId;
        TelegraphDuration = telegraphDuration;
        ExecuteDuration = executeDuration;
        RecoveryDuration = recoveryDuration;
        Range = range;
        int componentCount = components?.Count ?? 0;
        _components = new AttackComponent[componentCount];
        if (components is not null)
        {
            for (int index = 0; index < componentCount; index++)
            {
                _components[index] = components[index];
            }
        }
    }

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

    private bool AllComponentsValid()
    {
        for (int index = 0; index < _components.Length; index++)
        {
            if (!_components[index].IsValid)
            {
                return false;
            }

            for (int previous = 0; previous < index; previous++)
            {
                if (_components[previous].Index == _components[index].Index)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool IsPositiveFinite(float value) => IsFinite(value) && value > 0;
    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
