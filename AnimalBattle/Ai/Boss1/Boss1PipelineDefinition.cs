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
    private static readonly BossBehaviorPipeline _pipeline = BuildPipeline();

    /// <summary>構築済みの Boss1 振る舞いパイプラインを取得します。</summary>
    public static BossBehaviorPipeline Pipeline => _pipeline;

    private static BossBehaviorPipeline BuildPipeline()
    {
        PatternConfig pattern = ConfigManager.Pattern;
        Boss1Config boss1 = ConfigManager.Boss1;

        return BossPipeline.Define(BossType.Boss1, boss1.BossName)
            .Phase(Phase.Phase1)
                .Timing(
                    idleDuration: boss1.Phase1.HoverDuration,
                    patrolDuration: 2.0f,
                    transitionDuration: boss1.Phase1.TransitionDuration)
                .Attack(Boss1AttackIds.Aimed3Shot)
                    .Telegraph(pattern.Aimed3Shot.WindUpTime, pattern.Aimed3Shot.TelegraphColor)
                    .Execute(pattern.Aimed3Shot.CastDuration, range: 600.0f)
                    .Recovery(pattern.Aimed3Shot.RecoveryTime)
                    .Rule(r => r.Cooldown(3.0f).Distance(0.0f, 800.0f).Weight(100.0f))
                .Attack(Boss1AttackIds.DiveRam)
                    .Telegraph(pattern.DiveRam.WindUpTime, pattern.DiveRam.TelegraphColor)
                    .Execute(pattern.DiveRam.CastDuration, range: 650.0f)
                        .Action(atTime: 0.0f, act => act.Dash(pattern.DiveRam.DashSpeed).Hitbox(x: 0.0f, y: 0.0f))
                    .Recovery(pattern.DiveRam.RecoveryTime)
                    .Rule(r => r.Cooldown(7.0f).Distance(100.0f, 700.0f).Weight(110.0f))
                .Attack(Boss1AttackIds.RingBurst)
                    .Telegraph(pattern.RingBurst.WindUpTime, pattern.RingBurst.TelegraphColor)
                    .Execute(pattern.RingBurst.CastDuration, range: 700.0f)
                    .Recovery(pattern.RingBurst.RecoveryTime)
                    .Rule(r => r.Cooldown(5.0f).Distance(0.0f, 900.0f).Weight(90.0f))

            .Phase(Phase.Phase2)
                .Timing(
                    idleDuration: boss1.Phase2.HoverDuration,
                    patrolDuration: 1.5f,
                    transitionDuration: boss1.Phase2.TransitionDuration)
                .Attack(Boss1AttackIds.DoubleSpiral)
                    .Telegraph(pattern.DoubleSpiral.WindUpTime, pattern.DoubleSpiral.TelegraphColor)
                    .Execute(pattern.DoubleSpiral.CastDuration, range: 800.0f)
                    .Recovery(pattern.DoubleSpiral.RecoveryTime)
                    .Rule(r => r.Cooldown(4.0f).Distance(0.0f, 850.0f).Weight(95.0f))
                .Attack(Boss1AttackIds.DiveRam)
                    .Telegraph(pattern.DiveRam.WindUpTime, pattern.DiveRam.TelegraphColor)
                    .Execute(pattern.DiveRam.CastDuration, range: 650.0f)
                        .Action(atTime: 0.0f, act => act.Dash(pattern.DiveRam.DashSpeed).Hitbox(x: 0.0f, y: 0.0f))
                    .Recovery(pattern.DiveRam.RecoveryTime)
                    .Rule(r => r.Cooldown(6.0f).Distance(100.0f, 750.0f).Weight(110.0f))
                .Attack(Boss1AttackIds.RainDrop)
                    .Telegraph(pattern.RainDrop.WindUpTime, pattern.RainDrop.TelegraphColor)
                    .Execute(pattern.RainDrop.CastDuration, range: 1000.0f)
                    .Recovery(pattern.RainDrop.RecoveryTime)
                    .Rule(r => r.Cooldown(6.0f).Distance(0.0f, 1000.0f).Weight(85.0f))

            .Phase(Phase.Phase3)
                .Timing(
                    idleDuration: boss1.Phase3.HoverDuration,
                    patrolDuration: 1.0f,
                    transitionDuration: boss1.Phase3.TransitionDuration)
                .Attack(Boss1AttackIds.CrossChaos)
                    .Telegraph(pattern.CrossChaos.WindUpTime, pattern.CrossChaos.TelegraphColor)
                    .Execute(pattern.CrossChaos.CastDuration, range: 900.0f)
                    .Recovery(pattern.CrossChaos.RecoveryTime)
                    .Rule(r => r.Cooldown(4.5f).Distance(0.0f, 900.0f).Weight(90.0f))
                .Attack(Boss1AttackIds.DiveRam)
                    .Telegraph(pattern.DiveRam.WindUpTime, pattern.DiveRam.TelegraphColor)
                    .Execute(pattern.DiveRam.CastDuration, range: 650.0f)
                        .Action(atTime: 0.0f, act => act.Dash(pattern.DiveRam.DashSpeed).Hitbox(x: 0.0f, y: 0.0f))
                    .Recovery(pattern.DiveRam.RecoveryTime)
                    .Rule(r => r.Cooldown(5.0f).Distance(100.0f, 800.0f).Weight(110.0f))
                .Attack(Boss1AttackIds.FastSniper)
                    .Telegraph(pattern.FastSniper.WindUpTime, pattern.FastSniper.TelegraphColor)
                    .Execute(pattern.FastSniper.CastDuration, range: 1200.0f)
                    .Recovery(pattern.FastSniper.RecoveryTime)
                    .Rule(r => r.Cooldown(3.5f).Distance(150.0f, 1200.0f).Weight(100.0f))
            .Build();
    }
}
