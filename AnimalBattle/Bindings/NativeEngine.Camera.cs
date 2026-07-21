using System;
using System.Runtime.InteropServices;

namespace AnimalBattle.Bindings;

public static partial class NativeEngine
{
    /// <summary>
    /// 指定された初期設定パラメータに基づき、Native C++ カメラモジュールのインスタンスを生成します。
    /// </summary>
    /// <param name="x">初期中心 X 座標</param>
    /// <param name="y">初期中心 Y 座標</param>
    /// <param name="scale">初期ズーム倍率</param>
    /// <param name="minScale">最小ズームスケール</param>
    /// <param name="maxScale">最大ズームスケール</param>
    /// <param name="lerpSpeed">補間追従速度</param>
    /// <param name="paddingX">横方向余白パディング</param>
    /// <param name="paddingY">縦方向余白パディング</param>
    /// <returns>Camera インスタンスポインタ</returns>
    [LibraryImport(_dllName, EntryPoint = "Camera_Create")]
    public static partial IntPtr CameraCreate(
        float x,
        float y,
        float scale,
        float minScale,
        float maxScale,
        float lerpSpeed,
        float paddingX,
        float paddingY);

    /// <summary>
    /// Native C++ カメラモジュールを破棄します。
    /// </summary>
    /// <param name="camera">Camera インスタンスポインタ</param>
    [LibraryImport(_dllName, EntryPoint = "Camera_Destroy")]
    public static partial void CameraDestroy(IntPtr camera);

    /// <summary>
    /// カメラの位置およびズーム倍率を即座にリセット・強制設定します。
    /// </summary>
    /// <param name="camera">Camera インスタンスポインタ</param>
    /// <param name="x">中心 X 座標</param>
    /// <param name="y">中心 Y 座標</param>
    /// <param name="scale">ズーム倍率</param>
    [LibraryImport(_dllName, EntryPoint = "Camera_Reset")]
    [SuppressGCTransition]
    public static partial void CameraReset(IntPtr camera, float x, float y, float scale);

    /// <summary>
    /// 追従対象ターゲット群に基づき、カメラの位置とズーム倍率を更新します。
    /// </summary>
    /// <param name="camera">Camera インスタンスポインタ</param>
    /// <param name="dt">フレーム経過時間（秒）</param>
    /// <param name="targets">ターゲット座標配列へのポインタ</param>
    /// <param name="targetCount">ターゲット数</param>
    /// <param name="viewportWidth">ビューポートの幅</param>
    /// <param name="viewportHeight">ビューポートの高さ</param>
    [LibraryImport(_dllName, EntryPoint = "Camera_Update")]
    [SuppressGCTransition]
    public static unsafe partial void CameraUpdate(
        IntPtr camera,
        float dt,
        Point<float>* targets,
        int targetCount,
        float viewportWidth,
        float viewportHeight);

    /// <summary>
    /// 現在のカメラの表示座標およびスケールを取得します。
    /// </summary>
    /// <param name="camera">Camera インスタンスポインタ</param>
    /// <param name="outX">出力先 X 座標ポインタ</param>
    /// <param name="outY">出力先 Y 座標ポインタ</param>
    /// <param name="outScale">出力先スケールポインタ</param>
    [LibraryImport(_dllName, EntryPoint = "Camera_GetState")]
    [SuppressGCTransition]
    public static unsafe partial void CameraGetState(IntPtr camera, float* outX, float* outY, float* outScale);

    /// <summary>
    /// カメラの制御設定パラメータ（最小/最大スケール、補間速度、パディング）を設定します。
    /// </summary>
    /// <param name="camera">Camera インスタンスポインタ</param>
    /// <param name="minScale">最小ズームスケール</param>
    /// <param name="maxScale">最大ズームスケール</param>
    /// <param name="lerpSpeed">補間追従速度</param>
    /// <param name="paddingX">横方向余白パディング</param>
    /// <param name="paddingY">縦方向余白パディング</param>
    [LibraryImport(_dllName, EntryPoint = "Camera_SetConfig")]
    [SuppressGCTransition]
    public static partial void CameraSetConfig(
        IntPtr camera,
        float minScale,
        float maxScale,
        float lerpSpeed,
        float paddingX,
        float paddingY);
}
