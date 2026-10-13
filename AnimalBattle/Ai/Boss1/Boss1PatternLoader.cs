using AnimalBattle.Abilities;
using AnimalBattle.Abilities.Attack;
using AnimalBattle.Abilities.Skills;
using AnimalBattle.Configs;
using AnimalBattle.Render;
using ErrorOr;

namespace AnimalBattle.Ai.Boss1;

/// <summary>
///     Boss 1（古代猫帝）の TOML 設定から検証済みの <see cref="AttackDefinition" /> および <see cref="TimelineAttackAbility" /> を構築するローダーです。
///     UE GAS 準拠：各招式に適切な CooldownTags を自動設定します。
/// </summary>
public static class Boss1PatternLoader {
    /// <summary>
    ///     指定技能の不変攻撃時序定義をビルドします。
    /// </summary>
    public static ErrorOr<AttackDefinition> BuildAttackDefinition(AbilityId skill, Boss1PatternConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return skill switch {
            AbilityId.DiveRam => SkillConfigLoader.BuildAttackDefinition(
                skill,
                config.DiveRam.WindUpTime,
                config.DiveRam.CastDuration,
                config.DiveRam.RecoveryTime,
                config.DiveRam.EffectiveRange,
                config.DiveRam.Events,
                armorTag: GameplayTag.SuperArmor),

            AbilityId.Aimed3Shot => SkillConfigLoader.BuildAttackDefinition(
                skill,
                config.Aimed3Shot.WindUpTime,
                config.Aimed3Shot.CastDuration,
                config.Aimed3Shot.RecoveryTime,
                config.Aimed3Shot.EffectiveRange,
                config.Aimed3Shot.Events,
                config.Aimed3Shot.Interval,
                config.Aimed3Shot.BulletSpeed,
                config.Aimed3Shot.BulletCount,
                config.Aimed3Shot.SpreadAngle),

            AbilityId.RingBurst => SkillConfigLoader.BuildAttackDefinition(
                skill,
                config.RingBurst.WindUpTime,
                config.RingBurst.CastDuration,
                config.RingBurst.RecoveryTime,
                config.RingBurst.EffectiveRange,
                config.RingBurst.Events,
                config.RingBurst.Interval,
                config.RingBurst.BulletSpeed,
                config.RingBurst.BulletCount,
                config.RingBurst.AngleStep),

            AbilityId.DoubleSpiral => SkillConfigLoader.BuildAttackDefinition(
                skill,
                config.DoubleSpiral.WindUpTime,
                config.DoubleSpiral.CastDuration,
                config.DoubleSpiral.RecoveryTime,
                config.DoubleSpiral.EffectiveRange,
                config.DoubleSpiral.Events,
                config.DoubleSpiral.Interval,
                config.DoubleSpiral.BulletSpeed,
                2,
                config.DoubleSpiral.AngleStep),

            AbilityId.RainDrop => SkillConfigLoader.BuildAttackDefinition(
                skill,
                config.RainDrop.WindUpTime,
                config.RainDrop.CastDuration,
                config.RainDrop.RecoveryTime,
                config.RainDrop.EffectiveRange,
                config.RainDrop.Events,
                config.RainDrop.Interval,
                config.RainDrop.BulletSpeed,
                1),

            AbilityId.CrossChaos => SkillConfigLoader.BuildAttackDefinition(
                skill,
                config.CrossChaos.WindUpTime,
                config.CrossChaos.CastDuration,
                config.CrossChaos.RecoveryTime,
                config.CrossChaos.EffectiveRange,
                config.CrossChaos.Events,
                config.CrossChaos.Interval,
                config.CrossChaos.BulletSpeed,
                4,
                config.CrossChaos.AngleStep),

            AbilityId.FastSniper => SkillConfigLoader.BuildAttackDefinition(
                skill,
                config.FastSniper.WindUpTime,
                config.FastSniper.CastDuration,
                config.FastSniper.RecoveryTime,
                config.FastSniper.EffectiveRange,
                config.FastSniper.Events,
                config.FastSniper.Interval,
                config.FastSniper.BulletSpeed,
                1),

            _ => Error.NotFound("Skill.Unsupported", $"Boss 1 ではサポートされていない技能です: {skill}")
        };
    }

    /// <summary>
    ///     指定技能の <see cref="TimelineAttackAbility" /> を構築し、UE GAS 冷却タグをバインドします。
    /// </summary>
    public static ErrorOr<TimelineAttackAbility> BuildAbility(
        AbilityId skill,
        Boss1PatternConfig config,
        float cooldown = 0.0f)
    {
        var defResult = BuildAttackDefinition(skill, config);
        if (defResult.IsError) return defResult.Errors;

        var cooldownTags = ResolveCooldownTags(skill);
        var telegraphRenderer = ResolveTelegraphRenderer(skill, config);
        return new TimelineAttackAbility(skill, defResult.Value, cooldown, cooldownTags, telegraphRenderer: telegraphRenderer);
    }

    /// <summary>
    ///     技能識別子に対応する前兆演出レンダラーを取得します。
    /// </summary>
    public static Action<AbilityTelegraphContext>? ResolveTelegraphRenderer(AbilityId skill, Boss1PatternConfig pattern1)
    {
        return skill switch {
            AbilityId.Aimed3Shot => ctx => TelegraphRenderHelper.DrawAimingLasers(
                ctx.BossX, ctx.BossY, ctx.TargetX, ctx.TargetY,
                pattern1.Aimed3Shot.SpreadAngle, pattern1.Aimed3Shot.BulletCount,
                ctx.Progress, pattern1.Aimed3Shot.TelegraphColor),

            AbilityId.FastSniper => ctx => TelegraphRenderHelper.DrawAimingLasers(
                ctx.BossX, ctx.BossY, ctx.TargetX, ctx.TargetY,
                0.0f, 1, ctx.Progress, pattern1.FastSniper.TelegraphColor, true),

            AbilityId.DiveRam => ctx => TelegraphRenderHelper.DrawDiveRamTelegraph(
                ctx.BossX, ctx.BossY, ctx.TargetX, ctx.TargetY,
                pattern1.DiveRam.ImpactRadius, ctx.Progress, pattern1.DiveRam.TelegraphColor),

            AbilityId.RingBurst => ctx => TelegraphRenderHelper.DrawRadialThreatSpikes(
                ctx.BossX, ctx.BossY, pattern1.RingBurst.BulletCount,
                0.0f, 75.0f, ctx.Progress, pattern1.RingBurst.TelegraphColor),

            AbilityId.DoubleSpiral => ctx => {
                var spiralAngle = ctx.StateTimer * 4.0f;
                TelegraphRenderHelper.DrawRadialThreatSpikes(
                    ctx.BossX, ctx.BossY, 2, spiralAngle, 90.0f, ctx.Progress, pattern1.DoubleSpiral.TelegraphColor);
            },

            AbilityId.RainDrop => ctx => TelegraphRenderHelper.DrawSkyfallColumns(
                pattern1.RainDrop.MinX, pattern1.RainDrop.MaxX, 6, 650.0f, ctx.Progress, pattern1.RainDrop.TelegraphColor, "▼ RAIN ▼"),

            AbilityId.CrossChaos => ctx => {
                var crossAngle = ctx.StateTimer * 2.5f;
                TelegraphRenderHelper.DrawRadialThreatSpikes(
                    ctx.BossX, ctx.BossY, 4, crossAngle, 110.0f, ctx.Progress, pattern1.CrossChaos.TelegraphColor);
            },

            _ => null
        };
    }

    /// <summary>
    ///     技能識別子に対応する UE GAS 冷却グループタグを取得します。
    /// </summary>
    public static GameplayTag ResolveCooldownTags(AbilityId skill)
    {
        return skill switch {
            AbilityId.Aimed3Shot => GameplayTag.CooldownRanged,
            AbilityId.RingBurst => GameplayTag.CooldownRanged,
            AbilityId.DoubleSpiral => GameplayTag.CooldownRanged,
            AbilityId.RainDrop => GameplayTag.CooldownRanged,
            AbilityId.CrossChaos => GameplayTag.CooldownRanged | GameplayTag.CooldownHeavy,
            AbilityId.FastSniper => GameplayTag.CooldownRanged,
            AbilityId.DiveRam => GameplayTag.CooldownHeavy | GameplayTag.CooldownMelee,
            _ => GameplayTag.None
        };
    }
}
