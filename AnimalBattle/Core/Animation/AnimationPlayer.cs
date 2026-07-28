using System;
using System.Runtime.InteropServices;

namespace AnimalBattle.Core;

/// <summary>
/// Native C++ 側の <c>AnimationPlayer</c> インスタンスを安全に保持・操作する C# ラッパークラス。
/// スプライトシートアニメーションの再生制御とパラメータ伝達を担当します。
/// </summary>
public sealed partial class AnimationPlayer : IDisposable
{
    private const string _dllName = "AnimalBattleCore.dll";

    private IntPtr _nativePtr;
    private bool _isDisposed;

    /// <summary>
    /// 新しい <see cref="AnimationPlayer"/> インスタンスを生成します。
    /// </summary>
    public AnimationPlayer()
    {
        _nativePtr = NativeAnimationPlayerCreate();
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
    public bool IsFinished => NativeAnimationPlayerIsFinished(_nativePtr);

    /// <summary>
    /// 現在表示中のコマインデックス (0 開始) を取得します。
    /// </summary>
    public int CurrentFrameIndex => NativeAnimationPlayerGetCurrentFrameIndex(_nativePtr);

    /// <summary>
    /// スプライトシートの格子状領域からアニメーションクリップを登録します。
    /// </summary>
    public void AddGridClip(string name, int row, int frameWidth, int frameHeight, int frameCount, float fps = 10.0f, bool isLooping = true)
    {
        NativeAnimationPlayerAddGridClip(_nativePtr, name, row, frameWidth, frameHeight, frameCount, fps, isLooping);
    }

    /// <summary>
    /// 指定された名前のアニメーション再生を開始します。
    /// </summary>
    public bool Play(string name, bool restartIfSame = false)
    {
        return NativeAnimationPlayerPlay(_nativePtr, name, restartIfSame);
    }

    /// <summary>
    /// アニメーション再生を一時停止します。
    /// </summary>
    public void Pause()
    {
        NativeAnimationPlayerPause(_nativePtr);
    }

    /// <summary>
    /// 一時停止中のアニメーション再生を再開します。
    /// </summary>
    public void Resume()
    {
        NativeAnimationPlayerResume(_nativePtr);
    }

    /// <summary>
    /// アニメーション再生を停止し、最初のフレームにリセットします。
    /// </summary>
    public void Stop()
    {
        NativeAnimationPlayerStop(_nativePtr);
    }

    /// <summary>
    /// 時間の経過に応じて C++ ネイティブアニメーションの状態を更新します。
    /// </summary>
    public void Update(float deltaTime)
    {
        NativeAnimationPlayerUpdate(_nativePtr, deltaTime);
    }

    /// <summary>
    /// 現在のアニメーションコマを指定された位置・パラメータで描画します。
    /// </summary>
    public void Draw(RenderService renderService, Point<int> position, int graphHandle, bool flipX = false, float scale = 1.0f, double angle = 0.0)
    {
        NativeAnimationPlayerDraw(_nativePtr, renderService.NativePtr, position.X, position.Y, graphHandle, flipX, scale, angle);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (_nativePtr != IntPtr.Zero)
            {
                NativeAnimationPlayerDestroy(_nativePtr);
                _nativePtr = IntPtr.Zero;
            }
            _isDisposed = true;
        }
    }

    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_Create")]
    private static partial IntPtr NativeAnimationPlayerCreate();

    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_Destroy")]
    private static partial void NativeAnimationPlayerDestroy(IntPtr player);

    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_AddGridClip", StringMarshalling = StringMarshalling.Utf16)]
    private static partial void NativeAnimationPlayerAddGridClip(
        IntPtr player,
        string name,
        int row,
        int frameWidth,
        int frameHeight,
        int frameCount,
        float fps,
        [MarshalAs(UnmanagedType.U1)] bool isLooping);

    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_Play", StringMarshalling = StringMarshalling.Utf16)]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool NativeAnimationPlayerPlay(IntPtr player, string name, [MarshalAs(UnmanagedType.U1)] bool restartIfSame);

    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_Pause")]
    [SuppressGCTransition]
    private static partial void NativeAnimationPlayerPause(IntPtr player);

    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_Resume")]
    [SuppressGCTransition]
    private static partial void NativeAnimationPlayerResume(IntPtr player);

    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_Stop")]
    [SuppressGCTransition]
    private static partial void NativeAnimationPlayerStop(IntPtr player);

    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_Update")]
    [SuppressGCTransition]
    private static partial void NativeAnimationPlayerUpdate(IntPtr player, float deltaTime);

    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_Draw")]
    [SuppressGCTransition]
    private static partial void NativeAnimationPlayerDraw(
        IntPtr player,
        IntPtr service,
        int posX,
        int posY,
        int graphHandle,
        [MarshalAs(UnmanagedType.U1)] bool flipX,
        float scale,
        double angle);

    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_IsFinished")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool NativeAnimationPlayerIsFinished(IntPtr player);

    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_GetCurrentFrameIndex")]
    [SuppressGCTransition]
    private static partial int NativeAnimationPlayerGetCurrentFrameIndex(IntPtr player);
}
