using System.Runtime.InteropServices;

namespace AnimalBattle.Core;

/// <summary>
/// Native C++ カメラモジュール (Camera) のライフサイクル、P/Invoke バインディング、および制御操作をカプセル化する C# クラス。
/// </summary>
public unsafe partial class Camera : IDisposable
{
    private const string _dllName = "AnimalBattleCore.dll";
    private readonly IntPtr _nativePtr;
    private bool _isDisposed;

    /// <summary>
    /// 指定された初期パラメータに基づき、新しい Native C++ カメラインスタンスを生成して初期化します。
    /// </summary>
    public Camera(
        float x,
        float y,
        float scale,
        float minScale,
        float maxScale,
        float lerpSpeed,
        float paddingX,
        float paddingY)
    {
        _nativePtr = NativeCameraCreate(x, y, scale, minScale, maxScale, lerpSpeed, paddingX, paddingY);
        if (_nativePtr == IntPtr.Zero)
        {
            throw new InvalidOperationException("Native カメラの生成に失敗しました。");
        }
    }

    /// <summary>
    /// <see cref="CameraConfig"/> 設定オブジェクトに基づき、新しい Native C++ カメラインスタンスを生成して初期化します。
    /// </summary>
    public Camera(CameraConfig config)
    {
        _nativePtr = NativeCameraCreate(
            config.InitialX,
            config.InitialY,
            config.InitialScale,
            config.MinScale,
            config.MaxScale,
            config.LerpSpeed,
            config.PaddingX,
            config.PaddingY);

        if (_nativePtr == IntPtr.Zero)
        {
            throw new InvalidOperationException("Native カメラの生成に失敗しました。");
        }
    }

    /// <summary>
    /// 既存の Native C++ カメラポインタを指定して初期化します。
    /// </summary>
    public Camera(IntPtr nativePtr)
    {
        _nativePtr = nativePtr;
    }

    /// <summary>
    /// デストラクタ
    /// </summary>
    ~Camera()
    {
        Dispose(false);
    }

    /// <summary>
    /// 内部で保持している Native C++ カメラポインタを取得します。
    /// </summary>
    public IntPtr NativePtr => _nativePtr;

    /// <summary>
    /// カメラの位置およびズーム倍率を即座にリセット・強制設定します。
    /// </summary>
    public void Reset(float x, float y, float scale)
    {
        NativeCameraReset(_nativePtr, x, y, scale);
    }

    /// <summary>
    /// 追従対象ターゲット群に基づき、カメラの位置とズーム倍率を更新します。
    /// </summary>
    public void Update(float deltaTime, ReadOnlySpan<Point<float>> targets, float viewportWidth, float viewportHeight)
    {
        fixed (Point<float>* pTargets = targets)
        {
            NativeCameraUpdate(_nativePtr, deltaTime, pTargets, targets.Length, viewportWidth, viewportHeight);
        }
    }

    /// <summary>
    /// 現在のカメラの表示位置 (X, Y) およびズームスケールを取得します。
    /// </summary>
    public (float X, float Y, float Scale) GetState()
    {
        float x = 0.0f;
        float y = 0.0f;
        float scale = 1.0f;
        NativeCameraGetState(_nativePtr, &x, &y, &scale);
        return (x, y, scale);
    }

    /// <summary>
    /// 現在のカメラの表示位置 (X, Y) およびズームスケールを出力パラメータとして取得します。
    /// </summary>
    public void GetState(out float x, out float y, out float scale)
    {
        fixed (float* px = &x, py = &y, ps = &scale)
        {
            NativeCameraGetState(_nativePtr, px, py, ps);
        }
    }

    /// <summary>
    /// カメラの制御設定パラメータ（最小/最大スケール、補間速度、パディング）を設定します。
    /// </summary>
    public void SetConfig(float minScale, float maxScale, float lerpSpeed, float paddingX, float paddingY)
    {
        NativeCameraSetConfig(_nativePtr, minScale, maxScale, lerpSpeed, paddingX, paddingY);
    }

    /// <summary>
    /// アンマネージドリソースを解放します。
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// リソース解放の保護仮想メソッド。
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (_nativePtr != IntPtr.Zero)
            {
                NativeCameraDestroy(_nativePtr);
            }
            _isDisposed = true;
        }
    }

    [LibraryImport(_dllName, EntryPoint = "Camera_Create")]
    private static partial IntPtr NativeCameraCreate(
        float x,
        float y,
        float scale,
        float minScale,
        float maxScale,
        float lerpSpeed,
        float paddingX,
        float paddingY);

    [LibraryImport(_dllName, EntryPoint = "Camera_Destroy")]
    private static partial void NativeCameraDestroy(IntPtr camera);

    [LibraryImport(_dllName, EntryPoint = "Camera_Reset")]
    [SuppressGCTransition]
    private static partial void NativeCameraReset(IntPtr camera, float x, float y, float scale);

    [LibraryImport(_dllName, EntryPoint = "Camera_Update")]
    [SuppressGCTransition]
    private static partial void NativeCameraUpdate(
        IntPtr camera,
        float deltaTime,
        Point<float>* targets,
        int targetCount,
        float viewportWidth,
        float viewportHeight);

    [LibraryImport(_dllName, EntryPoint = "Camera_GetState")]
    [SuppressGCTransition]
    private static partial void NativeCameraGetState(IntPtr camera, float* outX, float* outY, float* outScale);

    [LibraryImport(_dllName, EntryPoint = "Camera_SetConfig")]
    [SuppressGCTransition]
    private static partial void NativeCameraSetConfig(
        IntPtr camera,
        float minScale,
        float maxScale,
        float lerpSpeed,
        float paddingX,
        float paddingY);
}
