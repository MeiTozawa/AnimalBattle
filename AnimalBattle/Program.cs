using AnimalBattle.Core;
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

            Debug.WriteLine("双スレッドロックステップパイプラインループを開始します...");
            using DualThreadGameLoop gameLoop = new();
            gameLoop.Run(battleScene);
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