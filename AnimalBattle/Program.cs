using System;
using System.Diagnostics;
using AnimalBattle.Core;
using AnimalBattle.Scenes;

namespace AnimalBattle;

/// <summary>
/// アプリケーションのエントリポイントおよびメインループを管理するクラス。
/// </summary>
internal class Program
{
    private static void Main(string[] args)
    {
        const int Width = 1280;
        const int Height = 720;

        Console.WriteLine("C++ DxLib ネイティブエンジンを初期化中...");
        if (GameEngine.Initialize(Width, Height, true) == -1)
        {
            Console.WriteLine("ゲームエンジンの初期化に失敗しました！");
            return;
        }

        IntPtr nativeRenderService = GameEngine.GetRenderService();
        var render = new RenderService(nativeRenderService);

        Console.WriteLine("物理エンジンを初期化中...");
        using var physicsEngine = new PhysicsEngine();

        using var battleScene = new BattleScene(render, physicsEngine);
        battleScene.Initialize();

        var stopwatch = Stopwatch.StartNew();
        long lastTime = stopwatch.ElapsedTicks;
        double tickFrequency = (double)Stopwatch.Frequency;

        Console.WriteLine("メインゲームループを開始します...");
        while (GameEngine.ProcessMessage() == 0 && !GameEngine.IsExitRequested())
        {
            long currentTime = stopwatch.ElapsedTicks;
            float deltaTime = (float)((currentTime - lastTime) / tickFrequency);
            lastTime = currentTime;

            GameEngine.BeginFrame();

            if (!battleScene.Update(deltaTime))
            {
                break;
            }

            battleScene.Render();

            GameEngine.EndFrame(deltaTime);
        }

        Console.WriteLine("ネイティブエンジンを終了中...");
        GameEngine.Shutdown();

        // ウィンドウクローズ時にプロセスが確実に完全終了するように明示的に呼び出し
        Environment.Exit(0);
    }
}
