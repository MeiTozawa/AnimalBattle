using System;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Core.Input;
using AnimalBattle.Core.Physics;

namespace AnimalBattle.Systems;

/// <summary>
/// プレイヤー自機の左右移動、地上跳躍、空中二段跳躍、重力落下、地形衝突判定、および移動アニメーションを制御する ECS System。
/// </summary>
public static class PlayerMovementSystem
{

    /// <summary>
    /// プレイヤー移動・跳躍・地形衝突システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World"/></returns>
    public static World AddPlayerMovementSystem(this World world)
    {
        Query<Transform, CollisionBox> platformQuery =
            world.QueryBuilder<Transform, CollisionBox>()
                 .With<PlatformTag>()
                 .Build();

        world.System<Transform, Velocity, CollisionBox, Health, DoubleJump, PlayerState, Animation, Sprite>("PlayerMovementSystem")
             .With<PlayerTag>()
             .Kind(Ecs.OnUpdate)
             .Each((Iter it, int i, ref Transform trans, ref Velocity vel, ref CollisionBox col, ref Health health, ref DoubleJump doubleJump, ref PlayerState playerState, ref Animation anim, ref Sprite sprite) =>
             {
                 PlayerConfig pCfg = ConfigManager.Player;
                 ArenaConfig arenaCfg = ConfigManager.Arena;
                 float dt = it.DeltaTime();

                 if (health.InvincibleTimer > 0.0f)
                 {
                     health.InvincibleTimer = Math.Max(0.0f, health.InvincibleTimer - dt);
                 }

                 bool isLeft = InputService.IsHolding(InputAction.Left);
                 bool isRight = InputService.IsHolding(InputAction.Right);
                 bool jumpJustPressed = InputService.IsPressed(InputAction.Jump);

                 if (!health.IsAlive)
                 {
                     vel.Vx = 0.0f;
                     vel.Vy = 0.0f;
                     return;
                 }

                 if (isLeft)
                 {
                     vel.Vx = -pCfg.MoveSpeed;
                     sprite.FlipX = true;
                 }
                 else if (isRight)
                 {
                     vel.Vx = pCfg.MoveSpeed;
                     sprite.FlipX = false;
                 }
                 else
                 {
                     vel.Vx = 0.0f;
                 }

                 float colWidth = col.Width;
                 float colHeight = col.Height;

                 if (jumpJustPressed)
                 {
                     if (col.IsGrounded)
                     {
                         vel.Vy = pCfg.FirstJumpVelocity;
                         col.IsGrounded = false;
                         doubleJump.RemainingJumps = 1;
                     }
                     else if (doubleJump.CanJump)
                     {
                         vel.Vy = pCfg.SecondJumpVelocity;
                         doubleJump.RemainingJumps--;
                         CombatEffectHelper.SpawnJumpRingEffect(world, trans.X, trans.Y + colHeight / 2.0f);
                     }
                 }

                 float currentVy = vel.Vy;
                 PhysicsService.ApplyGravity(ref currentVy, pCfg.Gravity, pCfg.MaxFallSpeed, dt);
                 vel.Vy = currentVy;

                 float nextX = trans.X + vel.Vx * dt;
                 float nextY = trans.Y + vel.Vy * dt;

                 nextX = PhysicsService.Clamp(nextX, arenaCfg.BoundaryMinX, arenaCfg.BoundaryMaxX);

                 bool isGrounded = false;
                 float adjustedPosY = nextY;
                 float adjustedVy = vel.Vy;

                 float currentY = trans.Y;
                 Box2D playerBox = new(nextX, nextY, colWidth, colHeight);

                 platformQuery.Each((ref Transform platTrans, ref CollisionBox platCol) =>
                 {
                     Box2D platformBox = new(platTrans.X, platTrans.Y, platCol.Width, platCol.Height);
                     if (PhysicsService.CheckPlatformGround(
                         currentY,
                         nextY,
                         playerBox,
                         platformBox,
                         out float groundY,
                         out float vy))
                     {
                         isGrounded = true;
                         adjustedPosY = groundY;
                         adjustedVy = vy;
                     }
                 });

                 float floorLimitY = 560.0f - colHeight / 2.0f;
                 if (adjustedPosY >= floorLimitY)
                 {
                     isGrounded = true;
                     adjustedPosY = floorLimitY;
                     adjustedVy = 0.0f;
                 }

                 if (isGrounded)
                 {
                     doubleJump.RemainingJumps = doubleJump.MaxJumps;
                 }

                 trans.X = nextX;
                 trans.Y = adjustedPosY;
                 vel.Vy = adjustedVy;
                 col.IsGrounded = isGrounded;

                 if (!col.IsGrounded)
                 {
                     if (vel.Vy < 0.0f)
                     {
                         anim.Play("Jump");
                     }
                     else
                     {
                         anim.Play("Fall");
                     }
                 }
                 else if (Math.Abs(vel.Vx) > 0.1f)
                 {
                     anim.Play("Run");
                 }
                 else
                 {
                     anim.Play("Idle");
                 }
             });

        return world;
    }
}
