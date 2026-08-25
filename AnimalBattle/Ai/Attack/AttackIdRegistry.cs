using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Attack;

/// <summary>攻撃 ID registry の検証結果です。</summary>
public readonly record struct AttackRegistryResult(
    bool IsSuccess,
    AttackId AttackId,
    ErrorCode ErrorCode,
    string Message)
{
    /// <summary>成功結果を生成します。</summary>
    public static AttackRegistryResult Success() => new(true, default, ErrorCode.None, "攻撃 registry は正常です。");

    /// <summary>失敗結果を生成します。</summary>
    public static AttackRegistryResult Failure(AttackId attackId, ErrorCode errorCode, string message)
        => new(false, attackId, errorCode, message);
}

/// <summary>文字列を使わず、安定した AttackId から攻撃定義を取得する registry です。</summary>
public sealed class AttackIdRegistry
{
    private readonly AttackDefinition?[] _definitions;
    private int _count;

    /// <summary>登録済み定義数を取得します。</summary>
    public int Count => _count;
    /// <summary>登録可能な最大定義数を取得します。</summary>
    public int Capacity => _definitions.Length;
    /// <summary>直近の登録診断を取得します。</summary>
    public AttackRegistryResult LastResult { get; private set; }

    /// <summary>攻撃 ID registry を初期化します。</summary>
    public AttackIdRegistry(int capacity = 64)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _definitions = new AttackDefinition[capacity];
        LastResult = AttackRegistryResult.Success();
    }

    /// <summary>検証済みの攻撃定義を一度だけ登録します。</summary>
    public bool TryRegister(AttackDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (!definition.IsValid)
        {
            LastResult = AttackRegistryResult.Failure(
                definition.AttackId,
                ErrorCode.InvalidConfiguration,
                "攻撃設定が不正なため registry へ登録できません。");
            return false;
        }

        if (TryGet(definition.AttackId, out _))
        {
            LastResult = AttackRegistryResult.Failure(
                definition.AttackId,
                ErrorCode.InvalidAttackId,
                "同じ Attack_Id は registry へ重複登録できません。");
            return false;
        }

        if (_count >= _definitions.Length)
        {
            LastResult = AttackRegistryResult.Failure(
                definition.AttackId,
                ErrorCode.InvalidConfiguration,
                "攻撃 registry の容量を超えました。");
            return false;
        }

        _definitions[_count++] = definition;
        LastResult = AttackRegistryResult.Success();
        return true;
    }

    /// <summary>攻撃 ID に対応する定義を取得します。</summary>
    public bool TryGet(AttackId attackId, out AttackDefinition definition)
    {
        if (!attackId.IsValid)
        {
            definition = null!;
            return false;
        }

        for (int index = 0; index < _count; index++)
        {
            AttackDefinition candidate = _definitions[index]!;
            if (candidate.AttackId == attackId)
            {
                definition = candidate;
                return true;
            }
        }

        definition = null!;
        return false;
    }

    /// <summary>登録済み定義がすべて有効かどうかを検証します。</summary>
    public AttackRegistryResult Validate()
    {
        for (int index = 0; index < _count; index++)
        {
            AttackDefinition definition = _definitions[index]!;
            if (!definition.IsValid)
            {
                LastResult = AttackRegistryResult.Failure(
                    definition.AttackId,
                    ErrorCode.InvalidConfiguration,
                    "登録済み攻撃設定が無効です。");
                return LastResult;
            }
        }

        LastResult = AttackRegistryResult.Success();
        return LastResult;
    }
}
