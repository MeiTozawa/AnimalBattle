using System;
using AnimalBattle.Bindings;

namespace AnimalBattle;

public enum CharacterState
{
    Grounded,
    Airborne,
    FastFalling,
    Hitstun
}

public class CharacterStateMachine
{
    public CharacterState State { get; private set; } = CharacterState.Airborne;
    public int DoubleJumpsLeft { get; private set; } = 1;
    public int MaxDoubleJumps { get; private set; } = 1;

    public void Reset(int maxDoubleJumps = 1)
    {
        State = CharacterState.Airborne;
        MaxDoubleJumps = maxDoubleJumps;
        DoubleJumpsLeft = maxDoubleJumps;
    }

    public void Update(
        ref NativeEngine.NativePhysicsObject playerObj,
        InputManager input,
        int playerIndex,
        float deltaTime)
    {
        // Load config values dynamically from physics.toml directly in C# (real-time tuning)
        float gravity = PhysicsConfig.Get("gravity", 1500.0f);
        float terminalVelocity = PhysicsConfig.Get("terminalVelocity", 1000.0f);
        float fastFallVelocity = PhysicsConfig.Get("fastFallVelocity", 1500.0f);
        float walkAcceleration = PhysicsConfig.Get("walkAcceleration", 800.0f);
        float runAcceleration = PhysicsConfig.Get("runAcceleration", 1400.0f);
        float maxWalkSpeed = PhysicsConfig.Get("maxWalkSpeed", 300.0f);
        float maxRunSpeed = PhysicsConfig.Get("maxRunSpeed", 600.0f);
        float airAcceleration = PhysicsConfig.Get("airAcceleration", 800.0f);
        float maxAirSpeed = PhysicsConfig.Get("maxAirSpeed", 450.0f);
        float groundFriction = PhysicsConfig.Get("groundFriction", 1200.0f);
        float airDrag = PhysicsConfig.Get("airDrag", 200.0f);
        float jumpForce = PhysicsConfig.Get("jumpForce", 650.0f);
        float doubleJumpForce = PhysicsConfig.Get("doubleJumpForce", 550.0f);
        float runThreshold = PhysicsConfig.Get("runThreshold", 0.7f);
        float fastFallStartMultiplier = PhysicsConfig.Get("fastFallStartMultiplier", 0.6f);
        float dropThroughDuration = PhysicsConfig.Get("dropThroughDuration", 0.25f);
        float hitstunAirControlScale = PhysicsConfig.Get("hitstunAirControlScale", 0.2f);
        float landingTolerance = PhysicsConfig.Get("landingTolerance", 0.05f);

        // Lock in hitstun if hitstun timer is active
        if (playerObj.HitstunTimer > 0.0f)
        {
            State = CharacterState.Hitstun;
        }
        else if (State == CharacterState.Hitstun)
        {
            State = CharacterState.Airborne;
        }

        // Get inputs
        float moveX = input.GetHorizontalAxis(playerIndex);
        float moveY = input.GetVerticalAxis(playerIndex);
        bool jump = input.IsJumpPressed(playerIndex);
        bool down = input.IsDownPressed(playerIndex);

        // Run character state machine
        switch (State)
        {
            case CharacterState.Grounded:
                playerObj.Vy = 0.0f;

                // Ground movement
                if (Math.Abs(moveX) > landingTolerance)
                {
                    float targetSpeed = Math.Abs(moveX) > runThreshold ? maxRunSpeed : maxWalkSpeed;
                    float accel = Math.Abs(moveX) > runThreshold ? runAcceleration : walkAcceleration;
                    playerObj.Vx += moveX * accel * deltaTime;
                    playerObj.Vx = Math.Clamp(playerObj.Vx, -targetSpeed, targetSpeed);
                }
                else
                {
                    // Ground friction deceleration
                    if (playerObj.Vx > 0.0f)
                    {
                        playerObj.Vx -= groundFriction * deltaTime;
                        if (playerObj.Vx < 0.0f) playerObj.Vx = 0.0f;
                    }
                    else if (playerObj.Vx < 0.0f)
                    {
                        playerObj.Vx += groundFriction * deltaTime;
                        if (playerObj.Vx > 0.0f) playerObj.Vx = 0.0f;
                    }
                }

                // Ground jump
                if (jump)
                {
                    playerObj.Vy = -jumpForce;
                    State = CharacterState.Airborne;
                }
                // Platform drop-down
                else if (down)
                {
                    playerObj.DropThroughTimer = dropThroughDuration;
                    playerObj.Y += 3.0f; // immediately push player past platform edge
                    State = CharacterState.Airborne;
                }
                break;

            case CharacterState.Airborne:
                // Gravity
                playerObj.Vy += gravity * deltaTime;
                if (playerObj.Vy > terminalVelocity) playerObj.Vy = terminalVelocity;

                // Air movement/drift
                if (Math.Abs(moveX) > landingTolerance)
                {
                    playerObj.Vx += moveX * airAcceleration * deltaTime;
                    playerObj.Vx = Math.Clamp(playerObj.Vx, -maxAirSpeed, maxAirSpeed);
                }
                else
                {
                    // Air resistance deceleration
                    if (playerObj.Vx > 0.0f)
                    {
                        playerObj.Vx -= airDrag * deltaTime;
                        if (playerObj.Vx < 0.0f) playerObj.Vx = 0.0f;
                    }
                    else if (playerObj.Vx < 0.0f)
                    {
                        playerObj.Vx += airDrag * deltaTime;
                        if (playerObj.Vx > 0.0f) playerObj.Vx = 0.0f;
                    }
                }

                // Double jump
                if (jump && DoubleJumpsLeft > 0)
                {
                    playerObj.Vy = -doubleJumpForce;
                    DoubleJumpsLeft--;
                }
                // Trigger fast falling
                else if (moveY > runThreshold && playerObj.Vy > 0.0f)
                {
                    playerObj.Vy = Math.Max(playerObj.Vy, fastFallVelocity * fastFallStartMultiplier);
                    State = CharacterState.FastFalling;
                }
                break;

            case CharacterState.FastFalling:
                // Gravity
                playerObj.Vy += gravity * deltaTime;
                if (playerObj.Vy > fastFallVelocity) playerObj.Vy = fastFallVelocity;

                // Air movement (same as airborne)
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
                        if (playerObj.Vx < 0.0f) playerObj.Vx = 0.0f;
                    }
                    else if (playerObj.Vx < 0.0f)
                    {
                        playerObj.Vx += airDrag * deltaTime;
                        if (playerObj.Vx > 0.0f) playerObj.Vx = 0.0f;
                    }
                }

                // Double jump (cancels fast fall)
                if (jump && DoubleJumpsLeft > 0)
                {
                    playerObj.Vy = -doubleJumpForce;
                    DoubleJumpsLeft--;
                    State = CharacterState.Airborne;
                }
                break;

            case CharacterState.Hitstun:
                // Gravity
                playerObj.Vy += gravity * deltaTime;
                if (playerObj.Vy > terminalVelocity) playerObj.Vy = terminalVelocity;

                // DI horizontal drift
                if (Math.Abs(moveX) > landingTolerance)
                {
                    playerObj.Vx += moveX * airAcceleration * hitstunAirControlScale * deltaTime;
                    playerObj.Vx = Math.Clamp(playerObj.Vx, -maxAirSpeed, maxAirSpeed);
                }
                break;
        }
    }

    public void HandleCollisions(ref NativeEngine.NativePhysicsObject playerObj)
    {
        // Read collision flags to update character state machine
        if ((playerObj.CollisionFlags & NativeEngine.COLLISION_FLOOR) != 0)
        {
            State = CharacterState.Grounded;
            DoubleJumpsLeft = MaxDoubleJumps;
            playerObj.Vy = 0.0f;
            playerObj.Kby = 0.0f;
        }
        else if (State == CharacterState.Grounded)
        {
            // Slipped or walked off the edge
            State = CharacterState.Airborne;
            DoubleJumpsLeft = MaxDoubleJumps;
        }

        if ((playerObj.CollisionFlags & NativeEngine.COLLISION_CEILING) != 0)
        {
            playerObj.Vy = 0.0f;
            playerObj.Kby = 0.0f;
        }
        if ((playerObj.CollisionFlags & (NativeEngine.COLLISION_WALL_LEFT | NativeEngine.COLLISION_WALL_RIGHT)) != 0)
        {
            playerObj.Vx = 0.0f;
            playerObj.Kbx = 0.0f;
        }
    }
}
