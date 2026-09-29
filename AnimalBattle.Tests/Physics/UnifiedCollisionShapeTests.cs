using AnimalBattle.Components;
using AnimalBattle.Core;
using Xunit;

namespace AnimalBattle.Tests.Physics;

public class UnifiedCollisionShapeTests
{
    [Fact]
    public void CollisionShape_ConvertsFromCollisionBox_Seamlessly()
    {
        CollisionBox box = new(64.0f, 16.0f, 2.0f, 4.0f);
        CollisionShape shape = box.ToCollisionShape();

        Assert.Equal(CollisionShapeType.Box, shape.Type);
        Assert.Equal(64.0f, shape.Width);
        Assert.Equal(16.0f, shape.Height);
        Assert.Equal(2.0f, shape.OffsetX);
        Assert.Equal(4.0f, shape.OffsetY);

        CollisionShape implicitShape = box;
        Assert.Equal(CollisionShapeType.Box, implicitShape.Type);
        Assert.Equal(64.0f, implicitShape.Width);
    }

    [Fact]
    public void CheckPlatformGround_WithCollisionShape_DetectsLanding()
    {
        Transform playerTrans = new(100.0f, 175.0f);
        CollisionShape playerShape = new CollisionShape.Box(32.0f, 32.0f);

        Transform platformTrans = new(100.0f, 200.0f);
        CollisionBox platformBox = new(120.0f, 20.0f);

        bool isGrounded = PhysicsService.CheckPlatformGround(
            170.0f,
            180.0f,
            in playerTrans,
            playerShape,
            in platformTrans,
            platformBox,
            out float adjustedY,
            out float adjustedVy);

        Assert.True(isGrounded);
        Assert.Equal(174.0f, adjustedY);
        Assert.Equal(0.0f, adjustedVy);
    }
}
