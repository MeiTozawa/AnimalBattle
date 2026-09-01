using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Dsl;
using AnimalBattle.Configs;

namespace AnimalBattle.Ai.Boss2;

/// <summary>
/// Boss2 (Ground Crusher) の振る舞い・技能・意思決定パイプラインを宣言的に定義する静的定義クラスです。
/// 策划（TOML）が調整した数値を単一の情報源として参照し、マジックナンバーを排除しています。
/// </summary>
public static class Boss2PipelineDefinition
{
    private static readonly BossBehaviorPipeline _pipeline = BuildPipeline();

    /// <summary>構築済みの Boss2 振る舞いパイプラインを取得します。</summary>
    public static BossBehaviorPipeline Pipeline => _pipeline;

    private static BossBehaviorPipeline BuildPipeline()
    {
        PatternConfig pattern = ConfigManager.Pattern;
        Boss2Config boss2 = ConfigManager.Boss2;

        return BossPipeline.Define(BossType.Boss2, boss2.BossName)
            .Phase(Phase.Phase1)
                .Timing(
                    idleDuration: boss2.Phase1.IdleDuration,
                    patrolDuration: 2.0f,
                    transitionDuration: boss2.Phase1.TransitionDuration)
                .Attack(Boss2AttackIds.HeavySlash)
                    .Telegraph(pattern.HeavySlash.WindUpTime, pattern.HeavySlash.TelegraphColor)
                    .Execute(pattern.HeavySlash.CastDuration, range: 160.0f)
                        .Action(atTime: 0.0f, act => act.Dash(360.0f).Slash(offsetX: 80.0f, offsetY: -10.0f))
                    .Recovery(pattern.HeavySlash.RecoveryTime)
                    .Rule(r => r.Cooldown(2.0f).Distance(0.0f, 160.0f).Weight(100.0f).RequireSamePlatform())
                .Attack(Boss2AttackIds.GroundStomp)
                    .Telegraph(pattern.GroundStomp.WindUpTime, pattern.GroundStomp.TelegraphColor)
                    .Execute(pattern.GroundStomp.CastDuration, range: 420.0f)
                        .Action(atTime: 0.0f, act => act.Hitbox(x: 0.0f, y: 35.0f))
                    .Recovery(pattern.GroundStomp.RecoveryTime)
                    .Rule(r => r.Cooldown(5.0f).Distance(50.0f, 350.0f).Weight(70.0f).RequireSamePlatform())
                .Attack(Boss2AttackIds.LeapSmash)
                    .Telegraph(pattern.LeapSmash.WindUpTime, pattern.LeapSmash.TelegraphColor)
                    .Execute(pattern.LeapSmash.CastDuration, range: 580.0f)
                        .Action(atTime: 0.0f, act => act.Dash(0.0f, -680.0f).Hitbox(x: 0.0f, y: 45.0f))
                    .Recovery(pattern.LeapSmash.RecoveryTime)
                    .Rule(r => r.Cooldown(8.0f).Distance(150.0f, 600.0f).Weight(80.0f).PreferAirborneBonus(1.5f))

            .Phase(Phase.Phase2)
                .Timing(
                    idleDuration: boss2.Phase2.IdleDuration,
                    patrolDuration: 1.5f,
                    transitionDuration: boss2.Phase2.TransitionDuration)
                .Attack(Boss2AttackIds.DoubleCleave)
                    .Telegraph(pattern.DoubleCleave.WindUpTime, pattern.DoubleCleave.TelegraphColor)
                    .Execute(pattern.DoubleCleave.CastDuration, range: 175.0f)
                        .Action(atTime: 0.0f, act => act.Dash(420.0f).Slash(offsetX: 80.0f, offsetY: -10.0f))
                        .Action(atTime: 0.30f, act => act.Slash(offsetX: 95.0f, offsetY: -10.0f))
                    .Recovery(pattern.DoubleCleave.RecoveryTime)
                    .Rule(r => r.Cooldown(3.0f).Distance(0.0f, 175.0f).Weight(100.0f).RequireSamePlatform())
                .Attack(Boss2AttackIds.GroundStomp)
                    .Telegraph(pattern.GroundStomp.WindUpTime, pattern.GroundStomp.TelegraphColor)
                    .Execute(pattern.GroundStomp.CastDuration, range: 420.0f)
                        .Action(atTime: 0.0f, act => act.Hitbox(x: 0.0f, y: 35.0f))
                    .Recovery(pattern.GroundStomp.RecoveryTime)
                    .Rule(r => r.Cooldown(4.5f).Distance(50.0f, 400.0f).Weight(70.0f).RequireSamePlatform())
                .Attack(Boss2AttackIds.LeapSmash)
                    .Telegraph(pattern.LeapSmash.WindUpTime, pattern.LeapSmash.TelegraphColor)
                    .Execute(pattern.LeapSmash.CastDuration, range: 580.0f)
                        .Action(atTime: 0.0f, act => act.Dash(0.0f, -680.0f).Hitbox(x: 0.0f, y: 45.0f))
                    .Recovery(pattern.LeapSmash.RecoveryTime)
                    .Rule(r => r.Cooldown(6.0f).Distance(120.0f, 650.0f).Weight(90.0f).PreferAirborneBonus(1.8f))

            .Phase(Phase.Phase3)
                .Timing(
                    idleDuration: boss2.Phase3.IdleDuration,
                    patrolDuration: 1.0f,
                    transitionDuration: boss2.Phase3.TransitionDuration)
                .Attack(Boss2AttackIds.FrenzyCharge)
                    .Telegraph(pattern.FrenzyCharge.WindUpTime, pattern.FrenzyCharge.TelegraphColor)
                    .Execute(pattern.FrenzyCharge.CastDuration, range: 480.0f)
                        .Action(atTime: 0.0f, act => act.Dash(620.0f, 0.0f).Hitbox(x: 0.0f, y: 0.0f))
                    .Recovery(pattern.FrenzyCharge.RecoveryTime)
                    .Rule(r => r.Cooldown(7.0f).Distance(250.0f, 650.0f).Weight(100.0f).RequireSamePlatform())
                .Attack(Boss2AttackIds.EarthquakeRoar)
                    .Telegraph(pattern.EarthquakeRoar.WindUpTime, pattern.EarthquakeRoar.TelegraphColor)
                    .Execute(pattern.EarthquakeRoar.CastDuration, range: 1220.0f)
                        .Action(atTime: 0.0f, act => act.Bullet(x: 0.0f, y: 360.0f))
                    .Recovery(pattern.EarthquakeRoar.RecoveryTime)
                    .Rule(r => r.Cooldown(10.0f).Distance(0.0f, 1000.0f).Weight(80.0f))
                .Attack(Boss2AttackIds.DoubleCleave)
                    .Telegraph(pattern.DoubleCleave.WindUpTime, pattern.DoubleCleave.TelegraphColor)
                    .Execute(pattern.DoubleCleave.CastDuration, range: 175.0f)
                        .Action(atTime: 0.0f, act => act.Dash(420.0f).Slash(offsetX: 80.0f, offsetY: -10.0f))
                        .Action(atTime: 0.30f, act => act.Slash(offsetX: 95.0f, offsetY: -10.0f))
                    .Recovery(pattern.DoubleCleave.RecoveryTime)
                    .Rule(r => r.Cooldown(2.5f).Distance(0.0f, 175.0f).Weight(90.0f).RequireSamePlatform())
                .Attack(Boss2AttackIds.LeapSmash)
                    .Telegraph(pattern.LeapSmash.WindUpTime, pattern.LeapSmash.TelegraphColor)
                    .Execute(pattern.LeapSmash.CastDuration, range: 580.0f)
                        .Action(atTime: 0.0f, act => act.Dash(0.0f, -680.0f).Hitbox(x: 0.0f, y: 45.0f))
                    .Recovery(pattern.LeapSmash.RecoveryTime)
                    .Rule(r => r.Cooldown(5.0f).Distance(100.0f, 700.0f).Weight(95.0f).PreferAirborneBonus(2.0f))
            .Build();
    }
}
