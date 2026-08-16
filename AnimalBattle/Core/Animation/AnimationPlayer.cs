using System;
using AnimalBattle.Bindings;

namespace AnimalBattle.Core;
/// <summary>
/// Native C++ アニメーションプレイヤー (AnimalBattleCore.dll) を P/Invoke 経由で操作し、
/// クリップ管理、フレームタイマー進行、および UV 切り出し矩形計算を提供するクラス。
/// </summary>
public class AnimationPlayer : IDisposable
{
    private IntPtr _nativePlayer;
    private bool _isDisposed;

    /// <summary>
    /// <see cref="AnimationPlayer"/> の新しいインスタンスを初期化し、Native プレイヤーオブジェクトを生成します。
    /// </summary>
    public AnimationPlayer()
    {
        _nativePlayer = NativeBindings.AnimationPlayer_Create();
    }

    /// <summary>
    /// アニメーションクリップを登録します。
    /// </summary>
    /// <param name="name">クリップ名</param>
    /// <param name="startFrame">開始コマインデックス</param>
    /// <param name="endFrame">終了コマインデックス</param>
    /// <param name="frameWidth">1コマの幅 (px)</param>
    /// <param name="frameHeight">1コマの高さ (px)</param>
    /// <param name="fps">再生フレームレート (FPS)</param>
    /// <param name="isLooping">ループ再生するかどうか</param>
    public void AddClip(string name, int startFrame, int endFrame, int frameWidth, int frameHeight, float fps = 6.0f, bool isLooping = true)
    {
        NativeBindings.AnimationPlayer_AddClip(_nativePlayer, name, startFrame, endFrame, frameWidth, frameHeight, fps, isLooping);
    }

    /// <summary>
    /// 指定されたクリップ名の再生を開始します。
    /// </summary>
    /// <param name="name">再生するクリップ名</param>
    public void Play(string name)
    {
        NativeBindings.AnimationPlayer_Play(_nativePlayer, name);
    }

    /// <summary>
    /// アニメーションの再生を停止します。
    /// </summary>
    public void Stop()
    {
        NativeBindings.AnimationPlayer_Stop(_nativePlayer);
    }

    /// <summary>
    /// アニメーションの進行時間をデルタタイム分進めます。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過秒数</param>
    public void Update(float deltaTime)
    {
        NativeBindings.AnimationPlayer_Update(_nativePlayer, deltaTime);
    }

    /// <summary>
    /// 現在再生中のスプライトシート切り出し領域 (UV SrcRect) を取得します。
    /// </summary>
    /// <returns>切り出し矩形</returns>
    public Rect<int> GetCurrentSrcRect()
    {
        NativeBindings.AnimationPlayer_GetCurrentSrcRect(_nativePlayer, out int l, out int t, out int r, out int b);
        return new Rect<int>(l, t, r, b);
    }

    /// <summary>
    /// ネイティブアニメーションプレイヤーリソースを解放します。
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            if (_nativePlayer != IntPtr.Zero)
            {
                NativeBindings.AnimationPlayer_Destroy(_nativePlayer);
                _nativePlayer = IntPtr.Zero;
            }
            _isDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
