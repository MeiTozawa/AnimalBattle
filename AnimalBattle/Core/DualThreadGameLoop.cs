using System.Diagnostics;
using System.Runtime.ExceptionServices;
using AnimalBattle.Scenes;

namespace AnimalBattle.Core;

/// <summary>
///     ゲーム論理更新（Flecs ECS、物理・弾幕計算）と DxLib 描画提示（WinMsg、Submit、ScreenFlip/VSync）を
///     完全並行にパイプライン実行するロックステップ双スレッドゲームループ協調クラス。
/// </summary>
public sealed class DualThreadGameLoop : IDisposable {
    private readonly AutoResetEvent _frameReadyEvent = new(false);
    private readonly AutoResetEvent _frameConsumedEvent = new(false);
    private Thread? _logicThread;
    private volatile bool _isRunning = true;
    private volatile Exception? _fatalException;
    private bool _isDisposed;

    /// <summary>
    ///     メインスレッド上で双スレッドパイプラインループを開始します。
    ///     メッセージループ処理と DxLib の描画提示を統括し、論理スレッドが正常終了または例外発生するまでブロックします。
    /// </summary>
    /// <param name="battleScene">実行対象のバトルシーン。</param>
    public void Run(BattleScene battleScene)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        _isRunning = true;
        _fatalException = null;

        _logicThread = new Thread(() => LogicThreadEntry(battleScene)) {
            Name = "GameLogicThread",
            Priority = ThreadPriority.Normal,
            IsBackground = true
        };
        _logicThread.Start();

        var frameCount = 0;
        try
        {
            while (_isRunning)
            {
                // 1. Windows メッセージポンプおよび DirectInput 状態更新 (メインスレッド専任)
                if (!GameEngine.ProcessMessage())
                {
                    Debug.WriteLine($"[終了] ウィンドウメッセージ処理が終了を返しました (フレーム数: {frameCount})");
                    RequestShutdown();
                    break;
                }

                if (GameEngine.IsExitRequested())
                {
                    Debug.WriteLine($"[終了] アプリケーション終了要求を受信しました (フレーム数: {frameCount})");
                    RequestShutdown();
                    break;
                }

                // 2. 論理スレッドによるフレーム記録完了を待機（100ms 短期ポーリングでデッドロック防止）
                while (_isRunning)
                {
                    if (_frameReadyEvent.WaitOne(100))
                    {
                        break;
                    }

                    if (!_isRunning || _fatalException != null || GameEngine.IsExitRequested())
                    {
                        break;
                    }
                }

                if (!_isRunning || _fatalException != null)
                {
                    break;
                }

                // 3. DxLib 描画パイプライン実行 (メインスレッド専任)
                GameEngine.BeginDraw();
                GameEngine.SubmitFront();
                GameEngine.EndDraw();

                frameCount++;

                // 4. 前台バッファの消費完了を論理スレッドへ通知（スロットの再利用を許可）
                _frameConsumedEvent.Set();
            }
        }
        finally
        {
            RequestShutdown();

            if (_logicThread != null && _logicThread.IsAlive)
            {
                _logicThread.Join(2000);
            }

            if (_fatalException != null)
            {
                ExceptionDispatchInfo.Capture(_fatalException).Throw();
            }
        }
    }

    private void LogicThreadEntry(BattleScene scene)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var lastTime = stopwatch.ElapsedTicks;
            var tickFrequency = (double)Stopwatch.Frequency;

            while (_isRunning)
            {
                var currentTime = stopwatch.ElapsedTicks;
                var rawDeltaTime = (float)((currentTime - lastTime) / tickFrequency);
                lastTime = currentTime;

                // 産業級メインループ防トンネル保護：最大単一ステップを 50ms (20 FPS 相当) に制限
                var deltaTime = Math.Clamp(rawDeltaTime, 0.0f, 0.05f);

                // 1. 論理更新および描画コマンド記録
                GameEngine.BeginRecording();
                scene.Update(deltaTime);
                scene.Render();
                GameEngine.FinishRecording();

                // 2. メインスレッドへ描画準備完了を通知
                _frameReadyEvent.Set();

                // 3. 前フレームのバッファがメインスレッドによって消費・反転されるのを待機
                while (_isRunning)
                {
                    if (_frameConsumedEvent.WaitOne(100))
                    {
                        break;
                    }

                    if (!_isRunning || GameEngine.IsExitRequested())
                    {
                        break;
                    }
                }

                if (!_isRunning)
                {
                    break;
                }

                // 4. バッファスロットの乒乓切り替え
                GameEngine.SwapBuffers();
            }
        }
        catch (Exception ex)
        {
            _fatalException = ex;
            _isRunning = false;
            _frameReadyEvent.Set(); // メインスレッドの待機を直ちに解除
        }
    }

    private void RequestShutdown()
    {
        _isRunning = false;
        _frameConsumedEvent.Set(); // 待機中の論理スレッドを確実に起床
        _frameReadyEvent.Set();    // 待機中のメインスレッドを確実に起床
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed) return;
        RequestShutdown();
        if (_logicThread != null && _logicThread.IsAlive)
        {
            _logicThread.Join(2000);
        }
        _frameReadyEvent.Dispose();
        _frameConsumedEvent.Dispose();
        _isDisposed = true;
    }
}
