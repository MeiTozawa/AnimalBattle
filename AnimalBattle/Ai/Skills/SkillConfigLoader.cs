using System.Diagnostics;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Configs;
using ErrorOr;

namespace AnimalBattle.Ai.Skills;

/// <summary>
///     TOML 設定から検証済みの <see cref="AttackDefinition" /> を構築するデータローダーです。
/// </summary>
public static class SkillConfigLoader {
    /// <summary>
    ///     パラメータ群およびキーフレームイベント一覧から不変時序定義をビルドします。
    /// </summary>
    /// <param name="skill">技能識別子。</param>
    /// <param name="windUpTime">前隙予兆時間。</param>
    /// <param name="castDuration">発動持続時間。</param>
    /// <param name="recoveryTime">後隙硬直時間。</param>
    /// <param name="effectiveRange">有効射程。</param>
    /// <param name="events">TOML キーフレームイベント一覧。</param>
    /// <param name="defaultInterval">周期弾幕の生成間隔。</param>
    /// <param name="defaultSpeed">周期弾幕の弾速。</param>
    /// <param name="defaultCount">周期弾幕の弾数。</param>
    /// <param name="defaultSpread">周期弾幕の拡散角度。</param>
    /// <param name="armor">出招中のアーマー種別。</param>
    /// <param name="poiseThreshold">靭性閾値。</param>
    /// <returns>成功時は <see cref="AttackDefinition" />、検証失敗時は <see cref="Error" />。</returns>
    public static ErrorOr<AttackDefinition> BuildAttackDefinition(
        BossSkill skill,
        float windUpTime,
        float castDuration,
        float recoveryTime,
        float effectiveRange,
        IReadOnlyList<SkillEventConfig>? events,
        float defaultInterval = 0.0f,
        float defaultSpeed = 0.0f,
        int defaultCount = 0,
        float defaultSpread = 0.0f,
        ArmorType armor = ArmorType.None,
        float poiseThreshold = 0.0f)
    {
        // 業務バリデーション
        if (castDuration <= 0.0f || windUpTime <= 0.0f || recoveryTime <= 0.0f)
        {
            return Error.Validation(
                "Skill.InvalidDuration",
                $"技能 '{skill}' の時間設定が不正です。");
        }

        if (effectiveRange < 0.0f)
        {
            return Error.Validation("Skill.InvalidRange", $"技能 '{skill}' の有効射程が負数です: {effectiveRange}");
        }

        var components = new List<AttackComponent>();
        var index = 0;

        if (events != null && events.Count > 0)
        {
            foreach (var ev in events)
            {
                var kind = ResolveComponentKind(ev.Type);
                components.Add(new AttackComponent(kind, index++, ev.Param0, ev.Param1, ev.Param2, ev.Time));
            }
        }
        else if (defaultInterval > 0.0f && defaultSpeed > 0.0f)
        {
            // 旧来パラメータに基づく周期的弾幕自動生成フォールバック
            for (var t = 0.0f; t < castDuration; t += defaultInterval)
            {
                components.Add(new AttackComponent(
                    AttackComponentKind.Bullet,
                    index++,
                    defaultSpeed,
                    defaultCount > 0 ? defaultCount : 1,
                    defaultSpread,
                    t));
            }
        }

        var definition = new AttackDefinition(
            skill,
            windUpTime,
            castDuration,
            recoveryTime,
            effectiveRange,
            components.ToArray(),
            armor,
            poiseThreshold);

        if (!definition.IsValid)
        {
            return Error.Validation("Skill.InvalidDefinition", $"技能 '{skill}' の内部定義検証に失敗しました。");
        }

        return definition;
    }

    /// <summary>
    ///     Boss 1 設定から対象技能の時序定義をビルドします。
    /// </summary>
    public static ErrorOr<AttackDefinition> BuildFromBoss1(BossSkill skill, Boss1PatternConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return skill switch {
            BossSkill.DiveRam => BuildAttackDefinition(
                skill,
                config.DiveRam.WindUpTime,
                config.DiveRam.CastDuration,
                config.DiveRam.RecoveryTime,
                config.DiveRam.EffectiveRange,
                config.DiveRam.Events,
                armor: ArmorType.SuperArmor),

            BossSkill.Aimed3Shot => BuildAttackDefinition(
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

            BossSkill.RingBurst => BuildAttackDefinition(
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

            BossSkill.DoubleSpiral => BuildAttackDefinition(
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

            BossSkill.RainDrop => BuildAttackDefinition(
                skill,
                config.RainDrop.WindUpTime,
                config.RainDrop.CastDuration,
                config.RainDrop.RecoveryTime,
                config.RainDrop.EffectiveRange,
                config.RainDrop.Events,
                config.RainDrop.Interval,
                config.RainDrop.BulletSpeed,
                1,
                0.0f),

            BossSkill.CrossChaos => BuildAttackDefinition(
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

            BossSkill.FastSniper => BuildAttackDefinition(
                skill,
                config.FastSniper.WindUpTime,
                config.FastSniper.CastDuration,
                config.FastSniper.RecoveryTime,
                config.FastSniper.EffectiveRange,
                config.FastSniper.Events,
                config.FastSniper.Interval,
                config.FastSniper.BulletSpeed,
                1,
                0.0f),

            _ => Error.NotFound("Skill.Unsupported", $"Boss 1 ではサポートされていない技能です: {skill}")
        };
    }

    /// <summary>
    ///     Boss 2 設定から対象技能の時序定義をビルドします。
    /// </summary>
    public static ErrorOr<AttackDefinition> BuildFromBoss2(BossSkill skill, Boss2PatternConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return skill switch {
            BossSkill.HeavySlash => BuildAttackDefinition(
                skill,
                config.HeavySlash.WindUpTime,
                config.HeavySlash.CastDuration,
                config.HeavySlash.RecoveryTime,
                config.HeavySlash.SlashRangeX,
                config.HeavySlash.Events,
                armor: ArmorType.SuperArmor),

            BossSkill.GroundStomp => BuildAttackDefinition(
                skill,
                config.GroundStomp.WindUpTime,
                config.GroundStomp.CastDuration,
                config.GroundStomp.RecoveryTime,
                config.GroundStomp.ShockwaveSpeed,
                config.GroundStomp.Events,
                armor: ArmorType.SuperArmor),

            BossSkill.LeapSmash => BuildAttackDefinition(
                skill,
                config.LeapSmash.WindUpTime,
                config.LeapSmash.CastDuration,
                config.LeapSmash.RecoveryTime,
                config.LeapSmash.JumpForce,
                config.LeapSmash.Events,
                armor: ArmorType.Invincible),

            BossSkill.DoubleCleave => BuildAttackDefinition(
                skill,
                config.DoubleCleave.WindUpTime,
                config.DoubleCleave.CastDuration,
                config.DoubleCleave.RecoveryTime,
                config.DoubleCleave.SlashRangeX,
                config.DoubleCleave.Events,
                armor: ArmorType.SuperArmor),

            BossSkill.FrenzyCharge => BuildAttackDefinition(
                skill,
                config.FrenzyCharge.WindUpTime,
                config.FrenzyCharge.CastDuration,
                config.FrenzyCharge.RecoveryTime,
                config.FrenzyCharge.ChargeSpeed,
                config.FrenzyCharge.Events,
                armor: ArmorType.SuperArmor),

            BossSkill.EarthquakeRoar => BuildAttackDefinition(
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
                armor: ArmorType.SuperArmor),

            _ => Error.NotFound("Skill.Unsupported", $"Boss 2 ではサポートされていない技能です: {skill}")
        };
    }

    private static AttackComponentKind ResolveComponentKind(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return FallbackUnknownType("<empty>");

        return typeName.Trim().ToLowerInvariant() switch {
            "bullet" => AttackComponentKind.Bullet,
            "hitbox" => AttackComponentKind.Hitbox,
            "movement" => AttackComponentKind.Movement,
            "easingmovement" => AttackComponentKind.EasingMovement,
            "velocitydive" => AttackComponentKind.VelocityDive,
            "ongroundedhitbox" => AttackComponentKind.OnGroundedHitbox,
            "sound" or "audiocue" => AttackComponentKind.AudioCue,
            "shake" or "screenshake" => AttackComponentKind.ScreenShake,
            _ => FallbackUnknownType(typeName)
        };
    }

    private static AttackComponentKind FallbackUnknownType(string typeName)
    {
        Debug.Assert(false, $"[SkillConfigLoader] 未知のイベント種別です: '{typeName}'。Bullet としてフォールバックします。");
        return AttackComponentKind.Bullet;
    }
}
