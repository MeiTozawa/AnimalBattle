using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Boss2;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Core;
using Xunit;

namespace AnimalBattle.Tests.Core;

public class EnumStringLutTests
{
    [Fact]
    public void EnumStringLut_ReturnsPrebakedStrings_WithoutAllocation()
    {
        Assert.Equal("DiveRam", EnumStringLut.Get(BossSkill.DiveRam));
        Assert.Equal("RingBurst", EnumStringLut.Get(BossSkill.RingBurst));
        Assert.Equal("Hover", EnumStringLut.Get(Boss1State.Hover));
        Assert.Equal("PatrolChase", EnumStringLut.Get(Boss2State.PatrolChase));
        Assert.Equal("Phase 1", EnumStringLut.Get(Phase.Phase1));
    }
}
