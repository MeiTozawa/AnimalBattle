using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Dsl;
using AnimalBattle.Configs;

namespace AnimalBattle.Ai.Boss1;

/// <summary>
///     Boss1 (Ancient Cat Overlord) の振る舞い・弾幕技能・意思決定パイプラインを宣言的に定義する静的定義クラスです。
///     策划（TOML）が調整した数値を単一の情報源として参照し、マジックナンバーを排除しています。
/// </summary>
public static class Boss1PipelineDefinition {
    /// <summary>構築済みの Boss1 振る舞いパイプラインを取得します。</summary>
    public static BossBehaviorPipeline Pipeline { get; private set; } = BuildPipeline();

    /// <summary>TOML 設定の再読み込み時などにパイプラインを再構築します。</summary>
    public static void Reload()
    {
        Pipeline = BuildPipeline();
    }

    private static BossBehaviorPipeline BuildPipeline()
    {
        var pattern = ConfigManager.Boss1Patterns;
        var boss1 = ConfigManager.Boss1;

        var builder = BossPipeline.Define(BossType.Boss1, boss1.BossName);

        // Phase 1
        var p1 = builder.Phase(Phase.Phase1)
           .Timing(
                boss1.Phase1.HoverDuration,
                boss1.Phase1.PatrolDuration,
                boss1.Phase1.TransitionDuration);

        var aimed = p1.Attack(BossSkill.Aimed3Shot)
           .Telegraph(pattern.Aimed3Shot.WindUpTime, pattern.Aimed3Shot.TelegraphColor)
           .Execute(pattern.Aimed3Shot.CastDuration, pattern.Aimed3Shot.EffectiveRange);
        var aimedInterval = pattern.Aimed3Shot.Interval > 0 ? pattern.Aimed3Shot.Interval : 0.45f;
        for (var t = 0.0f; t < pattern.Aimed3Shot.CastDuration; t += aimedInterval)
            aimed.Action(t, act => act.Barrage(pattern.Aimed3Shot.BulletSpeed, pattern.Aimed3Shot.BulletCount, pattern.Aimed3Shot.SpreadAngle));
        aimed.Recovery(pattern.Aimed3Shot.RecoveryTime)
           .Rule(boss1.Phase1.Rules);

        p1.Attack(BossSkill.DiveRam)
           .Telegraph(pattern.DiveRam.WindUpTime, pattern.DiveRam.TelegraphColor)
           .Execute(pattern.DiveRam.CastDuration, pattern.DiveRam.EffectiveRange)
           .Action(0.0f, act => act.Dash(pattern.DiveRam.DashSpeed).Hitbox())
           .Recovery(pattern.DiveRam.RecoveryTime)
           .Rule(boss1.Phase1.Rules);

        var ring = p1.Attack(BossSkill.RingBurst)
           .Telegraph(pattern.RingBurst.WindUpTime, pattern.RingBurst.TelegraphColor)
           .Execute(pattern.RingBurst.CastDuration, pattern.RingBurst.EffectiveRange);
        var ringInterval = pattern.RingBurst.Interval > 0 ? pattern.RingBurst.Interval : 0.50f;
        for (var t = 0.0f; t < pattern.RingBurst.CastDuration; t += ringInterval)
            ring.Action(t, act => act.Barrage(pattern.RingBurst.BulletSpeed, pattern.RingBurst.BulletCount));
        ring.Recovery(pattern.RingBurst.RecoveryTime)
           .Rule(boss1.Phase1.Rules);

        // Phase 2
        var p2 = builder.Phase(Phase.Phase2)
           .Timing(
                boss1.Phase2.HoverDuration,
                boss1.Phase2.PatrolDuration,
                boss1.Phase2.TransitionDuration);

        var spiral = p2.Attack(BossSkill.DoubleSpiral)
           .Telegraph(pattern.DoubleSpiral.WindUpTime, pattern.DoubleSpiral.TelegraphColor)
           .Execute(pattern.DoubleSpiral.CastDuration, pattern.DoubleSpiral.EffectiveRange);
        var spiralInterval = pattern.DoubleSpiral.Interval > 0 ? pattern.DoubleSpiral.Interval : 0.075f;
        for (var t = 0.0f; t < pattern.DoubleSpiral.CastDuration; t += spiralInterval)
            spiral.Action(t, act => act.Barrage(pattern.DoubleSpiral.BulletSpeed, 2.0f, pattern.DoubleSpiral.AngleStep));
        spiral.Recovery(pattern.DoubleSpiral.RecoveryTime)
           .Rule(boss1.Phase2.Rules);

        p2.Attack(BossSkill.DiveRam)
           .Telegraph(pattern.DiveRam.WindUpTime, pattern.DiveRam.TelegraphColor)
           .Execute(pattern.DiveRam.CastDuration, pattern.DiveRam.EffectiveRange)
           .Action(0.0f, act => act.Dash(pattern.DiveRam.DashSpeed).Hitbox())
           .Recovery(pattern.DiveRam.RecoveryTime)
           .Rule(boss1.Phase2.Rules);

        var rain = p2.Attack(BossSkill.RainDrop)
           .Telegraph(pattern.RainDrop.WindUpTime, pattern.RainDrop.TelegraphColor)
           .Execute(pattern.RainDrop.CastDuration, pattern.RainDrop.EffectiveRange);
        var rainInterval = pattern.RainDrop.Interval > 0 ? pattern.RainDrop.Interval : 0.32f;
        for (var t = 0.0f; t < pattern.RainDrop.CastDuration; t += rainInterval)
            rain.Action(t, act => act.Barrage(pattern.RainDrop.BulletSpeed, 2.0f, 60.0f));
        rain.Recovery(pattern.RainDrop.RecoveryTime)
           .Rule(boss1.Phase2.Rules);

        // Phase 3
        var p3 = builder.Phase(Phase.Phase3)
           .Timing(
                boss1.Phase3.HoverDuration,
                boss1.Phase3.PatrolDuration,
                boss1.Phase3.TransitionDuration);

        var cross = p3.Attack(BossSkill.CrossChaos)
           .Telegraph(pattern.CrossChaos.WindUpTime, pattern.CrossChaos.TelegraphColor)
           .Execute(pattern.CrossChaos.CastDuration, pattern.CrossChaos.EffectiveRange);
        var crossInterval = pattern.CrossChaos.Interval > 0 ? pattern.CrossChaos.Interval : 0.10f;
        for (var t = 0.0f; t < pattern.CrossChaos.CastDuration; t += crossInterval)
            cross.Action(t, act => act.Barrage(pattern.CrossChaos.BulletSpeed, 4.0f, pattern.CrossChaos.AngleStep));
        cross.Recovery(pattern.CrossChaos.RecoveryTime)
           .Rule(boss1.Phase3.Rules);

        p3.Attack(BossSkill.DiveRam)
           .Telegraph(pattern.DiveRam.WindUpTime, pattern.DiveRam.TelegraphColor)
           .Execute(pattern.DiveRam.CastDuration, pattern.DiveRam.EffectiveRange)
           .Action(0.0f, act => act.Dash(pattern.DiveRam.DashSpeed).Hitbox())
           .Recovery(pattern.DiveRam.RecoveryTime)
           .Rule(boss1.Phase3.Rules);

        var sniper = p3.Attack(BossSkill.FastSniper)
           .Telegraph(pattern.FastSniper.WindUpTime, pattern.FastSniper.TelegraphColor)
           .Execute(pattern.FastSniper.CastDuration, pattern.FastSniper.EffectiveRange);
        var sniperInterval = pattern.FastSniper.Interval > 0 ? pattern.FastSniper.Interval : 0.30f;
        for (var t = 0.0f; t < pattern.FastSniper.CastDuration; t += sniperInterval)
            sniper.Action(t, act => act.Barrage(pattern.FastSniper.BulletSpeed, 1.0f));
        sniper.Recovery(pattern.FastSniper.RecoveryTime)
           .Rule(boss1.Phase3.Rules);

        return builder.Build();
    }
}