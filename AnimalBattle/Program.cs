using AnimalBattle.Scenes;

namespace AnimalBattle;

/// <summary>
///     アプリケーションのエントリポイントおよびメインループを管理するクラス。
/// </summary>
internal static class Program {
    private static void Main(string[] args)
    {
        try
        {
            const int width = 1280;
            const int height = 720;

            Debug.WriteLine("C++ DxLib ネイティブエンジンを初期化中...");
            GameEngine.Initialize(width, height);

            Debug.WriteLine("Flecs.NET ワールドおよびバトルシーンを初期化中...");
            using BattleScene battleScene = new();
            battleScene.Initialize();

            var stopwatch = Stopwatch.StartNew();
            var lastTime = stopwatch.ElapsedTicks;
            var tickFrequency = (double)Stopwatch.Frequency;

            Debug.WriteLine("メインゲームループを開始します...");
            var frameCount = 0;
            while (true)
            {
                if (!GameEngine.ProcessMessage())
                {
                    Debug.WriteLine($"[終了] ウィンドウメッセージ処理が終了を返しました (フレーム数: {frameCount})");
                    break;
                }

                if (GameEngine.IsExitRequested())
                {
                    Debug.WriteLine($"[終了] アプリケーション終了要求を受信しました (フレーム数: {frameCount})");
                    break;
                }

                var currentTime = stopwatch.ElapsedTicks;
                var rawDeltaTime = (float)((currentTime - lastTime) / tickFrequency);
                lastTime = currentTime;

                // 産業級メインループ防トンネル保護：最大単一ステップを 50ms (20 FPS 相当) に制限し、ウィンドウドラッグやフレーム落ちによる壁抜けを防止
                var deltaTime = Math.Clamp(rawDeltaTime, 0.0f, 0.05f);

                GameEngine.BeginFrame();

                battleScene.Update(deltaTime);
                battleScene.Render();

                GameEngine.EndFrame(deltaTime);
                frameCount++;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[致命的クラッシュ例外]:\n{ex}");
        }
        finally
        {
            Debug.WriteLine("ネイティブエンジンを終了中...");
            GameEngine.Shutdown();
        }

        Environment.Exit(0);
    }
}