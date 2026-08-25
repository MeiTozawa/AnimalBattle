using System.Collections.Frozen;
using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Attack;

/// <summary>攻撃 ID 固定 map の初期化結果です。</summary>
public readonly record struct AttackRegistryResult(
    bool IsSuccess,
    AttackId AttackId,
    ErrorCode ErrorCode,
    string Message)
{
    /// <summary>成功結果を生成します。</summary>
    public static AttackRegistryResult Success() => new(true, default, ErrorCode.None, "攻撃 ID 固定 map は正常です。");

    /// <summary>失敗結果を生成します。</summary>
    public static AttackRegistryResult Failure(AttackId attackId, ErrorCode errorCode, string message)
        => new(false, attackId, errorCode, message);
}

/// <summary>
/// 初期化時に検証済みの攻撃定義を凍結して保持する互換境界です。
/// </summary>
/// <remarks>
/// この型は実行時登録 API を持ちません。定義の追加、削除、差し替えは次回初期化でだけ反映されます。
/// AttackId から Telegraph/Execute 状態を解決する実行用の固定対応は <see cref="AttackStateModuleMap" /> が提供します。
/// </remarks>
public sealed class AttackIdRegistry
{
    private readonly FrozenDictionary<AttackId, AttackDefinition> _definitions;
    private readonly AttackRegistryResult _validation;

    /// <summary>初期化に成功した攻撃定義数を取得します。</summary>
    public int Count => _definitions.Count;

    /// <summary>初期化結果を取得します。</summary>
    public AttackRegistryResult LastResult => _validation;

    /// <summary>空の攻撃定義集合を初期化します。</summary>
    public AttackIdRegistry()
        : this(Array.Empty<AttackDefinition>())
    {
    }

    /// <summary>検証済み攻撃定義を初期化時に一度だけ凍結します。</summary>
    /// <param name="definitions">攻撃設定境界から渡される攻撃定義集合。</param>
    public AttackIdRegistry(IEnumerable<AttackDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        Dictionary<AttackId, AttackDefinition> candidate = new();
        AttackRegistryResult validation = AttackRegistryResult.Success();
        foreach (AttackDefinition? definition in definitions)
        {
            if (definition is null)
            {
                validation = AttackRegistryResult.Failure(
                    default,
                    ErrorCode.InvalidConfiguration,
                    "攻撃設定が指定されていません。");
                continue;
            }

            if (!definition.IsValid)
            {
                validation = AttackRegistryResult.Failure(
                    definition.AttackId,
                    ErrorCode.InvalidConfiguration,
                    "攻撃設定が不正なため固定 map へ反映できません。");
                continue;
            }

            if (!candidate.TryAdd(definition.AttackId, definition))
            {
                validation = AttackRegistryResult.Failure(
                    definition.AttackId,
                    ErrorCode.InvalidAttackId,
                    "同じ Attack_Id は固定 map に重複できません。");
            }
        }

        _validation = validation;
        _definitions = validation.IsSuccess
            ? candidate.ToFrozenDictionary()
            : new Dictionary<AttackId, AttackDefinition>().ToFrozenDictionary();
    }

    /// <summary>攻撃 ID に対応する初期化済み定義を取得します。</summary>
    public bool TryGet(AttackId attackId, out AttackDefinition definition)
    {
        if (!_validation.IsSuccess)
        {
            definition = null!;
            return false;
        }

        return _definitions.TryGetValue(attackId, out definition!);
    }

    /// <summary>初期化済み攻撃定義の検証結果を取得します。</summary>
    public AttackRegistryResult Validate() => _validation;
}
