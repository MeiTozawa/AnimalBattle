using System.Drawing;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Dsl;

namespace AnimalBattle.Ai.Boss2;

/// <summary>
/// Boss2 (Ground Crusher) の振る舞い・技能・意思決定パイプラインを宣言的に定義する静的定義クラスです。
/// </summary>
public static class Boss2PipelineDefinition
{
    private static readonly BossBehaviorPipeline _pipeline = BuildPipeline();

    /// <summary>構築済みの Boss2 振る舞いパイプラインを取得します。</summary>
    public static BossBehaviorPipeline Pipeline => _pipeline;

    private static BossBehaviorPipeline BuildPipeline()
    {
        return BossPipeline.Define(BossType.Boss2, "GroundCrusher")
            .Phase(Phase.Phase1)
                .Timing(idleDuration: 0.8f, patrolDuration: 2.0f, transitionDuration: 1.0f)
                .Attack(Boss2AttackIds.HeavySlash)
                    .Telegraph(duration: 0.45f, Color.Orange)
                    .Execute(duration: 0.35f, range: 160.0f)
                        .Action(atTime: 0.0f, act => act.Dash(360.0f).Slash(offsetX: 80.0f, offsetY: -10.0f))
                    .Recovery(duration: 0.55f)
                    .Rule(r => r.Cooldown(2.0f).Distance(0.0f, 160.0f).Weight(100.0f).RequireSamePlatform())
                .Attack(Boss2AttackIds.GroundStomp)
                    .Telegraph(duration: 0.50f, Color.Yellow)
                    .Execute(duration: 0.40f, range: 420.0f)
                        .Action(atTime: 0.0f, act => act.Hitbox(x: 0.0f, y: 35.0f))
                    .Recovery(duration: 0.60f)
                    .Rule(r => r.Cooldown(5.0f).Distance(50.0f, 350.0f).Weight(70.0f).RequireSamePlatform())
                .Attack(Boss2AttackIds.LeapSmash)
                    .Telegraph(duration: 0.40f, Color.OrangeRed)
                    .Execute(duration: 0.75f, range: 580.0f)
                        .Action(atTime: 0.0f, act => act.Dash(0.0f, -680.0f).Hitbox(x: 0.0f, y: 45.0f))
                    .Recovery(duration: 0.70f)
                    .Rule(r => r.Cooldown(8.0f).Distance(150.0f, 600.0f).Weight(80.0f).PreferAirborneBonus(1.5f))

            .Phase(Phase.Phase2)
                .Timing(idleDuration: 0.5f, patrolDuration: 1.5f, transitionDuration: 1.0f)
                .Attack(Boss2AttackIds.DoubleCleave)
                    .Telegraph(duration: 0.35f, Color.Red)
                    .Execute(duration: 0.70f, range: 175.0f)
                        .Action(atTime: 0.0f, act => act.Dash(420.0f).Slash(offsetX: 80.0f, offsetY: -10.0f))
                        .Action(atTime: 0.30f, act => act.Slash(offsetX: 95.0f, offsetY: -10.0f))
                    .Recovery(duration: 0.50f)
                    .Rule(r => r.Cooldown(3.0f).Distance(0.0f, 175.0f).Weight(100.0f).RequireSamePlatform())
                .Attack(Boss2AttackIds.GroundStomp)
                    .Telegraph(duration: 0.50f, Color.Yellow)
                    .Execute(duration: 0.40f, range: 420.0f)
                        .Action(atTime: 0.0f, act => act.Hitbox(x: 0.0f, y: 35.0f))
                    .Recovery(duration: 0.60f)
                    .Rule(r => r.Cooldown(4.5f).Distance(50.0f, 400.0f).Weight(70.0f).RequireSamePlatform())
                .Attack(Boss2AttackIds.LeapSmash)
                    .Telegraph(duration: 0.40f, Color.OrangeRed)
                    .Execute(duration: 0.75f, range: 580.0f)
                        .Action(atTime: 0.0f, act => act.Dash(0.0f, -680.0f).Hitbox(x: 0.0f, y: 45.0f))
                    .Recovery(duration: 0.70f)
                    .Rule(r => r.Cooldown(6.0f).Distance(120.0f, 650.0f).Weight(90.0f).PreferAirborneBonus(1.8f))

            .Phase(Phase.Phase3)
                .Timing(idleDuration: 0.3f, patrolDuration: 1.0f, transitionDuration: 1.2f)
                .Attack(Boss2AttackIds.FrenzyCharge)
                    .Telegraph(duration: 0.55f, Color.DarkRed)
                    .Execute(duration: 1.30f, range: 480.0f)
                        .Action(atTime: 0.0f, act => act.Dash(620.0f, 0.0f).Hitbox(x: 0.0f, y: 0.0f))
                    .Recovery(duration: 0.75f)
                    .Rule(r => r.Cooldown(7.0f).Distance(250.0f, 650.0f).Weight(100.0f).RequireSamePlatform())
                .Attack(Boss2AttackIds.EarthquakeRoar)
                    .Telegraph(duration: 0.60f, Color.SaddleBrown)
                    .Execute(duration: 1.80f, range: 1220.0f)
                        .Action(atTime: 0.0f, act => act.Bullet(x: 0.0f, y: 360.0f))
                    .Recovery(duration: 0.90f)
                    .Rule(r => r.Cooldown(10.0f).Distance(0.0f, 1000.0f).Weight(80.0f))
                .Attack(Boss2AttackIds.DoubleCleave)
                    .Telegraph(duration: 0.35f, Color.Red)
                    .Execute(duration: 0.70f, range: 175.0f)
                        .Action(atTime: 0.0f, act => act.Dash(420.0f).Slash(offsetX: 80.0f, offsetY: -10.0f))
                        .Action(atTime: 0.30f, act => act.Slash(offsetX: 95.0f, offsetY: -10.0f))
                    .Recovery(duration: 0.50f)
                    .Rule(r => r.Cooldown(2.5f).Distance(0.0f, 175.0f).Weight(90.0f).RequireSamePlatform())
                .Attack(Boss2AttackIds.LeapSmash)
                    .Telegraph(duration: 0.40f, Color.OrangeRed)
                    .Execute(duration: 0.75f, range: 580.0f)
                        .Action(atTime: 0.0f, act => act.Dash(0.0f, -680.0f).Hitbox(x: 0.0f, y: 45.0f))
                    .Recovery(duration: 0.70f)
                    .Rule(r => r.Cooldown(5.0f).Distance(120.0f, 700.0f).Weight(85.0f).PreferAirborneBonus(2.0f))
            .Build();
    }
}
