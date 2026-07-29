using System;
using AnimalBattle.Core;
using AnimalBattle.Inputs;

namespace AnimalBattle.Characters;

/// <summary>
/// キャラクターの状態遷移および物理パラメータ更新を制御するステートマシン。
/// </summary>
public class CharacterStateMachine
{
    /// <summary>
    /// 現在のキャラクター状態を取得します。
    /// </summary>
    public CharacterState State { get; private set; } = CharacterState.Airborne;

    /// <summary>
    /// 残りの空中ジャンプ可能回数を取得します。
    /// </summary>
    public int DoubleJumpsLeft { get; private set; } = 1;

    /// <summary>
    /// 最大空中ジャンプ回数を取得します。
    /// </summary>
    public int MaxDoubleJumps { get; private set; } = 1;

    /// <summary>
    /// 攻撃モーションの残りタイマー (秒) を取得します。
    /// </summary>
    public float AttackTimer { get; private set; } = 0.0f;

    /// <summary>
    /// ステートマシンを初期状態にリセットします。
    /// </summary>
    /// <param name="maxDoubleJumps">最大空中ジャンプ可能回数</param>
    public void Reset(int maxDoubleJumps = 1)
    {
        State = CharacterState.Airborne;
        MaxDoubleJumps = maxDoubleJumps;
        DoubleJumpsLeft = maxDoubleJumps;
        AttackTimer = 0.0f;
    }

    /// <summary>
    /// 入力および設定パラメータに基づき、キャラクターの状態と物理オブジェクトパラメータを更新します。
    /// </summary>
    /// <param name="playerObj">Native C++ 物理オブジェクトの参照</param>
    /// <param name="input">入力マネージャー</param>
    /// <param name="playerIndex">プレイヤーインデックス (1 または 2)</param>
    /// <param name="config">キャラクターパラメータ設定</param>
    /// <param name="deltaTime">フレーム経過時間（秒）</param>
    public void Update(
        ref NativePhysicsObject playerObj,
        InputManager input,
        int playerIndex,
        CharacterConfig config,
        float deltaTime)
    {
        float gravity = config.Gravity;
        float terminalVelocity = config.TerminalVelocity;
        float fastFallVelocity = config.FastFallVelocity;
        float groundAcceleration = config.GroundAcceleration;
        float maxGroundSpeed = config.MaxGroundSpeed;
        float airAcceleration = config.AirAcceleration;
        float maxAirSpeed = config.MaxAirSpeed;
        float groundFriction = config.GroundFriction;
        float airDrag = config.AirDrag;
        float jumpForce = config.JumpForce;
        float doubleJumpForce = config.DoubleJumpForce;
        MaxDoubleJumps = config.MaxDoubleJumps;
        float fastFallThreshold = config.FastFallThreshold;
        float fastFallStartMultiplier = config.FastFallStartMultiplier;
        float dropThroughDuration = config.DropThroughDuration;
        float hitstunAirControlScale = config.HitstunAirControlScale;
        float landingTolerance = config.LandingTolerance;

        if (playerObj.HitstunTimer > 0.0f)
        {
            State = CharacterState.Hitstun;
        }
        else if (State == CharacterState.Hitstun)
        {
            State = CharacterState.Airborne;
        }

        float moveX = input.GetHorizontalAxis(playerIndex);
        float moveY = input.GetVerticalAxis(playerIndex);
        bool jump = input.IsPressed(playerIndex, InputAction.Jump);
        bool down = input.IsHolding(playerIndex, InputAction.MoveDown);
        bool attack = input.IsPressed(playerIndex, InputAction.Attack);

        if (attack && State != CharacterState.Hitstun && State != CharacterState.Attacking)
        {
            State = CharacterState.Attacking;
            AttackTimer = 0.4f;
        }

        switch (State)
        {
            case CharacterState.Attacking:
                AttackTimer -= deltaTime;
                playerObj.Vy += gravity * 0.5f * deltaTime;
                if (playerObj.Vy > terminalVelocity)
                {
                    playerObj.Vy = terminalVelocity;
                }

                if (AttackTimer <= 0.0f)
                {
                    AttackTimer = 0.0f;
                    State = ((playerObj.CollisionFlags & CollisionFlags.Floor) != CollisionFlags.None) ?
                        CharacterState.Grounded : CharacterState.Airborne;
                }
                break;

            case CharacterState.Grounded:
                playerObj.Vy = 0.0f;

                if (Math.Abs(moveX) > landingTolerance)
                {
                    playerObj.Vx += moveX * groundAcceleration * deltaTime;
                    playerObj.Vx = Math.Clamp(playerObj.Vx, -maxGroundSpeed, maxGroundSpeed);
                }
                else
                {
                    if (playerObj.Vx > 0.0f)
                    {
                        playerObj.Vx -= groundFriction * deltaTime;
                        if (playerObj.Vx < 0.0f)
                        {
                            playerObj.Vx = 0.0f;
                        }
                    }
                    else if (playerObj.Vx < 0.0f)
                    {
                        playerObj.Vx += groundFriction * deltaTime;
                        if (playerObj.Vx > 0.0f)
                        {
                            playerObj.Vx = 0.0f;
                        }
                    }
                }

                if (jump)
                {
                    playerObj.Vy = -jumpForce;
                    State = CharacterState.Airborne;
                }
                else if (down && (playerObj.CollisionFlags & CollisionFlags.OnSemiSolid) != CollisionFlags.None)
                {
                    playerObj.DropThroughTimer = dropThroughDuration;
                    playerObj.Y += 3.0f;
                    State = CharacterState.Airborne;
                }
                break;

            case CharacterState.Airborne:
                playerObj.Vy += gravity * deltaTime;
                if (playerObj.Vy > terminalVelocity)
                {
                    playerObj.Vy = terminalVelocity;
                }

                if (Math.Abs(moveX) > landingTolerance)
                {
                    playerObj.Vx += moveX * airAcceleration * deltaTime;
                    playerObj.Vx = Math.Clamp(playerObj.Vx, -maxAirSpeed, maxAirSpeed);
                }
                else
                {
                    if (playerObj.Vx > 0.0f)
                    {
                        playerObj.Vx -= airDrag * deltaTime;
                        if (playerObj.Vx < 0.0f)
                        {
                            playerObj.Vx = 0.0f;
                        }
                    }
                    else if (playerObj.Vx < 0.0f)
                    {
                        playerObj.Vx += airDrag * deltaTime;
                        if (playerObj.Vx > 0.0f)
                        {
                            playerObj.Vx = 0.0f;
                        }
                    }
                }

                if (jump && DoubleJumpsLeft > 0)
                {
                    playerObj.Vy = -doubleJumpForce;
                    DoubleJumpsLeft--;
                }
                else if (moveY > fastFallThreshold && playerObj.Vy > 0.0f)
                {
                    playerObj.Vy = Math.Max(playerObj.Vy, fastFallVelocity * fastFallStartMultiplier);
                    State = CharacterState.FastFalling;
                }
                break;

            case CharacterState.FastFalling:
                playerObj.Vy += gravity * deltaTime;
                if (playerObj.Vy > fastFallVelocity)
                {
                    playerObj.Vy = fastFallVelocity;
                }

                if (Math.Abs(moveX) > landingTolerance)
                {
                    playerObj.Vx += moveX * airAcceleration * deltaTime;
                    playerObj.Vx = Math.Clamp(playerObj.Vx, -maxAirSpeed, maxAirSpeed);
                }
                else
                {
                    if (playerObj.Vx > 0.0f)
                    {
                        playerObj.Vx -= airDrag * deltaTime;
                        if (playerObj.Vx < 0.0f)
                        {
                            playerObj.Vx = 0.0f;
                        }
                    }
                    else if (playerObj.Vx < 0.0f)
                    {
                        playerObj.Vx += airDrag * deltaTime;
                        if (playerObj.Vx > 0.0f)
                        {
                            playerObj.Vx = 0.0f;
                        }
                    }
                }

                if (jump && DoubleJumpsLeft > 0)
                {
                    playerObj.Vy = -doubleJumpForce;
                    DoubleJumpsLeft--;
                    State = CharacterState.Airborne;
                }
                break;

            case CharacterState.Hitstun:
                playerObj.Vy += gravity * deltaTime;
                if (playerObj.Vy > terminalVelocity)
                {
                    playerObj.Vy = terminalVelocity;
                }

                if (Math.Abs(moveX) > landingTolerance)
                {
                    playerObj.Vx += moveX * airAcceleration * hitstunAirControlScale * deltaTime;
                    playerObj.Vx = Math.Clamp(playerObj.Vx, -maxAirSpeed, maxAirSpeed);
                }
                break;
        }
    }

    /// <summary>
    /// C++ 物理エンジンからの衝突フラグを受信し、ステートマシンの状態を更新します。
    /// </summary>
    /// <param name="playerObj">Native C++ 物理オブジェクトの参照</param>
    public void HandleCollisions(ref NativePhysicsObject playerObj)
    {
        if ((playerObj.CollisionFlags & CollisionFlags.Floor) != CollisionFlags.None)
        {
            if (State != CharacterState.Hitstun && State != CharacterState.Attacking)
            {
                State = CharacterState.Grounded;
            }
            DoubleJumpsLeft = MaxDoubleJumps;
            playerObj.Vy = 0.0f;
            playerObj.KnockbackY = 0.0f;
        }
        else if (State == CharacterState.Grounded)
        {
            State = CharacterState.Airborne;
            DoubleJumpsLeft = MaxDoubleJumps;
        }

        if ((playerObj.CollisionFlags & CollisionFlags.Ceiling) != CollisionFlags.None)
        {
            playerObj.Vy = 0.0f;
            playerObj.KnockbackY = 0.0f;
        }
        if ((playerObj.CollisionFlags & (CollisionFlags.WallLeft | CollisionFlags.WallRight)) != CollisionFlags.None)
        {
            playerObj.Vx = 0.0f;
            playerObj.KnockbackX = 0.0f;
        }
    }
}
