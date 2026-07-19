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

        var stopwatch = Stopwatch.StartNew();
        long lastTime = stopwatch.ElapsedTicks;
        double tickFrequency = (double)Stopwatch.Frequency;

        const int KEY_INPUT_ESCAPE = 0x01;

        Console.WriteLine("Entering Game Loop...");
        while (NativeEngine.ProcessMessage() == 0 && !NativeEngine.IsExitRequested())
        {
            long currentTime = stopwatch.ElapsedTicks;
            float deltaTime = (float)((currentTime - lastTime) / tickFrequency);
            lastTime = currentTime;

            // Exit on Escape key
            if (NativeEngine.CheckHitKey(KEY_INPUT_ESCAPE) != 0)
            {
                break;
            }

            // Get mouse position
            int mouseX = 0;
            int mouseY = 0;
            unsafe
            {
                NativeEngine.GetMousePoint(&mouseX, &mouseY);
            }

            // Start frame drawing
            NativeEngine.BeginFrame();

            // Render a blue box
            const float boxX1 = 100f;
            const float boxY1 = 100f;
            const float boxX2 = 600f;
            const float boxY2 = 400f;
            render.DrawBoxAA(boxX1, boxY1, boxX2, boxY2, 0xFF0000AA, true); // Blue box

            // Cursor Hover Test: Change cursor to hand pointer if hovering over the blue box
            if (mouseX >= boxX1 && mouseX <= boxX2 && mouseY >= boxY1 && mouseY <= boxY2)
            {
                NativeEngine.SetCursorPointer();
            }
            else
            {
                NativeEngine.SetCursorArrow();
            }

            // Draw text
            render.DrawString(120, 150, "Hello from dotnet 10!", 0xFFFFFFFF); // White text
            render.DrawString(120, 200, $"Mouse Pos: ({mouseX}, {mouseY})", 0xFFFF00FF); // Purple text
            render.DrawString(120, 250, "Hover over the blue box to change cursor!", 0xFF00FF00); // Green text
            render.DrawString(120, 300, "Press ESC to exit.", 0xFF808080); // Gray text

            // Finish frame & Swap buffers
            NativeEngine.EndFrame(deltaTime);
        }

        Console.WriteLine("Shutting down Native Engine...");
        NativeEngine.Shutdown();
    }
}
