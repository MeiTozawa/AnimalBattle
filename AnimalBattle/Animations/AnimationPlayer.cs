using System;
using AnimalBattle.Bindings;

namespace AnimalBattle.Animations;

/// <summary>
/// Native C++ 側の <c>AnimationPlayer</c> インスタンスを安全に保持・操作する C# ラッパークラス。
/// スプライトシートアニメーションの再生制御とパラメータ伝達を担当します。
/// </summary>
public sealed class AnimationPlayer : IDisposable
{
    private IntPtr _nativePtr;
    private bool _isDisposed;

    /// <summary>
    /// 新しい <see cref="AnimationPlayer"/> インスタンスを生成します。
    /// </summary>
    /// <exception cref="InvalidOperationException">Native インスタンスの生成に失敗した場合に発生します。</exception>
    public AnimationPlayer()
    {
        _nativePtr = NativeEngine.AnimationPlayerCreate();
        if (_nativePtr == IntPtr.Zero)
        {
            throw new InvalidOperationException("Native C++ AnimationPlayer のインスタンス生成に失敗しました。");
        }
    }

    /// <summary>
    /// デストラクタ。アンマネージリソースを破棄します。
    /// </summary>
    ~AnimationPlayer()
    {
        Dispose(false);
    }

    /// <summary>
    /// 非ループアニメーションが再生完了したかを取得します。
    /// </summary>
    public bool IsFinished
    {
        get
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            return NativeEngine.AnimationPlayerIsFinished(_nativePtr);
        }
    }

    /// <summary>
    /// 現在表示中のコマインデックス (0 開始) を取得します。
    /// </summary>
    public int CurrentFrameIndex
    {
        get
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            return NativeEngine.AnimationPlayerGetCurrentFrameIndex(_nativePtr);
        }
    }

    /// <summary>
    /// スプライトシートの格子状領域からアニメーションクリップを登録します。
    /// </summary>
    /// <param name="name">アニメーション識別名</param>
    /// <param name="row">行インデックス (0 開始)</param>
    /// <param name="frameWidth">1 コマの幅 (ピクセル)</param>
    /// <param name="frameHeight">1 コマの高さ (ピクセル)</param>
    /// <param name="frameCount">コマ数</param>
    /// <param name="frameDuration">1 コマの表示維持時間 (秒)</param>
    /// <param name="isLooping">ループ再生する場合は true</param>
    public void AddGridClip(string name, int row, int frameWidth, int frameHeight, int frameCount, float frameDuration = 0.1f, bool isLooping = true)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        NativeEngine.AnimationPlayerAddGridClip(_nativePtr, name, row, frameWidth, frameHeight, frameCount, frameDuration, isLooping);
    }

    /// <summary>
    /// 指定された名前のアニメーション再生を開始します。
    /// </summary>
    /// <param name="name">再生するアニメーション名</param>
    /// <param name="restartIfSame">既に同じアニメーションが再生中でも最初から再開するか</param>
    /// <returns>再生開始に成功した場合は true</returns>
    public bool Play(string name, bool restartIfSame = false)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        return NativeEngine.AnimationPlayerPlay(_nativePtr, name, restartIfSame);
    }

    /// <summary>
    /// アニメーション再生を一時停止します。
    /// </summary>
    public void Pause()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        NativeEngine.AnimationPlayerPause(_nativePtr);
    }

    /// <summary>
    /// 一時停止中のアニメーション再生を再開します。
    /// </summary>
    public void Resume()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        NativeEngine.AnimationPlayerResume(_nativePtr);
    }

    /// <summary>
    /// アニメーション再生を停止し、最初のフレームにリセットします。
    /// </summary>
    public void Stop()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        NativeEngine.AnimationPlayerStop(_nativePtr);
    }

    /// <summary>
    /// 時間の経過に応じて C++ ネイティブアニメーションの状態を更新します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過秒数</param>
    public void Update(float deltaTime)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        NativeEngine.AnimationPlayerUpdate(_nativePtr, deltaTime);
    }

    /// <summary>
    /// 現在のアニメーションコマを指定された位置・パラメータで描画します。
    /// </summary>
    /// <param name="renderService">描画サービスインスタンス</param>
    /// <param name="position">描画座標 (中心位置)</param>
    /// <param name="graphHandle">スプライトシートのグラフィックハンドル</param>
    /// <param name="flipX">左右反転表示する場合は true</param>
    /// <param name="scale">表示倍率 (デフォルト 1.0)</param>
    /// <param name="angle">回転角度 (ラジアン)</param>
    public void Draw(RenderService renderService, Point<int> position, int graphHandle, bool flipX = false, float scale = 1.0f, double angle = 0.0)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        ArgumentNullException.ThrowIfNull(renderService, nameof(renderService));
        NativeEngine.AnimationPlayerDraw(_nativePtr, renderService.NativePtr, position.X, position.Y, graphHandle, flipX, scale, angle);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// アンマネージドリソース（および必要に応じてマネージドリソース）を解放します。
    /// </summary>
    /// <param name="disposing">マネージドリソースとアンマネージドリソースの両方を解放する場合は true。アンマネージドリソースのみを解放する場合は false。</param>
    private void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                // マネージドリソースの解放処理（現時点では保持するマネージドリソースなし）
            }

            if (_nativePtr != IntPtr.Zero)
            {
                NativeEngine.AnimationPlayerDestroy(_nativePtr);
                _nativePtr = IntPtr.Zero;
            }

            _isDisposed = true;
        }
    }
}
