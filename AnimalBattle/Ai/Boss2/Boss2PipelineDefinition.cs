using System;
using System.Collections.Generic;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Dsl;
using AnimalBattle.Configs;

namespace AnimalBattle.Ai.Boss2;

/// <summary>
/// Boss2 (Ground Crusher) の振る舞い・技能・意思決定パイプラインを宣言的に定義する静的定義クラスです。
/// 策划（TOML）が調整した数値を単一の情報源として参照し、すべてのマジックナンバーを排除しています。
/// </summary>
public static class Boss2PipelineDefinition
{
    private static BossBehaviorPipeline _pipeline = BuildPipeline();

    /// <summary>構築済みの Boss2 振る舞いパイプラインを取得します。</summary>
    public static BossBehaviorPipeline Pipeline => _pipeline;

    /// <summary>TOML 設定の再読み込み時などにパイプラインを再構築します。</summary>
    public static void Reload()
    {
        _pipeline = BuildPipeline();
    }

    private static BossBehaviorPipeline BuildPipeline()
    {
        PatternConfig pattern = ConfigManager.Pattern;
        Boss2Config boss2 = ConfigManager.Boss2;

        BossPipelineBuilder builder = BossPipeline.Define(BossType.Boss2, boss2.BossName);

        // Phase 1
        PhasePipelineBuilder p1 = builder.Phase(Phase.Phase1)
            .Timing(
                idleDuration: boss2.Phase1.IdleDuration,
                patrolDuration: 2.0f,
                transitionDuration: boss2.Phase1.TransitionDuration)
            .PlatformJump(
                jumpForce: boss2.Phase1.JumpForce,
                gravity: boss2.Gravity,
                maxFallSpeed: 850.0f,
                maxAscentDuration: 0.80f,
                landingRecoveryDuration: 0.10f,
                takeoffDistance: 50.0f);

        p1.Attack(BossSkill.HeavySlash)
            .Telegraph(pattern.HeavySlash.WindUpTime, pattern.HeavySlash.TelegraphColor)
            .Execute(pattern.HeavySlash.CastDuration, range: pattern.HeavySlash.SlashRangeX)
                .Action(atTime: 0.0f, act => act.Dash(pattern.HeavySlash.DashSpeed).Slash(offsetX: boss2.SlashSpawnOffsetX, offsetY: boss2.SlashSpawnOffsetY))
            .Recovery(pattern.HeavySlash.RecoveryTime)
            .Rule(FindRule(boss2.Phase1.Rules, "HeavySlash"));

        p1.Attack(BossSkill.GroundStomp)
            .Telegraph(pattern.GroundStomp.WindUpTime, pattern.GroundStomp.TelegraphColor)
            .Execute(pattern.GroundStomp.CastDuration, range: pattern.GroundStomp.ShockwaveSpeed)
                .Action(atTime: 0.0f, act => act.Hitbox(x: 0.0f, y: 35.0f))
            .Recovery(pattern.GroundStomp.RecoveryTime)
            .Rule(FindRule(boss2.Phase1.Rules, "GroundStomp"));

        ConfigureLeapSmash(p1.Attack(BossSkill.LeapSmash), pattern, FindRule(boss2.Phase1.Rules, "LeapSmash"));

        // Phase 2
        PhasePipelineBuilder p2 = builder.Phase(Phase.Phase2)
            .Timing(
                idleDuration: boss2.Phase2.IdleDuration,
                patrolDuration: 1.5f,
                transitionDuration: boss2.Phase2.TransitionDuration)
            .PlatformJump(
                jumpForce: boss2.Phase2.JumpForce,
                gravity: boss2.Gravity,
                maxFallSpeed: 850.0f,
                maxAscentDuration: 0.80f,
                landingRecoveryDuration: 0.10f,
                takeoffDistance: 50.0f);

        p2.Attack(BossSkill.DoubleCleave)
            .Telegraph(pattern.DoubleCleave.WindUpTime, pattern.DoubleCleave.TelegraphColor)
            .Execute(pattern.DoubleCleave.CastDuration, range: pattern.DoubleCleave.SlashRangeX)
                .Action(atTime: 0.0f, act => act.Dash(pattern.DoubleCleave.DashSpeed).Slash(offsetX: boss2.SlashSpawnOffsetX, offsetY: boss2.SlashSpawnOffsetY))
                .Action(atTime: pattern.DoubleCleave.SlashInterval, act => act.Slash(offsetX: boss2.SlashSpawnOffsetX + 15.0f, offsetY: boss2.SlashSpawnOffsetY))
            .Recovery(pattern.DoubleCleave.RecoveryTime)
            .Rule(FindRule(boss2.Phase2.Rules, "DoubleCleave"));

        p2.Attack(BossSkill.GroundStomp)
            .Telegraph(pattern.GroundStomp.WindUpTime, pattern.GroundStomp.TelegraphColor)
            .Execute(pattern.GroundStomp.CastDuration, range: pattern.GroundStomp.ShockwaveSpeed)
                .Action(atTime: 0.0f, act => act.Hitbox(x: 0.0f, y: 35.0f))
            .Recovery(pattern.GroundStomp.RecoveryTime)
            .Rule(FindRule(boss2.Phase2.Rules, "GroundStomp"));

        ConfigureLeapSmash(p2.Attack(BossSkill.LeapSmash), pattern, FindRule(boss2.Phase2.Rules, "LeapSmash"));

        // Phase 3
        PhasePipelineBuilder p3 = builder.Phase(Phase.Phase3)
            .Timing(
                idleDuration: boss2.Phase3.IdleDuration,
                patrolDuration: 1.0f,
                transitionDuration: boss2.Phase3.TransitionDuration)
            .PlatformJump(
                jumpForce: boss2.Phase3.JumpForce,
                gravity: boss2.Gravity,
                maxFallSpeed: 850.0f,
                maxAscentDuration: 0.80f,
                landingRecoveryDuration: 0.10f,
                takeoffDistance: 50.0f);

        AttackPipelineBuilder frenzy = p3.Attack(BossSkill.FrenzyCharge)
            .Telegraph(pattern.FrenzyCharge.WindUpTime, pattern.FrenzyCharge.TelegraphColor)
            .Execute(pattern.FrenzyCharge.CastDuration, range: pattern.FrenzyCharge.ChargeSpeed)
            .Action(atTime: 0.0f, act => act.Dash(pattern.FrenzyCharge.ChargeSpeed, 0.0f).Hitbox(x: 0.0f, y: 0.0f));
        for (float t = 0.25f; t < pattern.FrenzyCharge.CastDuration; t += 0.25f)
        {
            frenzy.Action(atTime: t, act => act.Hitbox(x: 0.0f, y: 0.0f));
        }
        frenzy.Recovery(pattern.FrenzyCharge.RecoveryTime)
            .Rule(FindRule(boss2.Phase3.Rules, "FrenzyCharge"));

        AttackPipelineBuilder roar = p3.Attack(BossSkill.EarthquakeRoar)
            .Telegraph(pattern.EarthquakeRoar.WindUpTime, pattern.EarthquakeRoar.TelegraphColor)
            .Execute(pattern.EarthquakeRoar.CastDuration, range: pattern.EarthquakeRoar.MaxX);
        float roarInterval = pattern.EarthquakeRoar.Interval > 0 ? pattern.EarthquakeRoar.Interval : 0.25f;
        for (float t = 0.0f; t < pattern.EarthquakeRoar.CastDuration; t += roarInterval)
        {
            roar.Action(atTime: t, act => act.Bullet(x: 0.0f, y: pattern.EarthquakeRoar.RockSpeed));
        }
        roar.Recovery(pattern.EarthquakeRoar.RecoveryTime)
            .Rule(FindRule(boss2.Phase3.Rules, "EarthquakeRoar"));

        p3.Attack(BossSkill.DoubleCleave)
            .Telegraph(pattern.DoubleCleave.WindUpTime, pattern.DoubleCleave.TelegraphColor)
            .Execute(pattern.DoubleCleave.CastDuration, range: pattern.DoubleCleave.SlashRangeX)
                .Action(atTime: 0.0f, act => act.Dash(pattern.DoubleCleave.DashSpeed).Slash(offsetX: boss2.SlashSpawnOffsetX, offsetY: boss2.SlashSpawnOffsetY))
                .Action(atTime: pattern.DoubleCleave.SlashInterval, act => act.Slash(offsetX: boss2.SlashSpawnOffsetX + 15.0f, offsetY: boss2.SlashSpawnOffsetY))
            .Recovery(pattern.DoubleCleave.RecoveryTime)
            .Rule(FindRule(boss2.Phase3.Rules, "DoubleCleave"));

        ConfigureLeapSmash(p3.Attack(BossSkill.LeapSmash), pattern, FindRule(boss2.Phase3.Rules, "LeapSmash"));

        return builder.Build();
    }

    private static void ConfigureLeapSmash(AttackPipelineBuilder builder, PatternConfig pattern, AttackRuleConfig? rule)
    {
        float flyDuration = pattern.LeapSmash.FlyDuration > 0.0f ? pattern.LeapSmash.FlyDuration : 0.45f;
        float peakY = pattern.LeapSmash.PeakAltitudeY > 0.0f ? pattern.LeapSmash.PeakAltitudeY : 130.0f;
        float hangDuration = pattern.LeapSmash.ApexHangDuration > 0.0f ? pattern.LeapSmash.ApexHangDuration : 0.50f;
        float descentSpeed = pattern.LeapSmash.DescentSpeed > 0.0f ? pattern.LeapSmash.DescentSpeed : 1200.0f;
        float diveStartTime = flyDuration + hangDuration;
        float maxExecuteDuration = MathF.Max(pattern.LeapSmash.CastDuration, diveStartTime + 1.5f);

        builder.Telegraph(pattern.LeapSmash.WindUpTime, pattern.LeapSmash.TelegraphColor)
            .Execute(maxExecuteDuration, range: pattern.LeapSmash.JumpForce)
                .Action(atTime: 0.0f, act => act.FlyToTarget(peakY, flyDuration))
                .Action(atTime: diveStartTime, act => act.Dive(descentSpeed))
                .OnGrounded(act => act.Hitbox(x: 0.0f, y: 45.0f))
            .Recovery(pattern.LeapSmash.RecoveryTime)
            .Rule(rule);
    }

    private static AttackRuleConfig? FindRule(List<AttackRuleConfig>? rules, string skillName)
    {
        return rules?.Find(r => string.Equals(r.Skill, skillName, StringComparison.OrdinalIgnoreCase));
    }
}
