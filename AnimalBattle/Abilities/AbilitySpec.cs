using AnimalBattle.Core;

namespace AnimalBattle.Abilities;

/// <summary>
///     实体已赋予技能的运行时状态规格（持有句柄、技能实例、当前激活状态等）。
/// </summary>
public sealed class AbilitySpec(AbilitySpecHandle handle, GameplayAbility ability) {
    /// <summary>一意の能力スペックハンドル。</summary>
    public AbilitySpecHandle Handle { get; } = handle;

    /// <summary>能力定义实例。</summary>
    public GameplayAbility Ability { get; } = ability;

    /// <summary>能力識別タグ。</summary>
    public StringId Tag => Ability.AbilityTag;

    /// <summary>当前是否处于激活执行状态。</summary>
    public bool IsActive { get; internal set; }

    /// <summary>後方互換コンストラクタ。</summary>
    public AbilitySpec(GameplayAbility ability) : this(AbilitySpecHandle.Generate(), ability) { }
}
