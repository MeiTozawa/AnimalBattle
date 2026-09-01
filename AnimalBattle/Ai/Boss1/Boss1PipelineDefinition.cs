using System.Drawing;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Dsl;

namespace AnimalBattle.Ai.Boss1;

/// <summary>
/// Boss1 (Ancient Cat Overlord) の振る舞い・弾幕技能・意思決定パイプラインを宣言的に定義する静的定義クラスです。
/// </summary>
public static class Boss1PipelineDefinition
{
    private static readonly BossBehaviorPipeline _pipeline = BuildPipeline();

    /// <summary>構築済みの Boss1 振る舞いパイプラインを取得します。</summary>
    public static BossBehaviorPipeline Pipeline => _pipeline;

    private static BossBehaviorPipeline BuildPipeline()
    {
        return BossPipeline.Define(BossType.Boss1, "AncientCatOverlord")
            .Phase(Phase.Phase1)
                .Timing(idleDuration: 0.8f, patrolDuration: 2.0f, transitionDuration: 1.0f)
                .Attack(Boss1AttackIds.Aimed3Shot)
                    .Telegraph(duration: 0.40f, Color.Cyan)
                    .Execute(duration: 0.90f, range: 600.0f)
                        .Action(atTime: 0.0f, act => act.Bullet(0.0f, 0.0f))
                    .Recovery(duration: 0.50f)
                    .Rule(r => r.Cooldown(3.0f).Distance(0.0f, 800.0f).Weight(100.0f))
                .Attack(Boss1AttackIds.DiveRam)
                    .Telegraph(duration: 0.55f, Color.Crimson)
                    .Execute(duration: 0.45f, range: 650.0f)
                        .Action(atTime: 0.0f, act => act.Dash(960.0f).Hitbox(x: 0.0f, y: 0.0f))
                    .Recovery(duration: 0.65f)
                    .Rule(r => r.Cooldown(7.0f).Distance(100.0f, 700.0f).Weight(110.0f))
                .Attack(Boss1AttackIds.RingBurst)
                    .Telegraph(duration: 0.55f, Color.Blue)
                    .Execute(duration: 1.00f, range: 700.0f)
                        .Action(atTime: 0.0f, act => act.Bullet(0.0f, 0.0f))
                    .Recovery(duration: 0.70f)
                    .Rule(r => r.Cooldown(5.0f).Distance(0.0f, 900.0f).Weight(90.0f))

            .Phase(Phase.Phase2)
                .Timing(idleDuration: 0.6f, patrolDuration: 1.5f, transitionDuration: 1.0f)
                .Attack(Boss1AttackIds.DoubleSpiral)
                    .Telegraph(duration: 0.45f, Color.MediumPurple)
                    .Execute(duration: 1.60f, range: 800.0f)
                        .Action(atTime: 0.0f, act => act.Bullet(0.0f, 0.0f))
                    .Recovery(duration: 0.60f)
                    .Rule(r => r.Cooldown(4.0f).Distance(0.0f, 850.0f).Weight(95.0f))
                .Attack(Boss1AttackIds.DiveRam)
                    .Telegraph(duration: 0.55f, Color.Crimson)
                    .Execute(duration: 0.45f, range: 650.0f)
                        .Action(atTime: 0.0f, act => act.Dash(960.0f).Hitbox(x: 0.0f, y: 0.0f))
                    .Recovery(duration: 0.65f)
                    .Rule(r => r.Cooldown(6.0f).Distance(100.0f, 750.0f).Weight(110.0f))
                .Attack(Boss1AttackIds.RainDrop)
                    .Telegraph(duration: 0.35f, Color.DeepSkyBlue)
                    .Execute(duration: 1.80f, range: 1000.0f)
                        .Action(atTime: 0.0f, act => act.Bullet(0.0f, 0.0f))
                    .Recovery(duration: 0.80f)
                    .Rule(r => r.Cooldown(6.0f).Distance(0.0f, 1000.0f).Weight(85.0f))

            .Phase(Phase.Phase3)
                .Timing(idleDuration: 0.4f, patrolDuration: 1.0f, transitionDuration: 1.0f)
                .Attack(Boss1AttackIds.CrossChaos)
                    .Telegraph(duration: 0.35f, Color.Magenta)
                    .Execute(duration: 2.00f, range: 900.0f)
                        .Action(atTime: 0.0f, act => act.Bullet(0.0f, 0.0f))
                    .Recovery(duration: 0.50f)
                    .Rule(r => r.Cooldown(4.5f).Distance(0.0f, 900.0f).Weight(90.0f))
                .Attack(Boss1AttackIds.DiveRam)
                    .Telegraph(duration: 0.55f, Color.Crimson)
                    .Execute(duration: 0.45f, range: 650.0f)
                        .Action(atTime: 0.0f, act => act.Dash(960.0f).Hitbox(x: 0.0f, y: 0.0f))
                    .Recovery(duration: 0.65f)
                    .Rule(r => r.Cooldown(5.0f).Distance(100.0f, 800.0f).Weight(110.0f))
                .Attack(Boss1AttackIds.FastSniper)
                    .Telegraph(duration: 0.50f, Color.Red)
                    .Execute(duration: 0.60f, range: 1200.0f)
                        .Action(atTime: 0.0f, act => act.Bullet(0.0f, 0.0f))
                    .Recovery(duration: 0.60f)
                    .Rule(r => r.Cooldown(3.5f).Distance(150.0f, 1200.0f).Weight(100.0f))
            .Build();
    }
}
