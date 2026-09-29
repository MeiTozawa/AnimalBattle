using System;
using AnimalBattle.Bindings;
using AnimalBattle.Components;
using AnimalBattle.Core;
using Xunit;

namespace AnimalBattle.Tests.Physics;

public class PhysicsServiceTests
{
    [Fact]
    public void CircleCircleIntersection_Overlapping_ReturnsTrue()
    {
        Circle2D c1 = new(new Vec2f(0.0f, 0.0f), 10.0f);
        Circle2D c2 = new(new Vec2f(12.0f, 0.0f), 10.0f);

        Assert.True(PhysicsService.Intersects(c1, c2));
    }

    [Fact]
    public void CircleCircleIntersection_TouchingBoundary_ReturnsTrue()
    {
        Circle2D c1 = new(new Vec2f(0.0f, 0.0f), 10.0f);
        Circle2D c2 = new(new Vec2f(20.0f, 0.0f), 10.0f);

        Assert.True(PhysicsService.Intersects(c1, c2));
    }

    [Fact]
    public void CircleCircleIntersection_Separated_ReturnsFalse()
    {
        Circle2D c1 = new(new Vec2f(0.0f, 0.0f), 10.0f);
        Circle2D c2 = new(new Vec2f(20.1f, 0.0f), 10.0f);

        Assert.False(PhysicsService.Intersects(c1, c2));
    }

    [Fact]
    public void BoxBoxIntersection_Overlapping_ReturnsTrue()
    {
        Box2D b1 = new(new Vec2f(0.0f, 0.0f), new Vec2f(20.0f, 20.0f));
        Box2D b2 = new(new Vec2f(15.0f, 15.0f), new Vec2f(20.0f, 20.0f));

        Assert.True(PhysicsService.Intersects(b1, b2));
    }

    [Fact]
    public void BoxBoxIntersection_Separated_ReturnsFalse()
    {
        Box2D b1 = new(new Vec2f(0.0f, 0.0f), new Vec2f(20.0f, 20.0f));
        Box2D b2 = new(new Vec2f(25.0f, 0.0f), new Vec2f(20.0f, 20.0f));

        Assert.False(PhysicsService.Intersects(b1, b2));
    }

    [Fact]
    public void CircleBoxIntersection_CircleInsideBox_ReturnsTrue()
    {
        Circle2D c = new(new Vec2f(0.0f, 0.0f), 5.0f);
        Box2D b = new(new Vec2f(0.0f, 0.0f), new Vec2f(20.0f, 20.0f));

        Assert.True(PhysicsService.Intersects(c, b));
    }

    [Fact]
    public void CircleBoxIntersection_NearCorner_ReturnsTrue()
    {
        // 盒半宽高为 10，右上角在 (10, 10)。圆在 (13, 14)，距离右上角平方为 3^2 + 4^2 = 25，半径 5，正好相交
        Box2D b = new(new Vec2f(0.0f, 0.0f), new Vec2f(20.0f, 20.0f));
        Circle2D c = new(new Vec2f(13.0f, 14.0f), 5.0f);

        Assert.True(PhysicsService.Intersects(c, b));
    }

    [Fact]
    public void CircleBoxIntersection_SeparatedFromCorner_ReturnsFalse()
    {
        Box2D b = new(new Vec2f(0.0f, 0.0f), new Vec2f(20.0f, 20.0f));
        Circle2D c = new(new Vec2f(14.0f, 14.0f), 5.0f);

        Assert.False(PhysicsService.Intersects(c, b));
    }

    [Fact]
    public void PointIntersection_InsideAndOutside()
    {
        Circle2D c = new(new Vec2f(10.0f, 10.0f), 5.0f);
        Box2D b = new(new Vec2f(10.0f, 10.0f), new Vec2f(10.0f, 10.0f));

        Assert.True(PhysicsService.Intersects(new Vec2f(12.0f, 10.0f), c));
        Assert.False(PhysicsService.Intersects(new Vec2f(16.0f, 10.0f), c));

        Assert.True(PhysicsService.Intersects(new Vec2f(14.0f, 14.0f), b));
        Assert.False(PhysicsService.Intersects(new Vec2f(16.0f, 16.0f), b));
    }

    [Fact]
    public void TransformCollisionShapeIntersects_PolymorphicDispatch()
    {
        Transform t1 = new(100.0f, 100.0f);
        CollisionShape s1 = new CollisionShape.Circle(15.0f);

        Transform t2 = new(120.0f, 100.0f);
        CollisionShape s2 = new CollisionShape.Box(20.0f, 20.0f);

        // t1 的圆半径 15，右侧边界到 115；t2 盒左侧边界在 120 - 10 = 110。重叠 5 单位。
        Assert.True(PhysicsService.Intersects(in t1, s1, in t2, s2));

        // 移远
        Transform t3 = new(150.0f, 100.0f);
        Assert.False(PhysicsService.Intersects(in t1, s1, in t3, s2));
    }

    [Fact]
    public void ScalarMath_ClampAndLerpWorkCorrectly()
    {
        float clamped = PhysicsService.Clamp(15.0f, -10.0f, 10.0f);
        Assert.Equal(10.0f, clamped);

        float lerped = PhysicsService.Lerp(0.0f, 100.0f, 0.25f);
        Assert.Equal(25.0f, lerped);
    }
}
