using System.Diagnostics;
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

            RunGameLoop(gameManager);
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

    /// <summary>
    ///     単一スレッドでメッセージポンプ、論理更新、描画提出を直列実行するメインループ。
    ///     スレッド間同期オーバーヘッドおよび入力遅延を完全に排除します。
    /// </summary>
    private static void RunGameLoop(GameManager gameManager)
    {
        var stopwatch = Stopwatch.StartNew();
        var lastTime = stopwatch.ElapsedTicks;
        var tickFrequency = (double)Stopwatch.Frequency;

        while (true)
        {
            // 1. Windows メッセージおよび終了要求の処理
            if (!GameEngine.ProcessMessage() || GameEngine.IsExitRequested())
            {
                break;
            }

            // 2. 経過時間 (deltaTime) の計測
            var currentTime = stopwatch.ElapsedTicks;
            var rawDeltaTime = (float)((currentTime - lastTime) / tickFrequency);
            lastTime = currentTime;

            // 最大単一ステップを設定値に制限 (50ms)
            const float maxDeltaLimit = 0.05f;
            var deltaTime = Math.Clamp(rawDeltaTime, 0.0f, maxDeltaLimit);

            // 3. 入力スナップショットの取得と描画コマンド記録開始
            GameEngine.BeginRecording();

            // 4. ゲーム論理更新 (ECS システム、物理、弾幕計算、描画コマンド発行)
            gameManager.Update(deltaTime);

            // 5. 描画開始、コマンド一括提出、画面提示 (ScreenFlip)
            GameEngine.BeginDraw();
            GameEngine.Submit();
            GameEngine.EndDraw();
        }
    }
}
