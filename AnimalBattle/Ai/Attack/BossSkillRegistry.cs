using System.Collections.Frozen;
using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Attack;

/// <summary>技能固定 map の初期化結果です。</summary>
public readonly record struct BossSkillRegistryResult(
    bool IsSuccess,
    BossSkill Skill,
    ErrorCode ErrorCode,
    string Message)
{
    /// <summary>成功結果を生成します。</summary>
    public static BossSkillRegistryResult Success() => new(true, default, ErrorCode.None, "技能固定 map は正常です。");

    /// <summary>失敗結果を生成します。</summary>
    public static BossSkillRegistryResult Failure(BossSkill skill, ErrorCode errorCode, string message)
        => new(false, skill, errorCode, message);
}

/// <summary>
/// 初期化時に検証済みの攻撃定義を凍結して保持する境界です。
/// </summary>
/// <remarks>
/// この型は実行時登録 API を持ちません。定義の追加、削除、差し替えは次回初期化でだけ反映されます。
/// BossSkill から Telegraph/Execute 状態を解決する実行用の固定対応は <see cref="AttackStateModuleMap" /> が提供します。
/// </remarks>
public sealed class BossSkillRegistry
{
    private readonly FrozenDictionary<BossSkill, AttackDefinition> _definitions;
    private readonly BossSkillRegistryResult _validation;

    /// <summary>初期化に成功した攻撃定義数を取得します。</summary>
    public int Count => _definitions.Count;

    /// <summary>初期化結果を取得します。</summary>
    public BossSkillRegistryResult LastResult => _validation;

    /// <summary>空の攻撃定義集合を初期化します。</summary>
    public BossSkillRegistry()
        : this(Array.Empty<AttackDefinition>())
    {
    }

    /// <summary>検証済み攻撃定義を初期化時に一度だけ凍結します。</summary>
    /// <param name="definitions">攻撃設定境界から渡される攻撃定義集合。</param>
    public BossSkillRegistry(IEnumerable<AttackDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        Dictionary<BossSkill, AttackDefinition> candidate = new();
        BossSkillRegistryResult validation = BossSkillRegistryResult.Success();
        foreach (AttackDefinition? definition in definitions)
        {
            if (definition is null)
            {
                validation = BossSkillRegistryResult.Failure(
                    default,
                    ErrorCode.InvalidConfiguration,
                    "攻撃設定が指定されていません。");
                continue;
            }

            if (!definition.IsValid)
            {
                validation = BossSkillRegistryResult.Failure(
                    definition.Skill,
                    ErrorCode.InvalidConfiguration,
                    "攻撃設定が不正なため固定 map へ反映できません。");
                continue;
            }

            if (!candidate.TryAdd(definition.Skill, definition))
            {
                validation = BossSkillRegistryResult.Failure(
                    definition.Skill,
                    ErrorCode.InvalidBossSkill,
                    "同じ BossSkill は固定 map に重複できません。");

            }
        }

        _validation = validation;
        _definitions = validation.IsSuccess
            ? candidate.ToFrozenDictionary()
            : new Dictionary<BossSkill, AttackDefinition>().ToFrozenDictionary();
    }

    /// <summary>技能識別子に対応する初期化済み定義を取得します。</summary>
    public bool Get(BossSkill skill, out AttackDefinition definition)
    {
        if (!_validation.IsSuccess)
        {
            definition = null!;
            return false;
        }

        return _definitions.TryGetValue(skill, out definition!);
    }

    /// <summary>初期化済み攻撃定義の検証結果を取得します。</summary>
    public bool Validate(out string diagnostic)
    {
        if (!_validation.IsSuccess)
        {
            diagnostic = _validation.Message;
            return false;
        }

        diagnostic = "攻撃定義の固定 map は正常です。";
        return true;
    }
}
