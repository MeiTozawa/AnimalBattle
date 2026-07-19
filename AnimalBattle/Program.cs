using System;
using System.Diagnostics;
using System.Threading;
using AnimalBattle.Bindings;

namespace AnimalBattle;

class Program
{
    static void Main(string[] args)
    {
        const int Width = 1280;
        const int Height = 720;

        Console.WriteLine("Initializing C++ DxLib Native Engine...");
        if (NativeEngine.Initialize(Width, Height, true) == -1)
        {
            Console.WriteLine("Failed to initialize game engine!");
            return;
        }

        IntPtr nativeRenderService = NativeEngine.GetRenderService();
        var render = new RenderService(nativeRenderService);

        // Initialize Physics Engine
        Console.WriteLine("Initializing Physics Engine...");
        IntPtr physics = NativeEngine.PhysicsCreate();

        // Blast zones managed in C#
        float blastZoneLeft = -100.0f;
        float blastZoneRight = 1380.0f;
        float blastZoneTop = -300.0f;
        float blastZoneBottom = 820.0f;

        // Add platforms:
        // Main Stage (Solid)
        int stageId = NativeEngine.PhysicsAddPlatform(physics, 300.0f, 500.0f, 980.0f, 540.0f, (int)NativeEngine.PlatformType.Solid);
        // Left Floating Platform (Semi-Solid)
        int platLeftId = NativeEngine.PhysicsAddPlatform(physics, 350.0f, 380.0f, 550.0f, 390.0f, (int)NativeEngine.PlatformType.SemiSolid);
        // Right Floating Platform (Semi-Solid)
        int platRightId = NativeEngine.PhysicsAddPlatform(physics, 730.0f, 380.0f, 930.0f, 390.0f, (int)NativeEngine.PlatformType.SemiSolid);

        // Spawn player: width=40, height=70, weight=100
        int playerObjId = NativeEngine.PhysicsAddObject(physics, 640.0f, 300.0f, 40.0f, 70.0f, 100.0f);

        // Initialize State Machine
        var stateMachine = new CharacterStateMachine();
        stateMachine.Reset();

        // Initialize Input Manager
        var input = new InputManager();

        var stopwatch = Stopwatch.StartNew();
        long lastTime = stopwatch.ElapsedTicks;
        double tickFrequency = (double)Stopwatch.Frequency;

        Console.WriteLine("Entering Game Loop...");
        while (NativeEngine.ProcessMessage() == 0 && !NativeEngine.IsExitRequested())
        {
            long currentTime = stopwatch.ElapsedTicks;
            float deltaTime = (float)((currentTime - lastTime) / tickFrequency);
            lastTime = currentTime;

            // Update Input
            input.Update();

            // Exit on Escape key
            if (input.IsKeyDown(InputManager.KEY_INPUT_ESCAPE))
            {
                break;
            }

            // Get player state from C++
            NativeEngine.NativePhysicsObject playerObj = default;
            unsafe
            {
                NativeEngine.PhysicsGetObjectState(physics, playerObjId, &playerObj);
            }

            // Real-time config queries for respawn and knockback parameters
            float respawnY = NativeEngine.PhysicsGetConfigValue(physics, "respawnY");

            // Reset player position & state
            if (input.IsResetPressed())
            {
                NativeEngine.PhysicsRemoveObject(physics, playerObjId);
                playerObjId = NativeEngine.PhysicsAddObject(physics, 640.0f, 300.0f, 40.0f, 70.0f, 100.0f);
                stateMachine.Reset();
                continue;
            }

            // Apply test knockback (Hit simulator)
            if (input.IsKnockbackTestPressed())
            {
                NativeEngine.PhysicsApplyKnockback(physics, playerObjId, 25.0f, (float)(Math.PI / 4.0), 400.0f, 1.3f);
                stateMachine.Reset(); // will force into hitstun in next update since hitstunTimer > 0
                unsafe
                {
                    NativeEngine.PhysicsGetObjectState(physics, playerObjId, &playerObj);
                }
            }

            // Update the Character State Machine (inputs, physics values, state transition logic)
            stateMachine.Update(ref playerObj, input, physics, deltaTime);

            // Write modified velocities and coordinates back to C++
            unsafe
            {
                NativeEngine.PhysicsSetObjectState(physics, playerObjId, &playerObj);
            }

            // Step Physics in C++ (resolves solid stage and semi-solid platforms collisions)
            NativeEngine.PhysicsStep(physics, deltaTime);

            // Fetch final resolved coordinates and collision flags from C++
            unsafe
            {
                NativeEngine.PhysicsGetObjectState(physics, playerObjId, &playerObj);
            }

            // Handle collision flags response in the State Machine
            stateMachine.HandleCollisions(ref playerObj);

            // Blast Zones checking in C#
            if (playerObj.X < blastZoneLeft || playerObj.X > blastZoneRight ||
                playerObj.Y < blastZoneTop || playerObj.Y > blastZoneBottom)
            {
                // KO! Respawn player
                NativeEngine.PhysicsRemoveObject(physics, playerObjId);
                playerObjId = NativeEngine.PhysicsAddObject(physics, 640.0f, respawnY, 40.0f, 70.0f, 100.0f);
                stateMachine.Reset();
                Console.WriteLine("PLAYER KO!");
                continue;
            }

            // Start frame drawing
            NativeEngine.BeginFrame();

            // Render Blast Zones boundaries (Hollow grey rectangle)
            render.DrawBox(new Rect<int>((int)blastZoneLeft, (int)blastZoneTop, (int)blastZoneRight, (int)blastZoneBottom), 0xFF444444, false);

            // Draw Stage & Platforms
            // Main Stage: Sleek Dark Green Solid
            render.DrawBox(new Rect<int>(300, 500, 980, 540), 0xFF2E6F40, true);
            render.DrawBox(new Rect<int>(300, 500, 980, 540), 0xFF1B4D26, false); // stage outline
            
            // Left Plat: Semi-Solid Orange
            render.DrawBox(new Rect<int>(350, 380, 550, 390), 0xFFE67E22, true);
            render.DrawBox(new Rect<int>(350, 380, 550, 390), 0xFFD35400, false);

            // Right Plat: Semi-Solid Orange
            render.DrawBox(new Rect<int>(730, 380, 930, 390), 0xFFE67E22, true);
            render.DrawBox(new Rect<int>(730, 380, 930, 390), 0xFFD35400, false);

            // Draw Player AABB
            int pX1 = (int)(playerObj.X - playerObj.Width / 2.0f);
            int pY1 = (int)(playerObj.Y - playerObj.Height);
            int pX2 = (int)(playerObj.X + playerObj.Width / 2.0f);
            int pY2 = (int)playerObj.Y;

            uint playerColor = 0xFF3498DB; // Default Airborne Blue
            if (stateMachine.State == CharacterState.Hitstun)
            {
                // Flashing red & white in hitstun
                playerColor = ((int)(playerObj.HitstunTimer * 10) % 2 == 0) ? 0xFFE74C3C : 0xFFFFFFFF;
            }
            else if (stateMachine.State == CharacterState.Grounded)
            {
                playerColor = 0xFF2ECC71; // Grounded Green
            }
            else if (stateMachine.State == CharacterState.FastFalling)
            {
                playerColor = 0xFF2980B9; // Fast Falling Dark Blue
            }

            render.DrawBox(new Rect<int>(pX1, pY1, pX2, pY2), playerColor, true);
            render.DrawBox(new Rect<int>(pX1, pY1, pX2, pY2), 0xFF2C3E50, false); // Player outline

            // Draw text status panel
            int startTextX = 50;
            int startTextY = 50;
            int lineSpacing = 30;

            render.DrawString(new Point<int>(startTextX, startTextY), "--- AnimalBattle State Machine Engine ---", 0xFFFFFFFF);
            render.DrawString(new Point<int>(startTextX, startTextY + lineSpacing), $"Damage: {playerObj.DamagePercent:F1}%", 0xFFE74C3C);
            render.DrawString(new Point<int>(startTextX, startTextY + lineSpacing * 2), $"Position: ({playerObj.X:F1}, {playerObj.Y:F1})", 0xFFFFFF00);
            render.DrawString(new Point<int>(startTextX, startTextY + lineSpacing * 3), $"Velocity: ({playerObj.Vx:F1}, {playerObj.Vy:F1})", 0xFF00FFFF);
            render.DrawString(new Point<int>(startTextX, startTextY + lineSpacing * 4), $"Knockback: ({playerObj.Kbx:F1}, {playerObj.Kby:F1})", 0xFFFF00FF);
            render.DrawString(new Point<int>(startTextX, startTextY + lineSpacing * 5), $"Hitstun Timer: {playerObj.HitstunTimer:F2}s", 0xFFE74C3C);
            render.DrawString(new Point<int>(startTextX, startTextY + lineSpacing * 6), $"Double Jumps Left: {stateMachine.DoubleJumpsLeft}", 0xFFFFFFFF);
            
            render.DrawString(new Point<int>(startTextX, startTextY + lineSpacing * 7), $"State: {stateMachine.State.ToString().ToUpper()}", 0xFF00FF00);

            // Draw Controls Help Panel
            int helpTextX = Width - 450;
            int helpTextY = 50;
            render.DrawString(new Point<int>(helpTextX, helpTextY), "--- Controls ---", 0xFFFFFFFF);
            render.DrawString(new Point<int>(helpTextX, helpTextY + lineSpacing), "A / D or Left / Right: Move", 0xFFDDDDDD);
            render.DrawString(new Point<int>(helpTextX, helpTextY + lineSpacing * 2), "Space or W or Up: Jump / Double Jump", 0xFFDDDDDD);
            render.DrawString(new Point<int>(helpTextX, helpTextY + lineSpacing * 3), "S or Down: Fast Fall / Drop Down Platform", 0xFFDDDDDD);
            render.DrawString(new Point<int>(helpTextX, helpTextY + lineSpacing * 4), "F: Apply Knockback Test (Simulate Hit)", 0xFFFF8888);
            render.DrawString(new Point<int>(helpTextX, helpTextY + lineSpacing * 5), "R: Reset Player Position & Stats", 0xFF88FF88);
            render.DrawString(new Point<int>(helpTextX, helpTextY + lineSpacing * 6), "ESC: Exit Game", 0xFF888888);

            // Finish frame & Swap buffers
            NativeEngine.EndFrame(deltaTime);
        }

        Console.WriteLine("Cleaning up Physics...");
        NativeEngine.PhysicsDestroy(physics);

        Console.WriteLine("Shutting down Native Engine...");
        NativeEngine.Shutdown();
    }
}
