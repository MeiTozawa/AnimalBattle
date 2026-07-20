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

        // Initialize Native C++ Camera
        Console.WriteLine("Initializing Native Camera Module...");
        IntPtr camera = NativeEngine.CameraCreate();
        NativeEngine.CameraSetConfig(camera, minScale: 0.45f, maxScale: 1.0f, lerpSpeed: 4.5f, paddingX: 280.0f, paddingY: 180.0f);

        // Initialize Stage Manager & Load default map (Map 1)
        Console.WriteLine("Initializing Stage Manager & Loading Map 1...");
        var stageManager = new StageManager();
        stageManager.LoadMap(physics, 0);

        MapData currentMap = stageManager.CurrentMap;
        NativeEngine.CameraReset(camera, 640.0f, 360.0f, 1.0f);

        // Spawn 2 Players at current map spawn points
        int player1ObjId = NativeEngine.PhysicsAddObject(physics, currentMap.P1Spawn.X, currentMap.P1Spawn.Y, 40.0f, 70.0f, 100.0f);
        int player2ObjId = NativeEngine.PhysicsAddObject(physics, currentMap.P2Spawn.X, currentMap.P2Spawn.Y, 40.0f, 70.0f, 100.0f);

        // Initialize State Machines for P1 and P2
        var stateMachine1 = new CharacterStateMachine();
        var stateMachine2 = new CharacterStateMachine();
        stateMachine1.Reset();
        stateMachine2.Reset();

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

            // Real-time Map Switching (Keys 1, 2, 3, 4)
            int switchMapIndex = input.GetRequestedMapSwitchIndex();
            if (switchMapIndex != -1 && switchMapIndex != stageManager.CurrentMapIndex)
            {
                stageManager.LoadMap(physics, switchMapIndex);
                currentMap = stageManager.CurrentMap;

                // Respawn players on new stage
                NativeEngine.PhysicsRemoveObject(physics, player1ObjId);
                NativeEngine.PhysicsRemoveObject(physics, player2ObjId);
                player1ObjId = NativeEngine.PhysicsAddObject(physics, currentMap.P1Spawn.X, currentMap.P1Spawn.Y, 40.0f, 70.0f, 100.0f);
                player2ObjId = NativeEngine.PhysicsAddObject(physics, currentMap.P2Spawn.X, currentMap.P2Spawn.Y, 40.0f, 70.0f, 100.0f);

                stateMachine1.Reset();
                stateMachine2.Reset();
                NativeEngine.CameraReset(camera, 640.0f, 360.0f, 1.0f);
                continue;
            }

            // Fetch player states from C++
            NativeEngine.NativePhysicsObject p1Obj = default;
            NativeEngine.NativePhysicsObject p2Obj = default;
            unsafe
            {
                NativeEngine.PhysicsGetObjectState(physics, player1ObjId, &p1Obj);
                NativeEngine.PhysicsGetObjectState(physics, player2ObjId, &p2Obj);
            }

            // Reset both players on R key
            if (input.IsResetPressed())
            {
                NativeEngine.PhysicsRemoveObject(physics, player1ObjId);
                NativeEngine.PhysicsRemoveObject(physics, player2ObjId);
                player1ObjId = NativeEngine.PhysicsAddObject(physics, currentMap.P1Spawn.X, currentMap.P1Spawn.Y, 40.0f, 70.0f, 100.0f);
                player2ObjId = NativeEngine.PhysicsAddObject(physics, currentMap.P2Spawn.X, currentMap.P2Spawn.Y, 40.0f, 70.0f, 100.0f);
                stateMachine1.Reset();
                stateMachine2.Reset();
                NativeEngine.CameraReset(camera, 640.0f, 360.0f, 1.0f);
                continue;
            }

            // Apply test knockback (F for P1, G for P2)
            if (input.IsKnockbackTestPressed(1))
            {
                NativeEngine.PhysicsApplyKnockback(physics, player1ObjId, 25.0f, (float)(Math.PI / 4.0), 450.0f, 1.3f);
                stateMachine1.Reset();
            }
            if (input.IsKnockbackTestPressed(2))
            {
                NativeEngine.PhysicsApplyKnockback(physics, player2ObjId, 25.0f, (float)(3.0 * Math.PI / 4.0), 450.0f, 1.3f);
                stateMachine2.Reset();
            }

            // Update Character State Machines
            stateMachine1.Update(ref p1Obj, input, playerIndex: 1, deltaTime);
            stateMachine2.Update(ref p2Obj, input, playerIndex: 2, deltaTime);

            // Write modified velocities and coordinates back to C++
            unsafe
            {
                NativeEngine.PhysicsSetObjectState(physics, player1ObjId, &p1Obj);
                NativeEngine.PhysicsSetObjectState(physics, player2ObjId, &p2Obj);
            }

            // Step Physics in C++
            NativeEngine.PhysicsStep(physics, deltaTime);

            // Fetch final resolved coordinates and collision flags
            unsafe
            {
                NativeEngine.PhysicsGetObjectState(physics, player1ObjId, &p1Obj);
                NativeEngine.PhysicsGetObjectState(physics, player2ObjId, &p2Obj);
            }

            // Handle collision responses
            stateMachine1.HandleCollisions(ref p1Obj);
            stateMachine2.HandleCollisions(ref p2Obj);

            // Blast Zone checks
            Rect<float> blastZone = currentMap.BlastZone;
            if (p1Obj.X < blastZone.X1 || p1Obj.X > blastZone.X2 || p1Obj.Y < blastZone.Y1 || p1Obj.Y > blastZone.Y2)
            {
                NativeEngine.PhysicsRemoveObject(physics, player1ObjId);
                player1ObjId = NativeEngine.PhysicsAddObject(physics, currentMap.P1Spawn.X, currentMap.P1Spawn.Y, 40.0f, 70.0f, 100.0f);
                stateMachine1.Reset();
                Console.WriteLine($"[KO!] PLAYER 1 KO on {currentMap.Name}!");
            }
            if (p2Obj.X < blastZone.X1 || p2Obj.X > blastZone.X2 || p2Obj.Y < blastZone.Y1 || p2Obj.Y > blastZone.Y2)
            {
                NativeEngine.PhysicsRemoveObject(physics, player2ObjId);
                player2ObjId = NativeEngine.PhysicsAddObject(physics, currentMap.P2Spawn.X, currentMap.P2Spawn.Y, 40.0f, 70.0f, 100.0f);
                stateMachine2.Reset();
                Console.WriteLine($"[KO!] PLAYER 2 KO on {currentMap.Name}!");
            }

            // Update Native Camera tracking targets
            Point<float>[] targets =
            [
                new Point<float>(p1Obj.X, p1Obj.Y - p1Obj.Height / 2.0f),
                new Point<float>(p2Obj.X, p2Obj.Y - p2Obj.Height / 2.0f)
            ];

            float camX, camY, camScale;
            unsafe
            {
                fixed (Point<float>* pTargets = targets)
                {
                    NativeEngine.CameraUpdate(camera, deltaTime, pTargets, 2, Width, Height);
                }
                NativeEngine.CameraGetState(camera, &camX, &camY, &camScale);
            }

            // Start frame drawing
            NativeEngine.BeginFrame();

            // Set Camera transform for world objects
            render.SetCameraState(camX, camY, camScale);
            render.SetCameraEnabled(true);

            // Render Blast Zone boundary box
            render.DrawBox(new Rect<int>((int)blastZone.X1, (int)blastZone.Y1, (int)blastZone.X2, (int)blastZone.Y2), 0xFF333333, false);

            // Render Current Map Platforms
            foreach (var plat in currentMap.Platforms)
            {
                int px1 = (int)plat.X1;
                int py1 = (int)plat.Y1;
                int px2 = (int)plat.X2;
                int py2 = (int)plat.Y2;

                // Fill
                render.DrawBox(new Rect<int>(px1, py1, px2, py2), plat.FillColor, true);
                // Outline
                render.DrawBox(new Rect<int>(px1, py1, px2, py2), plat.OutlineColor, false);

                // Semi-solid platform top indicator line
                if (plat.Type == NativeEngine.PlatformType.SemiSolid)
                {
                    render.DrawBox(new Rect<int>(px1, py1, px2, py1 + 2), 0xFFFFFFFF, true);
                }
            }

            // Draw Player 1 (Blue palette)
            int p1X1 = (int)(p1Obj.X - p1Obj.Width / 2.0f);
            int p1Y1 = (int)(p1Obj.Y - p1Obj.Height);
            int p1X2 = (int)(p1Obj.X + p1Obj.Width / 2.0f);
            int p1Y2 = (int)p1Obj.Y;
            uint p1Color = (stateMachine1.State == CharacterState.Hitstun) ?
                (((int)(p1Obj.HitstunTimer * 10) % 2 == 0) ? 0xFFE74C3C : 0xFFFFFFFF) :
                ((stateMachine1.State == CharacterState.Grounded) ? 0xFF3498DB : 0xFF2980B9);

            render.DrawBox(new Rect<int>(p1X1, p1Y1, p1X2, p1Y2), p1Color, true);
            render.DrawBox(new Rect<int>(p1X1, p1Y1, p1X2, p1Y2), 0xFF1F618D, false);

            // Draw Player 2 (Purple/Red palette)
            int p2X1 = (int)(p2Obj.X - p2Obj.Width / 2.0f);
            int p2Y1 = (int)(p2Obj.Y - p2Obj.Height);
            int p2X2 = (int)(p2Obj.X + p2Obj.Width / 2.0f);
            int p2Y2 = (int)p2Obj.Y;
            uint p2Color = (stateMachine2.State == CharacterState.Hitstun) ?
                (((int)(p2Obj.HitstunTimer * 10) % 2 == 0) ? 0xFFE74C3C : 0xFFFFFFFF) :
                ((stateMachine2.State == CharacterState.Grounded) ? 0xFF9B59B6 : 0xFF8E44AD);

            render.DrawBox(new Rect<int>(p2X1, p2Y1, p2X2, p2Y2), p2Color, true);
            render.DrawBox(new Rect<int>(p2X1, p2Y1, p2X2, p2Y2), 0xFF5B2C6F, false);

            // Disable Camera transform for HUD & UI
            render.SetCameraEnabled(false);

            // Title & Map Status Header
            int lineSpacing = 22;
            render.DrawString(new Point<int>(30, 15), $"=== {currentMap.Name} ===", currentMap.ThemeColor);
            render.DrawString(new Point<int>(30, 37), $"{currentMap.Subtitle}", 0xFFCCCCCC);
            render.DrawString(new Point<int>(30, 59), $"Camera: ({camX:F1}, {camY:F1}) | Zoom: {camScale:F2}x | Dist: {Math.Abs(p1Obj.X - p2Obj.X):F1}px", 0xFF00FF00);

            // Player 1 HUD (Left)
            int p1HudX = 30;
            int p1HudY = 90;
            render.DrawString(new Point<int>(p1HudX, p1HudY), "[ PLAYER 1 ]", 0xFF3498DB);
            render.DrawString(new Point<int>(p1HudX, p1HudY + lineSpacing), $"Damage: {p1Obj.DamagePercent:F1}%", 0xFFE74C3C);
            render.DrawString(new Point<int>(p1HudX, p1HudY + lineSpacing * 2), $"Pos: ({p1Obj.X:F1}, {p1Obj.Y:F1})", 0xFFDDDDDD);
            render.DrawString(new Point<int>(p1HudX, p1HudY + lineSpacing * 3), $"Vel: ({p1Obj.Vx:F1}, {p1Obj.Vy:F1})", 0xFFDDDDDD);
            render.DrawString(new Point<int>(p1HudX, p1HudY + lineSpacing * 4), $"State: {stateMachine1.State}", 0xFF00FF00);

            // Player 2 HUD (Middle)
            int p2HudX = 360;
            int p2HudY = 90;
            render.DrawString(new Point<int>(p2HudX, p2HudY), "[ PLAYER 2 ]", 0xFF9B59B6);
            render.DrawString(new Point<int>(p2HudX, p2HudY + lineSpacing), $"Damage: {p2Obj.DamagePercent:F1}%", 0xFFE74C3C);
            render.DrawString(new Point<int>(p2HudX, p2HudY + lineSpacing * 2), $"Pos: ({p2Obj.X:F1}, {p2Obj.Y:F1})", 0xFFDDDDDD);
            render.DrawString(new Point<int>(p2HudX, p2HudY + lineSpacing * 3), $"Vel: ({p2Obj.Vx:F1}, {p2Obj.Vy:F1})", 0xFFDDDDDD);
            render.DrawString(new Point<int>(p2HudX, p2HudY + lineSpacing * 4), $"State: {stateMachine2.State}", 0xFF00FF00);

            // Controls & Map Switch Panel (Right)
            int helpTextX = Width - 480;
            int helpTextY = 15;
            render.DrawString(new Point<int>(helpTextX, helpTextY), "--- Controls & Map Selector ---", 0xFFFFFFFF);
            render.DrawString(new Point<int>(helpTextX, helpTextY + lineSpacing), "P1: A/D (Move) | W/Space (Jump) | S (FastFall)", 0xFF3498DB);
            render.DrawString(new Point<int>(helpTextX, helpTextY + lineSpacing * 2), "P2: Left/Right (Move) | Up (Jump) | Down (FastFall)", 0xFF9B59B6);
            render.DrawString(new Point<int>(helpTextX, helpTextY + lineSpacing * 3), "F: Hit P1 | G: Hit P2 | R: Reset Players", 0xFFFF8888);
            render.DrawString(new Point<int>(helpTextX, helpTextY + lineSpacing * 4), "1-4: Switch Stages In Real-Time", 0xFFFFFF00);
            render.DrawString(new Point<int>(helpTextX, helpTextY + lineSpacing * 5), "ESC: Exit Game", 0xFF888888);

            // Swap buffers
            NativeEngine.EndFrame(deltaTime);
        }

        Console.WriteLine("Cleaning up Native Camera...");
        NativeEngine.CameraDestroy(camera);

        Console.WriteLine("Cleaning up Physics...");
        NativeEngine.PhysicsDestroy(physics);

        Console.WriteLine("Shutting down Native Engine...");
        NativeEngine.Shutdown();
    }
}
