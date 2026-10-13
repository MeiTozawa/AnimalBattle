using AnimalBattle.Abilities;
using AnimalBattle.Abilities.Attack;
using AnimalBattle.Abilities.Skills;
using AnimalBattle.Configs;
using AnimalBattle.Render;
using ErrorOr;

namespace AnimalBattle.Ai.Boss2;

/// <summary>
///     Boss 2（大地粉碎者）の TOML 設定から検証済みの <see cref="AttackDefinition" /> および <see cref="TimelineAttackAbility" /> を構築するローダーです。
///     UE GAS 準拠：各招式に適切な CooldownTags を自動設定します。
/// </summary>
public static class Boss2PatternLoader {
    /// <summary>
    ///     指定技能の不変攻撃時序定義をビルドします。
    /// </summary>
    public static ErrorOr<AttackDefinition> BuildAttackDefinition(AbilityId skill, Boss2PatternConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return skill switch {
            AbilityId.HeavySlash => SkillConfigLoader.BuildAttackDefinition(
                skill,
                config.HeavySlash.WindUpTime,
                config.HeavySlash.CastDuration,
                config.HeavySlash.RecoveryTime,
                config.HeavySlash.SlashRangeX,
                config.HeavySlash.Events,
                armorTag: GameplayTag.SuperArmor),

            AbilityId.GroundStomp => SkillConfigLoader.BuildAttackDefinition(
                skill,
                config.GroundStomp.WindUpTime,
                config.GroundStomp.CastDuration,
                config.GroundStomp.RecoveryTime,
                config.GroundStomp.ShockwaveSpeed,
                config.GroundStomp.Events,
                armorTag: GameplayTag.SuperArmor),

            AbilityId.LeapSmash => SkillConfigLoader.BuildAttackDefinition(
                skill,
                config.LeapSmash.WindUpTime,
                config.LeapSmash.CastDuration,
                config.LeapSmash.RecoveryTime,
                config.LeapSmash.JumpForce,
                config.LeapSmash.Events,
                armorTag: GameplayTag.Invincible),

            AbilityId.DoubleCleave => SkillConfigLoader.BuildAttackDefinition(
                skill,
                config.DoubleCleave.WindUpTime,
                config.DoubleCleave.CastDuration,
                config.DoubleCleave.RecoveryTime,
                config.DoubleCleave.SlashRangeX,
                config.DoubleCleave.Events,
                armorTag: GameplayTag.SuperArmor),

            AbilityId.FrenzyCharge => SkillConfigLoader.BuildAttackDefinition(
                skill,
                config.FrenzyCharge.WindUpTime,
                config.FrenzyCharge.CastDuration,
                config.FrenzyCharge.RecoveryTime,
                config.FrenzyCharge.ChargeSpeed,
                config.FrenzyCharge.Events,
                armorTag: GameplayTag.SuperArmor),

            AbilityId.EarthquakeRoar => SkillConfigLoader.BuildAttackDefinition(
                skill,
                config.EarthquakeRoar.WindUpTime,
                config.EarthquakeRoar.CastDuration,
                config.EarthquakeRoar.RecoveryTime,
                config.EarthquakeRoar.MaxX,
                config.EarthquakeRoar.Events,
                config.EarthquakeRoar.Interval,
                config.EarthquakeRoar.RockSpeed,
                config.EarthquakeRoar.RockCount > 0 ? config.EarthquakeRoar.RockCount : 1,
                0.0f,
                GameplayTag.SuperArmor),

            _ => Error.NotFound("Skill.Unsupported", $"Boss 2 ではサポートされていない技能です: {skill}")
        };
    }

    /// <summary>
    ///     指定技能の <see cref="TimelineAttackAbility" /> を構築し、UE GAS 冷却タグをバインドします。
    /// </summary>
    public static ErrorOr<TimelineAttackAbility> BuildAbility(
        AbilityId skill,
        Boss2PatternConfig config,
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
    public static Action<AbilityTelegraphContext>? ResolveTelegraphRenderer(AbilityId skill, Boss2PatternConfig pattern2)
    {
        return skill switch {
            AbilityId.GroundStomp => ctx => {
                var b2Cfg = ConfigManager.Boss2;
                var groundY = ctx.BossY + b2Cfg.CollisionHeight / 2.0f;
                TelegraphRenderHelper.DrawGroundShockwaveRails((int)ctx.BossX, groundY, ctx.Progress, pattern2.GroundStomp.TelegraphColor);
            },

            AbilityId.LeapSmash => ctx => TelegraphRenderHelper.DrawLeapSmashTelegraph(
                ctx.TargetX, ctx.TargetY, ctx.Progress, pattern2.LeapSmash.TelegraphColor),

            AbilityId.EarthquakeRoar => ctx => TelegraphRenderHelper.DrawSkyfallColumns(
                pattern2.EarthquakeRoar.MinX, pattern2.EarthquakeRoar.MaxX, 5, 650.0f, ctx.Progress, pattern2.EarthquakeRoar.TelegraphColor, "▼ ROCK ▼"),

            AbilityId.FrenzyCharge => ctx => TelegraphRenderHelper.DrawChargeDangerTrack(
                (int)ctx.BossX, (int)ctx.BossY, ConfigManager.Boss2.CollisionHeight, ctx.FacingRight, ctx.Progress, pattern2.FrenzyCharge.TelegraphColor),

            AbilityId.DoubleCleave => ctx => TelegraphRenderHelper.DrawMeleeDangerBox(
                (int)ctx.BossX, (int)ctx.BossY, pattern2.DoubleCleave.SlashRangeX, pattern2.DoubleCleave.SlashRangeY, ctx.FacingRight, ctx.Progress, pattern2.DoubleCleave.TelegraphColor),

            AbilityId.HeavySlash => ctx => TelegraphRenderHelper.DrawMeleeDangerBox(
                (int)ctx.BossX, (int)ctx.BossY, pattern2.HeavySlash.SlashRangeX, pattern2.HeavySlash.SlashRangeY, ctx.FacingRight, ctx.Progress, pattern2.HeavySlash.TelegraphColor),

            _ => null
        };
    }

    /// <summary>
    ///     技能識別子に対応する UE GAS 冷却グループタグを取得します。
    /// </summary>
    public static GameplayTag ResolveCooldownTags(AbilityId skill)
    {
        return skill switch {
            AbilityId.HeavySlash => GameplayTag.CooldownMelee,
            AbilityId.DoubleCleave => GameplayTag.CooldownMelee,
            AbilityId.GroundStomp => GameplayTag.CooldownMelee,
            AbilityId.LeapSmash => GameplayTag.CooldownHeavy | GameplayTag.CooldownMelee,
            AbilityId.FrenzyCharge => GameplayTag.CooldownHeavy,
            AbilityId.EarthquakeRoar => GameplayTag.CooldownHeavy,
            _ => GameplayTag.None
        };
    }
}
