using AnimalBattle.Core;

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

            GameEngine.Initialize(width, height);

            using GameManager gameManager = new();
            gameManager.Initialize();

            using DualThreadGameLoop gameLoop = new();
            gameLoop.Run(gameManager);
        }
        catch (Exception ex)
        {
            Debug.Fail($"[致命的クラッシュ例外]: {ex.Message}", ex.ToString());
            return;
        }
        finally
        {
            Debug.WriteLine("ネイティブエンジンを終了中...");
            GameEngine.Shutdown();
        }

        Environment.Exit(0);
    }
}