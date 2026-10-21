using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace AnimalBattle.Core;

/// <summary>
///     ゲーム論理更新（Flecs ECS、物理・弾幕計算）と DxLib 描画提示（WinMsg、Submit、ScreenFlip/VSync）を
///     真の 2 段パイプライン（Pipelined Double Buffering）で完全並行実行する双スレッドゲームループ協調クラス。
///     論理スレッドが次フレーム (N+1) をバックバッファへ記録するのと並行して、
///     メインスレッドが前フレーム (N) のフロントバッファを GPU/DxLib へ提示します。
/// </summary>
public sealed class DualThreadGameLoop : IDisposable {
    // 起動時の第 0 プリロールフレーム準備完了通知
    private readonly AutoResetEvent _firstFrameReadyEvent = new(false);
    // 論理スレッドによる次フレームのバックバッファ記録完了通知 (Logic -> Main)
    private readonly AutoResetEvent _logicFrameDoneEvent = new(false);
    // メインスレッドによるフロントバッファ消費＆SwapBuffers完了通知 (Main -> Logic)
    private readonly AutoResetEvent _nextLogicFrameEvent = new(false);

    private Thread? _logicThread;
    private volatile bool _isRunning = true;
    private volatile Exception? _fatalException;
    private bool _isDisposed;

    /// <summary>
    ///     メインスレッド上で双スレッドパイプラインループを開始します。
    ///     メッセージループ処理と DxLib の描画提示を統括し、論理スレッドが正常終了または例外発生するまでブロックします。
    /// </summary>
    /// <param name="gameManager">実行対象のバトルシーン。</param>
    public void Run(GameManager gameManager)
    {
        Debug.Assert(!_isDisposed, "[DualThreadGameLoop] 破棄済みのループを開始することはできません。");
        if (_isDisposed) return;

        _isRunning = true;
        _fatalException = null;

        _logicThread = new Thread(() => LogicThreadEntry(gameManager)) {
            Name = "GameLogicThread",
            Priority = ThreadPriority.Normal,
            IsBackground = true
        };
        _logicThread.Start();

        var frameCount = 0;
        try
        {
            // 1. パイプライン充填：第 0 プリロールフレームの記録および初回 Swap 完了を待機
            while (_isRunning)
            {
                if (_firstFrameReadyEvent.WaitOne(100))
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
                return;
            }

            // 2. 定常パイプライン並行ループ
            // メインスレッドが前フレーム (N) を描画・提示している間に、論理スレッドは並行して次フレーム (N+1) を記録します。
            while (_isRunning)
            {
                // A. Windows メッセージポンプおよび DirectInput 状態更新 (メインスレッド専任)
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

                // B. DxLib 描画パイプライン実行 (メインスレッド専任：Front Buffer 描画＆VSync)
                // ※ この間、論理スレッドはバックグラウンドで並行して次フレームの ECS / 物理 / 描画記録を実行中
                GameEngine.BeginDraw();
                GameEngine.SubmitFront();
                GameEngine.EndDraw();

                frameCount++;

                // C. フレーム境界バリア同期：論理スレッドによるバックバッファ記録完了を合流待機
                while (_isRunning)
                {
                    if (_logicFrameDoneEvent.WaitOne(100))
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

                // D. 双方が完了した安全な同期ポイントでバッファ切り替え（メインスレッドが一意に所有）
                // 論理スレッドが記録した BackBuffer が次フレームの FrontBuffer となり、
                // 今回描画し終えた旧 FrontBuffer が次フレームの BackBuffer として再利用可能になります。
                GameEngine.SwapBuffers();

                // E. 次フレームの論理更新を開始するよう論理スレッドを起床
                _nextLogicFrameEvent.Set();
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

    private void LogicThreadEntry(GameManager scene)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var lastTime = stopwatch.ElapsedTicks;
            var tickFrequency = (double)Stopwatch.Frequency;

            // ---------------------------------------------------------
            // パイプライン立ち上げ：第 0 プリロールフレームの生成
            // ---------------------------------------------------------
            GameEngine.BeginRecording();
            scene.Update(0.0f);
            GameEngine.FinishRecording();

            // 第 0 フレームデータを FrontBuffer へ昇格させ、メインスレッドへ描画開始を許可
            GameEngine.SwapBuffers();
            _firstFrameReadyEvent.Set();

            // ---------------------------------------------------------
            // 定常並行パイプラインループ (フレーム N+1 の先行記録)
            // ---------------------------------------------------------
            while (_isRunning)
            {
                var currentTime = stopwatch.ElapsedTicks;
                var rawDeltaTime = (float)((currentTime - lastTime) / tickFrequency);
                lastTime = currentTime;

                // 最大単一ステップを設定値に制限
                const float maxDeltaLimit = 0.05f;
                var deltaTime = Math.Clamp(rawDeltaTime, 0.0f, maxDeltaLimit);

                // 1. 論理更新および次フレーム描画コマンドのバックバッファ記録
                // ※ メインスレッドが前フレームのフロントバッファを描画・VSync しているのと完全並行に動作
                GameEngine.BeginRecording();
                scene.Update(deltaTime);
                GameEngine.FinishRecording();

                // 2. メインスレッドへ「次フレームのバックバッファ記録完了」を通知
                _logicFrameDoneEvent.Set();

                // 3. メインスレッドが前フレームの描画提示を終え、SwapBuffers を完了するのを待機
                while (_isRunning)
                {
                    if (_nextLogicFrameEvent.WaitOne(100))
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
            }
        }
        catch (Exception ex)
        {
            _fatalException = ex;
            _isRunning = false;
            _firstFrameReadyEvent.Set(); // メインスレッド待機解除
            _logicFrameDoneEvent.Set();  // メインスレッド待機解除
        }
    }

    private void RequestShutdown()
    {
        _isRunning = false;
        _firstFrameReadyEvent.Set(); // 待機中のメインスレッドを確実に起床
        _logicFrameDoneEvent.Set();  // 待機中のメインスレッドを確実に起床
        _nextLogicFrameEvent.Set();  // 待機中の論理スレッドを確実に起床
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
        _firstFrameReadyEvent.Dispose();
        _logicFrameDoneEvent.Dispose();
        _nextLogicFrameEvent.Dispose();
        _isDisposed = true;
    }
}
