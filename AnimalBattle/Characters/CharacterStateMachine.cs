using System;
using AnimalBattle.Bindings;

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
    /// ステートマシンを初期状態にリセットします。
    /// </summary>
    /// <param name="maxDoubleJumps">最大空中ジャンプ可能回数</param>
    public void Reset(int maxDoubleJumps = 1)
    {
        State = CharacterState.Airborne;
        MaxDoubleJumps = maxDoubleJumps;
        DoubleJumpsLeft = maxDoubleJumps;
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
        ref NativeEngine.NativePhysicsObject playerObj,
        InputManager input,
        int playerIndex,
        CharacterConfig config,
        float deltaTime)
    {
        float gravity = config.Gravity;
        float terminalVelocity = config.TerminalVelocity;
        float fastFallVelocity = config.FastFallVelocity;
        float walkAcceleration = config.WalkAcceleration;
        float runAcceleration = config.RunAcceleration;
        float maxWalkSpeed = config.MaxWalkSpeed;
        float maxRunSpeed = config.MaxRunSpeed;
        float airAcceleration = config.AirAcceleration;
        float maxAirSpeed = config.MaxAirSpeed;
        float groundFriction = config.GroundFriction;
        float airDrag = config.AirDrag;
        float jumpForce = config.JumpForce;
        float doubleJumpForce = config.DoubleJumpForce;
        float runThreshold = config.RunThreshold;
        float fastFallStartMultiplier = config.FastFallStartMultiplier;
        float dropThroughDuration = config.DropThroughDuration;
        float hitstunAirControlScale = config.HitstunAirControlScale;
        float landingTolerance = config.LandingTolerance;

        // ヒットスタンタイマーが有効な場合はヒットスタン状態に固定
        if (playerObj.HitstunTimer > 0.0f)
        {
            State = CharacterState.Hitstun;
        }
        else if (State == CharacterState.Hitstun)
        {
            State = CharacterState.Airborne;
        }

        // 入力値の取得
        float moveX = input.GetHorizontalAxis(playerIndex);
        float moveY = input.GetVerticalAxis(playerIndex);
        bool jump = input.IsJumpPressed(playerIndex);
        bool down = input.IsDownPressed(playerIndex);

        // ステートマシン処理
        switch (State)
        {
            case CharacterState.Grounded:
                playerObj.Vy = 0.0f;

                // 地上移動
                if (Math.Abs(moveX) > landingTolerance)
                {
                    float targetSpeed = Math.Abs(moveX) > runThreshold ? maxRunSpeed : maxWalkSpeed;
                    float accel = Math.Abs(moveX) > runThreshold ? runAcceleration : walkAcceleration;
                    playerObj.Vx += moveX * accel * deltaTime;
                    playerObj.Vx = Math.Clamp(playerObj.Vx, -targetSpeed, targetSpeed);
                }
                else
                {
                    // 地上摩擦減速
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

                // 地上ジャンプ
                if (jump)
                {
                    playerObj.Vy = -jumpForce;
                    State = CharacterState.Airborne;
                }
                // すり抜け床の下降り操作（半実体足場のみ許可）
                else if (down && (playerObj.CollisionFlags & NativeEngine.CollisionOnSemiSolid) != 0)
                {
                    playerObj.DropThroughTimer = dropThroughDuration;
                    playerObj.Y += 3.0f; // すり抜け開始のため即座に足場下へ移動
                    State = CharacterState.Airborne;
                }
                break;

            case CharacterState.Airborne:
                // 重力適用
                playerObj.Vy += gravity * deltaTime;
                if (playerObj.Vy > terminalVelocity)
                {
                    playerObj.Vy = terminalVelocity;
                }

                // 空中移動・ドリフト
                if (Math.Abs(moveX) > landingTolerance)
                {
                    playerObj.Vx += moveX * airAcceleration * deltaTime;
                    playerObj.Vx = Math.Clamp(playerObj.Vx, -maxAirSpeed, maxAirSpeed);
                }
                else
                {
                    // 空中抵抗減速
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

                // 空中ジャンプ（2段ジャンプ）
                if (jump && DoubleJumpsLeft > 0)
                {
                    playerObj.Vy = -doubleJumpForce;
                    DoubleJumpsLeft--;
                }
                // 急降下開始判定
                else if (moveY > runThreshold && playerObj.Vy > 0.0f)
                {
                    playerObj.Vy = Math.Max(playerObj.Vy, fastFallVelocity * fastFallStartMultiplier);
                    State = CharacterState.FastFalling;
                }
                break;

            case CharacterState.FastFalling:
                // 急降下重力適用
                playerObj.Vy += gravity * deltaTime;
                if (playerObj.Vy > fastFallVelocity)
                {
                    playerObj.Vy = fastFallVelocity;
                }

                // 空中移動
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

                // 空中ジャンプ（急降下キャンセル）
                if (jump && DoubleJumpsLeft > 0)
                {
                    playerObj.Vy = -doubleJumpForce;
                    DoubleJumpsLeft--;
                    State = CharacterState.Airborne;
                }
                break;

            case CharacterState.Hitstun:
                // 重力適用
                playerObj.Vy += gravity * deltaTime;
                if (playerObj.Vy > terminalVelocity)
                {
                    playerObj.Vy = terminalVelocity;
                }

                // DI（ベクトル変更）空中制御
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
    public void HandleCollisions(ref NativeEngine.NativePhysicsObject playerObj)
    {
        if ((playerObj.CollisionFlags & NativeEngine.CollisionFloor) != 0)
        {
            State = CharacterState.Grounded;
            DoubleJumpsLeft = MaxDoubleJumps;
            playerObj.Vy = 0.0f;
            playerObj.Kby = 0.0f;
        }
        else if (State == CharacterState.Grounded)
        {
            // 足場から落下
            State = CharacterState.Airborne;
            DoubleJumpsLeft = MaxDoubleJumps;
        }

        if ((playerObj.CollisionFlags & NativeEngine.CollisionCeiling) != 0)
        {
            playerObj.Vy = 0.0f;
            playerObj.Kby = 0.0f;
        }
        if ((playerObj.CollisionFlags & (NativeEngine.CollisionWallLeft | NativeEngine.CollisionWallRight)) != 0)
        {
            playerObj.Vx = 0.0f;
            playerObj.Kbx = 0.0f;
        }
    }
}
