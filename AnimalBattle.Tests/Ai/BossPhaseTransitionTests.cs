using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Ports;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Components;
using ErrorOr;
using Xunit;

namespace AnimalBattle.Tests.Ai;

public class BossPhaseTransitionTests
{
    [Fact]
    public void BossPhaseThresholds_DefaultValues_AreValid()
    {
        BossPhaseThresholds thresholds = BossPhaseThresholds.Default;

        Assert.True(thresholds.IsValid);
        Assert.Equal(0.66f, thresholds.Phase2);
        Assert.Equal(0.33f, thresholds.Phase3);
    }

    [Theory]
    [InlineData(1.0f, Phase.Phase1)]
    [InlineData(0.70f, Phase.Phase1)]
    [InlineData(0.66f, Phase.Phase2)]
    [InlineData(0.50f, Phase.Phase2)]
    [InlineData(0.33f, Phase.Phase3)]
    [InlineData(0.10f, Phase.Phase3)]
    [InlineData(0.0f, Phase.Phase3)]
    public void BossPhaseThresholds_GetPhase_CalculatesCorrectly(float hpRatio, Phase expected)
    {
        BossPhaseThresholds thresholds = BossPhaseThresholds.Default;
        Phase calculated = thresholds.GetPhase(hpRatio);

        Assert.Equal(expected, calculated);
    }

    [Fact]
    public void BossPhaseThresholds_GetNextPhase_ProgressesMonotonically()
    {
        BossPhaseThresholds thresholds = BossPhaseThresholds.Default;

        // 从 Phase1 在 HP 0.5 时应该推进到 Phase2
        bool canAdvance = thresholds.GetNextPhase(Phase.Phase1, 0.5f, out Phase next, out float threshold);
        Assert.True(canAdvance);
        Assert.Equal(Phase.Phase2, next);
        Assert.Equal(0.66f, threshold);

        // 如果已经在 Phase2，HP 还在 0.5，不能推进
        bool cannotAdvance = thresholds.GetNextPhase(Phase.Phase2, 0.5f, out _, out _);
        Assert.False(cannotAdvance);

        // 如果在 Phase2，HP 降至 0.2，应该推进到 Phase3
        bool advanceToP3 = thresholds.GetNextPhase(Phase.Phase2, 0.2f, out Phase nextP3, out float thresholdP3);
        Assert.True(advanceToP3);
        Assert.Equal(Phase.Phase3, nextP3);
        Assert.Equal(0.33f, thresholdP3);

        // 不支持逆向推导（HP 恢复也不允许回退）
        bool cannotRegress = thresholds.GetNextPhase(Phase.Phase3, 0.9f, out _, out _);
        Assert.False(cannotRegress);
    }

    [Fact]
    public void PhaseDeathController_BeginPhaseTransition_ActivatesAndGuardsAgainstDuplicate()
    {
        BossPhaseThresholds thresholds = BossPhaseThresholds.Default;
        BossAiData ai = new()
        {
            BossType = BossType.Boss1,
            Phase = Phase.Phase1,
            BossState = BossState.Boss1_Hover
        };
        BossPhaseTransitionData transition = default;
        TransitionReason reason = new(ReasonKind.PhaseThreshold);

        // 第一次转移：从 Phase1 (HP 500/1000 = 0.5) 转移到 Phase2
        ErrorOr<Success> result1 = PhaseDeathController.BeginPhaseTransition(
            ref ai,
            ref transition,
            in thresholds,
            currentHp: 500.0f,
            maxHp: 1000.0f,
            in reason);

        Assert.False(result1.IsError);
        Assert.True(transition.IsActive);
        Assert.True(ai.IsInvulnerable);
        Assert.Equal(Phase.Phase2, ai.Phase);
        Assert.Equal(BossState.Boss1_PhaseTransition, ai.BossState);

        // 第二次在同个 Phase2 下重复调用必须被拒绝
        ErrorOr<Success> duplicateResult = PhaseDeathController.BeginPhaseTransition(
            ref ai,
            ref transition,
            in thresholds,
            currentHp: 500.0f,
            maxHp: 1000.0f,
            in reason);

        Assert.True(duplicateResult.IsError);
    }

    [Fact]
    public void PhaseDeathController_CreateDeathReason_ReturnsValidReason()
    {
        TransitionReason reason = PhaseDeathController.CreateDeathReason(0.0f);

        Assert.Equal(ReasonKind.Death, reason.Kind);
    }
}
