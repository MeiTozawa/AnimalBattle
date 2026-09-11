using System;
using System.Collections.Generic;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Dsl;
using AnimalBattle.Configs;

namespace AnimalBattle.Ai.Boss1;

/// <summary>
/// Boss1 (Ancient Cat Overlord) の振る舞い・弾幕技能・意思決定パイプラインを宣言的に定義する静的定義クラスです。
/// 策划（TOML）が調整した数値を単一の情報源として参照し、マジックナンバーを排除しています。
/// </summary>
public static class Boss1PipelineDefinition
{
    private static BossBehaviorPipeline _pipeline = BuildPipeline();

    /// <summary>構築済みの Boss1 振る舞いパイプラインを取得します。</summary>
    public static BossBehaviorPipeline Pipeline => _pipeline;

    /// <summary>TOML 設定の再読み込み時などにパイプラインを再構築します。</summary>
    public static void Reload()
    {
        _pipeline = BuildPipeline();
    }

    private static BossBehaviorPipeline BuildPipeline()
    {
        PatternConfig pattern = ConfigManager.Pattern;
        Boss1Config boss1 = ConfigManager.Boss1;

        return BossPipeline.Define(BossType.Boss1, boss1.BossName)
            .Phase(Phase.Phase1)
                .Timing(
                    idleDuration: boss1.Phase1.HoverDuration,
                    patrolDuration: boss1.Phase1.PatrolDuration,
                    transitionDuration: boss1.Phase1.TransitionDuration)
                .Attack(BossSkill.Aimed3Shot)
                    .Telegraph(pattern.Aimed3Shot.WindUpTime, pattern.Aimed3Shot.TelegraphColor)
                    .Execute(pattern.Aimed3Shot.CastDuration, range: pattern.Aimed3Shot.EffectiveRange)
                    .Recovery(pattern.Aimed3Shot.RecoveryTime)
                    .Rule(boss1.Phase1.Rules)
                .Attack(BossSkill.DiveRam)
                    .Telegraph(pattern.DiveRam.WindUpTime, pattern.DiveRam.TelegraphColor)
                    .Execute(pattern.DiveRam.CastDuration, range: pattern.DiveRam.EffectiveRange)
                        .Action(atTime: 0.0f, act => act.Dash(pattern.DiveRam.DashSpeed).Hitbox(x: 0.0f, y: 0.0f))
                    .Recovery(pattern.DiveRam.RecoveryTime)
                    .Rule(boss1.Phase1.Rules)
                .Attack(BossSkill.RingBurst)
                    .Telegraph(pattern.RingBurst.WindUpTime, pattern.RingBurst.TelegraphColor)
                    .Execute(pattern.RingBurst.CastDuration, range: pattern.RingBurst.EffectiveRange)
                    .Recovery(pattern.RingBurst.RecoveryTime)
                    .Rule(boss1.Phase1.Rules)

            .Phase(Phase.Phase2)
                .Timing(
                    idleDuration: boss1.Phase2.HoverDuration,
                    patrolDuration: boss1.Phase2.PatrolDuration,
                    transitionDuration: boss1.Phase2.TransitionDuration)
                .Attack(BossSkill.DoubleSpiral)
                    .Telegraph(pattern.DoubleSpiral.WindUpTime, pattern.DoubleSpiral.TelegraphColor)
                    .Execute(pattern.DoubleSpiral.CastDuration, range: pattern.DoubleSpiral.EffectiveRange)
                    .Recovery(pattern.DoubleSpiral.RecoveryTime)
                    .Rule(boss1.Phase2.Rules)
                .Attack(BossSkill.DiveRam)
                    .Telegraph(pattern.DiveRam.WindUpTime, pattern.DiveRam.TelegraphColor)
                    .Execute(pattern.DiveRam.CastDuration, range: pattern.DiveRam.EffectiveRange)
                        .Action(atTime: 0.0f, act => act.Dash(pattern.DiveRam.DashSpeed).Hitbox(x: 0.0f, y: 0.0f))
                    .Recovery(pattern.DiveRam.RecoveryTime)
                    .Rule(boss1.Phase2.Rules)
                .Attack(BossSkill.RainDrop)
                    .Telegraph(pattern.RainDrop.WindUpTime, pattern.RainDrop.TelegraphColor)
                    .Execute(pattern.RainDrop.CastDuration, range: pattern.RainDrop.EffectiveRange)
                    .Recovery(pattern.RainDrop.RecoveryTime)
                    .Rule(boss1.Phase2.Rules)

            .Phase(Phase.Phase3)
                .Timing(
                    idleDuration: boss1.Phase3.HoverDuration,
                    patrolDuration: boss1.Phase3.PatrolDuration,
                    transitionDuration: boss1.Phase3.TransitionDuration)
                .Attack(BossSkill.CrossChaos)
                    .Telegraph(pattern.CrossChaos.WindUpTime, pattern.CrossChaos.TelegraphColor)
                    .Execute(pattern.CrossChaos.CastDuration, range: pattern.CrossChaos.EffectiveRange)
                    .Recovery(pattern.CrossChaos.RecoveryTime)
                    .Rule(boss1.Phase3.Rules)
                .Attack(BossSkill.DiveRam)
                    .Telegraph(pattern.DiveRam.WindUpTime, pattern.DiveRam.TelegraphColor)
                    .Execute(pattern.DiveRam.CastDuration, range: pattern.DiveRam.EffectiveRange)
                        .Action(atTime: 0.0f, act => act.Dash(pattern.DiveRam.DashSpeed).Hitbox(x: 0.0f, y: 0.0f))
                    .Recovery(pattern.DiveRam.RecoveryTime)
                    .Rule(boss1.Phase3.Rules)
                .Attack(BossSkill.FastSniper)
                    .Telegraph(pattern.FastSniper.WindUpTime, pattern.FastSniper.TelegraphColor)
                    .Execute(pattern.FastSniper.CastDuration, range: pattern.FastSniper.EffectiveRange)
                    .Recovery(pattern.FastSniper.RecoveryTime)
                    .Rule(boss1.Phase3.Rules)
            .Build();
    }
}
