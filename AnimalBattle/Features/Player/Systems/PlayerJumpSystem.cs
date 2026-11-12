namespace AnimalBattle.Player.Systems;

using System;
using AnimalBattle.Events;
using AnimalBattle.Player.Components;

/// <summary>
///     自機の跳躍（ジャンプ）入力を検知し、先行入力、コヨーテタイム、二段ジャンプおよび跳躍演出を管理するシステム。
/// </summary>
public static class PlayerJumpSystem
{
    /// <summary>
    ///     跳躍制御システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    public static World AddPlayerJumpSystem(this World world)
    {
        world.System<Transform, Velocity, CollisionBox, MovementIntent, JumpCapability, SpringScale>("PlayerJumpSystem")
            .Kind(Ecs.OnUpdate)
            .Without<DeadTag>()
            .Without<HitstopState>()
            .Without<DashingTag>()
            .Iter(ExecutePlayerJump);

        return world;
    }

    private static void ExecutePlayerJump(
        Iter it,
        Field<Transform> transforms,
        Field<Velocity> velocities,
        Field<CollisionBox> collisionBoxes,
        Field<MovementIntent> intents,
        Field<JumpCapability> jumps,
        Field<SpringScale> springs)
    {
        var count = it.Count();
        if (count == 0) return;

        var dt = it.DeltaTime();

        for (var i = 0; i < count; i++)
        {
            ref readonly var trans = ref transforms[i];
            ref var vel = ref velocities[i];
            ref var col = ref collisionBoxes[i];
            ref readonly var intent = ref intents[i];
            ref var jump = ref jumps[i];
            ref var spring = ref springs[i];

            // 先行入力およびコヨーテタイムの更新
            if (intent.JumpTriggered)
            {
                jump.JumpBufferTimer = jump.JumpBufferDuration;
            }
            else if (jump.JumpBufferTimer > 0.0f)
            {
                jump.JumpBufferTimer = MathF.Max(0.0f, jump.JumpBufferTimer - dt);
            }

            if (col.IsGrounded)
            {
                jump.CoyoteTimer = jump.CoyoteTimeDuration;
            }
            else if (jump.CoyoteTimer > 0.0f)
            {
                jump.CoyoteTimer = MathF.Max(0.0f, jump.CoyoteTimer - dt);
            }

            // 跳躍の判定と初速付与
            if (jump.JumpBufferTimer > 0.0f)
            {
                var didJump = false;
                if (col.IsGrounded || jump.CoyoteTimer > 0.0f)
                {
                    // 地面初段ジャンプ：接地中またはコヨーテ猶予内
                    vel.Value = new Vec2f(vel.Value.X, jump.FirstJumpVelocity);
                    col.IsGrounded = false;
                    jump.CoyoteTimer = 0.0f;
                    jump.JumpBufferTimer = 0.0f;
                    jump.RemainingJumps = jump.MaxJumps - 1;
                    didJump = true;
                    EventBus.Publish(new PlayerJumpedEvent());
                }
                else if (jump.RemainingJumps > 0)
                {
                    // 空中二段ジャンプ
                    vel.Value = new Vec2f(vel.Value.X, jump.SecondJumpVelocity);
                    jump.RemainingJumps--;
                    jump.JumpBufferTimer = 0.0f;
                    didJump = true;
                    var ringPos = new Vec2f(trans.Position.X, trans.Position.Y + col.Size.Y / 2.0f);
                    EventBus.Publish(new PlayerDoubleJumpedEvent(ringPos));
                }

                if (didJump)
                {
                    spring.Current = jump.JumpStretchScale;
                    spring.Velocity = Vec2f.Zero;
                }
            }
        }
    }
}
