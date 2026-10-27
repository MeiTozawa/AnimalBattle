using AnimalBattle.Abilities.Attack;
using ErrorOr;

namespace AnimalBattle.Ai;

/// <summary>
///     TOML 設定から検証済みの <see cref="AttackDefinition" /> および <see cref="TimelineAttackAbility" /> を構築する共通ローダー。
/// </summary>
public static class BossPatternLoader {
    /// <summary>
    ///     指定技能タグの不変攻撃タイムライン定義をビルドします。
    /// </summary>
    public static ErrorOr<AttackDefinition> BuildAttackDefinition(StringId skill, BossPatternConfigContainerBase config)
    {
        Debug.Assert(config != null, "config は null であってはなりません。");
        if (config == null) return Error.Unexpected("Config.Null", "Boss パターン設定が null です。");
        if (!config.TryGetSkill(skill, out var patternConfig))
        {
            return Error.NotFound("Skill.Unsupported", $"サポートされていない技能です: {skill}");
        }
        return patternConfig.BuildAttackDefinition(skill);
    }

    /// <summary>
    ///     指定技能タグの <see cref="TimelineAttackAbility" /> を構築します。
    /// </summary>
    public static ErrorOr<TimelineAttackAbility> BuildAbility(
        StringId skill,
        BossPatternConfigContainerBase config,
        float cooldown = 0.0f)
    {
        Debug.Assert(config != null, "config は null であってはなりません。");
        if (config == null) return Error.Unexpected("Config.Null", "Boss パターン設定が null です。");
        if (!config.TryGetSkill(skill, out var patternConfig))
        {
            return Error.NotFound("Skill.Unsupported", $"サポートされていない技能です: {skill}");
        }

        var defResult = patternConfig.BuildAttackDefinition(skill);
        if (defResult.IsError) return defResult.Errors;

        var telegraphRenderer = patternConfig.BuildTelegraphRenderer();
        var displayName = string.IsNullOrWhiteSpace(patternConfig.SkillName) ? skill.ToString() : patternConfig.SkillName;
        return new TimelineAttackAbility(skill, defResult.Value, cooldown, displayName: displayName, telegraphRenderer: telegraphRenderer);
    }

    /// <summary>
    ///     技能識別タグに対応する表示名を解決します。
    /// </summary>
    public static string ResolveDisplayName(StringId skill, BossPatternConfigContainerBase config)
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
    public static Action<AbilityTelegraphContext>? ResolveTelegraphRenderer(StringId skill, BossPatternConfigContainerBase config)
    {
        Debug.Assert(config != null, "config は null であってはなりません。");
        if (config == null) return null;
        return config.TryGetSkill(skill, out var patternConfig)
            ? patternConfig.BuildTelegraphRenderer()
            : null;
    }
}
