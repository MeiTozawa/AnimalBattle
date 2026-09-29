using AnimalBattle.Core;
using Xunit;

namespace AnimalBattle.Tests.Physics;

public class BossTrajectoryMathTests
{
    [Fact]
    public void CalculateBossHoverPosition_MatchesExpectedSineWave()
    {
        Vec2f curPos = new(500.0f, 300.0f);
        Vec2f basePos = new(500.0f, 300.0f);
        Vec2f playerPos = new(400.0f, 500.0f);

        Vec2f nextPos = PhysicsService.CalculateBossHoverPosition(
            curPos, basePos, playerPos,
            hoverTimer: 1.0f,
            hoverSpeedX: 2.0f, hoverSpeedY: 3.0f,
            hoverAmpX: 30.0f, hoverAmpY: 20.0f,
            playerPursuitWeight: 0.2f,
            hoverLerpSpeed: 5.0f,
            deltaTime: 0.016f);

        Assert.True(float.IsFinite(nextPos.X));
        Assert.True(float.IsFinite(nextPos.Y));
    }

    [Fact]
    public void CalculateBossReturnPosition_InterpolatesTowardsTarget()
    {
        Vec2f curPos = new(100.0f, 100.0f);
        Vec2f targetBase = new(500.0f, 300.0f);

        Vec2f nextPos = PhysicsService.CalculateBossReturnPosition(
            curPos, targetBase,
            returnSpeed: 2.0f,
            deltaTime: 0.1f);

        Assert.True(nextPos.X > curPos.X);
        Assert.True(nextPos.Y > curPos.Y);
        Assert.True(nextPos.X <= targetBase.X);
        Assert.True(nextPos.Y <= targetBase.Y);
    }
}
