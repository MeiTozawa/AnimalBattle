using AnimalBattle.Core;

namespace AnimalBattle;

/// <summary>
///     アプリケーションのエントリポイントおよびメインループを管理するクラス。
/// </summary>
internal static class Program {
    private static void Main(string[] args)
    {
        var exitCode = 0;
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
            exitCode = 1;
            var crashLog = $"""
                ================================================================================
                [致命的クラッシュレポート]
                発生日時 (UTC): {DateTime.UtcNow:O}
                プロセス: {Environment.ProcessPath}
                OS バージョン: {Environment.OSVersion}
                64bit プロセス: {Environment.Is64BitProcess}
                例外型: {ex.GetType().FullName}
                例外メッセージ: {ex.Message}
                --------------------------------------------------------------------------------
                スタックトレース:
                {ex}
                ================================================================================
                """;

            Console.Error.WriteLine(crashLog);

            try
            {
                var crashFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
                File.WriteAllText(crashFilePath, crashLog);
            }
            catch
            {
                // クラッシュログ保存時の副次例外は握りつぶして本体の終了処理を優先
            }

            Debug.Fail($"[致命的クラッシュ例外]: {ex.Message}", ex.ToString());
        }
        finally
        {
            Debug.WriteLine("ネイティブエンジンを終了中...");
            GameEngine.Shutdown();
        }

        Environment.Exit(exitCode);
    }
}