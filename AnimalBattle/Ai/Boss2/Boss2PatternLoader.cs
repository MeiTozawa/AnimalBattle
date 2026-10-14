using AnimalBattle.Abilities;
using AnimalBattle.Abilities.Attack;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using ErrorOr;

namespace AnimalBattle.Ai.Boss2;

/// <summary>
///     Boss 2（大地粉碎者）の TOML 設定から検証済みの <see cref="AttackDefinition" /> および <see cref="TimelineAttackAbility" /> を構築するローダーです。
///     技能識別タグ StringId を基盤とする攻撃能力を構築します。
/// </summary>
public static class Boss2PatternLoader {
    /// <summary>
    ///     指定技能タグの不変攻撃時序定義をビルドします。
    /// </summary>
    public static ErrorOr<AttackDefinition> BuildAttackDefinition(StringId skill, Boss2PatternConfig config)
    {
        Debug.Assert(config != null, "config は null であってはなりません。");
        if (config == null) return Error.Unexpected("Config.Null", "Boss 2 パターン設定が null です。");
        if (!config.TryGetSkill(skill, out var patternConfig))
        {
            return Error.NotFound("Skill.Unsupported", $"Boss 2 ではサポートされていない技能です: {skill}");
        }
        return patternConfig.BuildAttackDefinition(skill);
    }

    /// <summary>
    ///     指定技能タグの <see cref="TimelineAttackAbility" /> を構築します。
    /// </summary>
    public static ErrorOr<TimelineAttackAbility> BuildAbility(
        StringId skill,
        Boss2PatternConfig config,
        float cooldown = 0.0f)
    {
        Debug.Assert(config != null, "config は null であってはなりません。");
        if (config == null) return Error.Unexpected("Config.Null", "Boss 2 パターン設定が null です。");
        if (!config.TryGetSkill(skill, out var patternConfig))
        {
            return Error.NotFound("Skill.Unsupported", $"Boss 2 ではサポートされていない技能です: {skill}");
        }

        var defResult = patternConfig.BuildAttackDefinition(skill);
        if (defResult.IsError) return defResult.Errors;

        var telegraphRenderer = patternConfig.BuildTelegraphRenderer();
        var displayName = string.IsNullOrWhiteSpace(patternConfig.SkillName) ? skill.ToString() : patternConfig.SkillName;
        return new TimelineAttackAbility(skill, defResult.Value, cooldown, displayName: displayName, telegraphRenderer: telegraphRenderer);
    }

    /// <summary>
    ///     技能識別タグに対応する表示名を TOML 設定から解決します。
    /// </summary>
    public static string ResolveDisplayName(StringId skill, Boss2PatternConfig config)
    {
        Debug.Assert(config != null, "config は null であってはなりません。");
        if (config == null) return skill.ToString();
        return config.TryGetSkill(skill, out var patternConfig) && !string.IsNullOrWhiteSpace(patternConfig.SkillName)
            ? patternConfig.SkillName
            : skill.ToString();
    }

    /// <summary>
    ///     技能識別タグに対応する前兆演出レンダラーを取得します。
    /// </summary>
    public static Action<AbilityTelegraphContext>? ResolveTelegraphRenderer(StringId skill, Boss2PatternConfig config)
    {
        Debug.Assert(config != null, "config は null であってはなりません。");
        if (config == null) return null;
        return config.TryGetSkill(skill, out var patternConfig)
            ? patternConfig.BuildTelegraphRenderer()
            : null;
    }
}
