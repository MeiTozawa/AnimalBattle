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
        try
        {
            const int width = 1280;
            const int height = 720;

            Console.WriteLine("C++ DxLib ネイティブエンジンを初期化中...");
            if (!GameEngine.Initialize(width, height, true))
            {
                Console.WriteLine("ゲームエンジンの初期化に失敗しました！");
                return;
            }

            Console.WriteLine("Flecs.NET ワールドおよびバトルシーンを初期化中...");
            using BattleScene battleScene = new BattleScene();
            battleScene.Initialize();

            Stopwatch stopwatch = Stopwatch.StartNew();
            long lastTime = stopwatch.ElapsedTicks;
            double tickFrequency = (double)Stopwatch.Frequency;

            Console.WriteLine("メインゲームループを開始します...");
            int frameCount = 0;
            while (true)
            {
                if (!GameEngine.ProcessMessage())
                {
                    Console.WriteLine($"[終了] ウィンドウメッセージ処理が終了を返しました (フレーム数: {frameCount})");
                    break;
                }

                if (GameEngine.IsExitRequested())
                {
                    Console.WriteLine($"[終了] アプリケーション終了要求を受信しました (フレーム数: {frameCount})");
                    break;
                }

                long currentTime = stopwatch.ElapsedTicks;
                float deltaTime = (float)((currentTime - lastTime) / tickFrequency);
                lastTime = currentTime;

                GameEngine.BeginFrame();

                if (!battleScene.Update(deltaTime))
                {
                    Console.WriteLine($"[終了] バトルシーンの更新が終了を要求しました (フレーム数: {frameCount})");
                    break;
                }

                battleScene.Render();

                GameEngine.EndFrame(deltaTime);
                frameCount++;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[致命的クラッシュ例外]:\n{ex}");
        }
        finally
        {
            Console.WriteLine("ネイティブエンジンを終了中...");
            GameEngine.Shutdown();
        }

        Environment.Exit(0);
    }
}
