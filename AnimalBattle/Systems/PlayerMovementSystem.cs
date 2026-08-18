using System;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// プレイヤー自機の左右移動、地上跳躍、空中二段跳躍、重力落下、地形衝突判定、および移動アニメーションを制御する ECS System。
/// </summary>
public static class PlayerMovementSystem
{
    private static bool _prevJumpKey;

    /// <summary>
    /// プレイヤー移動・跳躍・地形衝突システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    public static void Register(World world)
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

                 bool isLeft = GameEngine.CheckHitKey(0x1E) != 0 || GameEngine.CheckHitKey(0xCB) != 0;
                 bool isRight = GameEngine.CheckHitKey(0x20) != 0 || GameEngine.CheckHitKey(0xCD) != 0;
                 bool isJump = GameEngine.CheckHitKey(0x39) != 0 || GameEngine.CheckHitKey(0x25) != 0;

                 int pad = GameEngine.GetJoypadInputState(0);
                 isLeft = isLeft || (pad & GameEngine.PadInputLeft) != 0;
                 isRight = isRight || (pad & GameEngine.PadInputRight) != 0;
                 isJump = isJump || (pad & GameEngine.PadInputButton1) != 0;

                 bool jumpJustPressed = isJump && !_prevJumpKey;
                 _prevJumpKey = isJump;

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

                 vel.Vy += pCfg.Gravity;
                 if (vel.Vy > pCfg.MaxFallSpeed)
                 {
                     vel.Vy = pCfg.MaxFallSpeed;
                 }

                 float nextX = trans.X + vel.Vx;
                 float nextY = trans.Y + vel.Vy;

                 if (nextX < arenaCfg.BoundaryMinX)
                 {
                     nextX = arenaCfg.BoundaryMinX;
                 }
                 else if (nextX > arenaCfg.BoundaryMaxX)
                 {
                     nextX = arenaCfg.BoundaryMaxX;
                 }

                 bool isGrounded = false;
                 float adjustedPosY = nextY;
                 float adjustedVy = vel.Vy;

                 float pLeft = nextX - colWidth / 2.0f;
                 float pRight = nextX + colWidth / 2.0f;
                 float pBottomPrev = trans.Y + colHeight / 2.0f;
                 float pBottomNext = nextY + colHeight / 2.0f;

                 platformQuery.Each((ref Transform platTrans, ref CollisionBox platCol) =>
                 {
                     float platLeft = platTrans.X - platCol.Width / 2.0f;
                     float platRight = platTrans.X + platCol.Width / 2.0f;
                     float platTop = platTrans.Y - platCol.Height / 2.0f;

                     if (pRight > platLeft && pLeft < platRight)
                     {
                         if (pBottomPrev <= platTop + 4.0f && pBottomNext >= platTop)
                         {
                             isGrounded = true;
                             adjustedPosY = platTop - colHeight / 2.0f;
                             adjustedVy = 0.0f;
                         }
                     }
                 });

                 if (isGrounded)
                 {
                     doubleJump.Reset();
                 }

                 trans.X = nextX;
                 trans.Y = adjustedPosY;
                 vel.Vy = adjustedVy;
                 col.IsGrounded = isGrounded;

                 if (!col.IsGrounded)
                 {
                     anim.Play("Jump");
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
    }
}
