namespace AnimalBattle.Abilities;

/// <summary>
///     实体已赋予技能的运行时状态规格（持有技能实例、当前激活状态等）。
/// </summary>
public sealed class AbilitySpec(GameplayAbility ability) {
    /// <summary>能力定义实例。</summary>
    public GameplayAbility Ability { get; } = ability;

    /// <summary>能力标识符。</summary>
    public AbilityId Id => Ability.AbilityId;

    /// <summary>当前是否处于激活执行状态。</summary>
    public bool IsActive { get; internal set; }
}
