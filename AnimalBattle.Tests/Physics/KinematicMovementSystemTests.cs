using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Systems;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Physics;

/// <summary>
///     汎用キネマティック運動物理システム <see cref="KinematicMovementSystem"/> の動作検証テスト。
/// </summary>
public class KinematicMovementSystemTests
{
    [Fact]
    public void KinematicMovement_AppliesGravity_And_ConsumesHorizontalIntent()
    {
        using var world = World.Create();
        var entity = world.Entity("TestPawn")
            .Set(new Transform(100.0f, 100.0f, 1.0f, 1.0f))
            .Set(new Velocity(0.0f, 0.0f))
            .Set(new CollisionBox(32.0f, 32.0f))
            .Set(new CharacterMovementIntent { MoveX = 1.0f })
            .Set(new JumpCapability { MaxJumps = 2, RemainingJumps = 2, MoveSpeed = 200.0f, Gravity = 1000.0f, MaxFallSpeed = 500.0f });

        var platformQuery = world.QueryBuilder<Transform, CollisionBox>().With<PlatformTag>().Build();

        KinematicMovementSystem.UpdateEntity(entity, 0.1f, platformQuery);

        ref readonly var vel = ref entity.Get<Velocity>();
        Assert.Equal(200.0f, vel.Vx);
        Assert.Equal(100.0f, vel.Vy); // 1000 * 0.1s
    }

    [Fact]
    public void KinematicMovement_JumpTrigger_SetsJumpVelocity_And_DecrementsRemainingJumps()
    {
        using var world = World.Create();
        var entity = world.Entity("TestPawn")
            .Set(new Transform(100.0f, 100.0f, 1.0f, 1.0f))
            .Set(new Velocity(0.0f, 0.0f))
            .Set(new CollisionBox(32.0f, 32.0f) { IsGrounded = true })
            .Set(new CharacterMovementIntent { JumpTriggered = true })
            .Set(new JumpCapability { 
                MaxJumps = 2, 
                RemainingJumps = 2, 
                FirstJumpVelocity = -400.0f, 
                SecondJumpVelocity = -350.0f, 
                Gravity = 0.0f 
            });

        var platformQuery = world.QueryBuilder<Transform, CollisionBox>().With<PlatformTag>().Build();

        KinematicMovementSystem.UpdateEntity(entity, 0.016f, platformQuery);

        ref readonly var vel = ref entity.Get<Velocity>();
        ref readonly var jump = ref entity.Get<JumpCapability>();
        Assert.Equal(-400.0f, vel.Vy);
        Assert.Equal(1, jump.RemainingJumps);
    }
}
