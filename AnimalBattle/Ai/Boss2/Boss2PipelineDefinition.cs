using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Dsl;
using AnimalBattle.Configs;

namespace AnimalBattle.Ai.Boss2;

/// <summary>
///     Boss2 (Ground Crusher) の振る舞い・技能・意思決定パイプラインを宣言的に定義する静的定義クラスです。
///     策划（TOML）が調整した数値を単一の情報源として参照し、すべてのマジックナンバーを排除しています。
/// </summary>
public static class Boss2PipelineDefinition {
    /// <summary>構築済みの Boss2 振る舞いパイプラインを取得します。</summary>
    public static BossBehaviorPipeline Pipeline { get; private set; } = BuildPipeline();

    /// <summary>TOML 設定の再読み込み時などにパイプラインを再構築します。</summary>
    public static void Reload()
    {
        Pipeline = BuildPipeline();
    }

    private static BossBehaviorPipeline BuildPipeline()
    {
        var pattern = ConfigManager.Pattern;
        var boss2 = ConfigManager.Boss2;

        var builder = BossPipeline.Define(BossType.Boss2, boss2.BossName);

        // Phase 1
        var p1 = builder.Phase(Phase.Phase1)
           .Timing(
                boss2.Phase1.IdleDuration,
                boss2.Phase1.PatrolDuration,
                boss2.Phase1.TransitionDuration)
           .PlatformJump(
                boss2.Phase1.JumpForce,
                boss2.Gravity,
                boss2.MaxFallSpeed,
                boss2.MaxAscentDuration,
                boss2.LandingRecoveryDuration,
                boss2.TakeoffDistance);

        p1.Attack(BossSkill.HeavySlash)
           .Telegraph(pattern.HeavySlash.WindUpTime, pattern.HeavySlash.TelegraphColor)
           .Execute(pattern.HeavySlash.CastDuration, pattern.HeavySlash.SlashRangeX)
           .Action(0.0f,
                act => act.Dash(pattern.HeavySlash.DashSpeed).Slash(boss2.SlashSpawnOffsetX, boss2.SlashSpawnOffsetY))
           .Recovery(pattern.HeavySlash.RecoveryTime)
           .Rule(boss2.Phase1.Rules);

        p1.Attack(BossSkill.GroundStomp)
           .Telegraph(pattern.GroundStomp.WindUpTime, pattern.GroundStomp.TelegraphColor)
           .Execute(pattern.GroundStomp.CastDuration, pattern.GroundStomp.ShockwaveSpeed)
           .Action(0.0f, act => act.Hitbox(0.0f, pattern.GroundStomp.HitboxOffsetY))
           .Recovery(pattern.GroundStomp.RecoveryTime)
           .Rule(boss2.Phase1.Rules);

        ConfigureLeapSmash(p1.Attack(BossSkill.LeapSmash), pattern, boss2.Phase1.Rules);
        ConfigureEarthquakeRoar(p1.Attack(BossSkill.EarthquakeRoar), pattern, boss2.Phase1.Rules);

        // Phase 2
        var p2 = builder.Phase(Phase.Phase2)
           .Timing(
                boss2.Phase2.IdleDuration,
                boss2.Phase2.PatrolDuration,
                boss2.Phase2.TransitionDuration)
           .PlatformJump(
                boss2.Phase2.JumpForce,
                boss2.Gravity,
                boss2.MaxFallSpeed,
                boss2.MaxAscentDuration,
                boss2.LandingRecoveryDuration,
                boss2.TakeoffDistance);

        p2.Attack(BossSkill.DoubleCleave)
           .Telegraph(pattern.DoubleCleave.WindUpTime, pattern.DoubleCleave.TelegraphColor)
           .Execute(pattern.DoubleCleave.CastDuration, pattern.DoubleCleave.SlashRangeX)
           .Action(0.0f,
                act => act.Dash(pattern.DoubleCleave.DashSpeed).Slash(boss2.SlashSpawnOffsetX, boss2.SlashSpawnOffsetY))
           .Action(pattern.DoubleCleave.SlashInterval,
                act => act.Slash(boss2.SlashSpawnOffsetX + pattern.DoubleCleave.SecondSlashOffsetX,
                    boss2.SlashSpawnOffsetY))
           .Recovery(pattern.DoubleCleave.RecoveryTime)
           .Rule(boss2.Phase2.Rules);

        p2.Attack(BossSkill.GroundStomp)
           .Telegraph(pattern.GroundStomp.WindUpTime, pattern.GroundStomp.TelegraphColor)
           .Execute(pattern.GroundStomp.CastDuration, pattern.GroundStomp.ShockwaveSpeed)
           .Action(0.0f, act => act.Hitbox(0.0f, pattern.GroundStomp.HitboxOffsetY))
           .Recovery(pattern.GroundStomp.RecoveryTime)
           .Rule(boss2.Phase2.Rules);

        ConfigureLeapSmash(p2.Attack(BossSkill.LeapSmash), pattern, boss2.Phase2.Rules);
        ConfigureEarthquakeRoar(p2.Attack(BossSkill.EarthquakeRoar), pattern, boss2.Phase2.Rules);

        // Phase 3
        var p3 = builder.Phase(Phase.Phase3)
           .Timing(
                boss2.Phase3.IdleDuration,
                boss2.Phase3.PatrolDuration,
                boss2.Phase3.TransitionDuration)
           .PlatformJump(
                boss2.Phase3.JumpForce,
                boss2.Gravity,
                boss2.MaxFallSpeed,
                boss2.MaxAscentDuration,
                boss2.LandingRecoveryDuration,
                boss2.TakeoffDistance);

        var frenzy = p3.Attack(BossSkill.FrenzyCharge)
           .Telegraph(pattern.FrenzyCharge.WindUpTime, pattern.FrenzyCharge.TelegraphColor)
           .Execute(pattern.FrenzyCharge.CastDuration, pattern.FrenzyCharge.ChargeSpeed)
           .Action(0.0f, act => act.Dash(pattern.FrenzyCharge.ChargeSpeed).Hitbox());
        for (var t = 0.25f; t < pattern.FrenzyCharge.CastDuration; t += 0.25f) frenzy.Action(t, act => act.Hitbox());
        frenzy.Recovery(pattern.FrenzyCharge.RecoveryTime)
           .Rule(boss2.Phase3.Rules);

        ConfigureEarthquakeRoar(p3.Attack(BossSkill.EarthquakeRoar), pattern, boss2.Phase3.Rules);

        p3.Attack(BossSkill.DoubleCleave)
           .Telegraph(pattern.DoubleCleave.WindUpTime, pattern.DoubleCleave.TelegraphColor)
           .Execute(pattern.DoubleCleave.CastDuration, pattern.DoubleCleave.SlashRangeX)
           .Action(0.0f,
                act => act.Dash(pattern.DoubleCleave.DashSpeed).Slash(boss2.SlashSpawnOffsetX, boss2.SlashSpawnOffsetY))
           .Action(pattern.DoubleCleave.SlashInterval,
                act => act.Slash(boss2.SlashSpawnOffsetX + pattern.DoubleCleave.SecondSlashOffsetX,
                    boss2.SlashSpawnOffsetY))
           .Recovery(pattern.DoubleCleave.RecoveryTime)
           .Rule(boss2.Phase3.Rules);

        ConfigureLeapSmash(p3.Attack(BossSkill.LeapSmash), pattern, boss2.Phase3.Rules);

        return builder.Build();
    }

    private static void ConfigureEarthquakeRoar(AttackPipelineBuilder builder, PatternConfig pattern,
        List<AttackRuleConfig>? rules)
    {
        builder.Telegraph(pattern.EarthquakeRoar.WindUpTime, pattern.EarthquakeRoar.TelegraphColor)
           .Execute(pattern.EarthquakeRoar.CastDuration, pattern.EarthquakeRoar.MaxX);
        var roarInterval = pattern.EarthquakeRoar.Interval;
        for (var t = 0.0f; t < pattern.EarthquakeRoar.CastDuration; t += roarInterval)
            builder.Action(t, act => act.Bullet(0.0f, pattern.EarthquakeRoar.RockSpeed));
        builder.Recovery(pattern.EarthquakeRoar.RecoveryTime)
           .Rule(rules);
    }

    private static void ConfigureLeapSmash(AttackPipelineBuilder builder, PatternConfig pattern,
        List<AttackRuleConfig>? rules)
    {
        var flyDuration = pattern.LeapSmash.FlyDuration;
        var peakY = pattern.LeapSmash.PeakAltitudeY;
        var hangDuration = pattern.LeapSmash.ApexHangDuration;
        var descentSpeed = pattern.LeapSmash.DescentSpeed;
        var diveStartTime = flyDuration + hangDuration;
        var maxExecuteDuration = MathF.Max(pattern.LeapSmash.CastDuration, diveStartTime + 1.5f);

        builder.Telegraph(pattern.LeapSmash.WindUpTime, pattern.LeapSmash.TelegraphColor)
           .Execute(maxExecuteDuration, pattern.LeapSmash.JumpForce)
           .Action(0.0f, act => act.FlyToTarget(peakY, flyDuration))
           .Action(diveStartTime, act => act.Dive(descentSpeed))
           .OnGrounded(act => act.Hitbox(0.0f, 45.0f))
           .Recovery(pattern.LeapSmash.RecoveryTime)
           .Rule(rules);
    }
}