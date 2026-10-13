using AnimalBattle.Abilities;
using AnimalBattle.Abilities.Attack;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using ErrorOr;

namespace AnimalBattle.Ai.Boss2;

/// <summary>
///     Boss 2（大地粉碎者）の TOML 設定から検証済みの <see cref="AttackDefinition" /> および <see cref="TimelineAttackAbility" /> を構築するローダーです。
///     UE GAS 準拠：各招式に適切な CooldownTags を自動設定します。
/// </summary>
public static class Boss2PatternLoader {
    /// <summary>
    ///     指定技能タグの不変攻撃時序定義をビルドします。
    /// </summary>
    public static ErrorOr<AttackDefinition> BuildAttackDefinition(StringId skill, Boss2PatternConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (!config.TryGetSkill(skill, out var patternConfig))
        {
            return Error.NotFound("Skill.Unsupported", $"Boss 2 ではサポートされていない技能です: {skill}");
        }
        return patternConfig.BuildAttackDefinition(skill);
    }

    /// <summary>
    ///     指定技能タグの <see cref="TimelineAttackAbility" /> を構築し、UE GAS 冷却タグをバインドします。
    /// </summary>
    public static ErrorOr<TimelineAttackAbility> BuildAbility(
        StringId skill,
        Boss2PatternConfig config,
        float cooldown = 0.0f)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (!config.TryGetSkill(skill, out var patternConfig))
        {
            return Error.NotFound("Skill.Unsupported", $"Boss 2 ではサポートされていない技能です: {skill}");
        }

        var defResult = patternConfig.BuildAttackDefinition(skill);
        if (defResult.IsError) return defResult.Errors;

        var cooldownTags = patternConfig.ResolveCooldownTags();
        var telegraphRenderer = patternConfig.BuildTelegraphRenderer();
        var displayName = string.IsNullOrWhiteSpace(patternConfig.SkillName) ? skill.ToString() : patternConfig.SkillName;
        return new TimelineAttackAbility(skill, defResult.Value, cooldown, cooldownTags, displayName: displayName, telegraphRenderer: telegraphRenderer);
    }

    /// <summary>
    ///     技能識別タグに対応する表示名を TOML 設定から解決します。
    /// </summary>
    public static string ResolveDisplayName(StringId skill, Boss2PatternConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return config.TryGetSkill(skill, out var patternConfig) && !string.IsNullOrWhiteSpace(patternConfig.SkillName)
            ? patternConfig.SkillName
            : skill.ToString();
    }

    /// <summary>
    ///     技能識別タグに対応する前兆演出レンダラーを取得します。
    /// </summary>
    public static Action<AbilityTelegraphContext>? ResolveTelegraphRenderer(StringId skill, Boss2PatternConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return config.TryGetSkill(skill, out var patternConfig)
            ? patternConfig.BuildTelegraphRenderer()
            : null;
    }

    /// <summary>
    ///     技能識別タグに対応する UE GAS 冷却グループタグを取得します。
    /// </summary>
    public static GameplayTag ResolveCooldownTags(StringId skill, Boss2PatternConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return config.TryGetSkill(skill, out var patternConfig)
            ? patternConfig.ResolveCooldownTags()
            : GameplayTag.None;
    }
}
