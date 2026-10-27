using AnimalBattle.Abilities.Attack;
using ErrorOr;

namespace AnimalBattle.Skills;

/// <summary>
/// <see cref="SkillConfig"/> から検証済みの <see cref="AttackDefinition"/> および <see cref="TimelineAttackAbility"/> を構築するビルダー。
/// </summary>
public static class SkillBuilder
{
    /// <summary>
    /// 単一技能設定から攻撃タイムライン定義をビルドします。
    /// </summary>
    public static ErrorOr<AttackDefinition> BuildAttackDefinition(StringId skillId, SkillConfig config)
    {
        var validation = config.Validate();
        if (validation.IsError) return validation.Errors;

        var actions = new List<AttackActionStep>();
        var index = 0;

        foreach (var effect in config.Effects)
        {
            if (effect.Trigger == SkillTriggerKind.OnGrounded)
            {
                actions.Add(new AttackActionStep(index++, effect, effect.Time, isOnGrounded: true));
            }
            else if (effect.Interval > 0.0f)
            {
                var count = 0;
                for (var t = effect.Time; t < config.CastDuration; t += effect.Interval)
                {
                    if (effect.RepeatCount > 0 && count >= effect.RepeatCount) break;
                    actions.Add(new AttackActionStep(index++, effect, t, isOnGrounded: false));
                    count++;
                }
            }
            else
            {
                actions.Add(new AttackActionStep(index++, effect, effect.Time, isOnGrounded: false));
            }
        }

        var definition = new AttackDefinition(
            skillId,
            config.WindUpTime,
            config.CastDuration,
            config.RecoveryTime,
            config.EffectiveRange,
            actions,
            config.ParsedArmorTag,
            config.ParsedRecoveryTag);

        if (!definition.IsValid)
        {
            return Error.Validation("Skill.InvalidDefinition", $"技能 '{skillId}' の内部定義検証に失敗しました。");
        }

        return definition;
    }

    /// <summary>
    /// 技能識別子に対応する <see cref="TimelineAttackAbility"/> をビルドします。
    /// </summary>
    public static ErrorOr<TimelineAttackAbility> BuildAbility(
        StringId skillId,
        SkillLibraryConfig library,
        float cooldownOverride = 0.0f)
    {
        if (!library.TryGetSkill(skillId, out var config))
        {
            return Error.NotFound("Skill.NotFound", $"技能ライブラリに未登録の技能です: {skillId}");
        }

        var defResult = BuildAttackDefinition(skillId, config);
        if (defResult.IsError) return defResult.Errors;

        var cooldown = cooldownOverride > 0.0f ? cooldownOverride : config.DefaultCooldown;
        var telegraphRenderer = SkillTelegraphBuilder.Build(config.Telegraph);
        var displayName = string.IsNullOrWhiteSpace(config.SkillName) ? skillId.ToString() : config.SkillName;

        return new TimelineAttackAbility(
            skillId,
            defResult.Value,
            cooldown,
            displayName: displayName,
            telegraphRenderer: telegraphRenderer)
        {
            Motion = config.Motion
        };
    }
}
