using System.Runtime.InteropServices;

namespace AnimalBattle.Core;

/// <summary>
/// Native C++ コアエンジン (AnimalBattleCore.dll) のライフサイクル、メインループ制御、およびレンダリングサービス取得を提供する C# クラス。
/// </summary>
public static partial class GameEngine
{
    private const string _dllName = "AnimalBattleCore.dll";

    /// <summary>
    /// Native C++ エンジンを初期化します。
    /// </summary>
    /// <param name="width">画面幅</param>
    /// <param name="height">画面高さ</param>
    /// <param name="windowMode">ウィンドウモードで起動する場合は true</param>
    /// <returns>成功時は 0、失敗時は -1</returns>
    public static int Initialize(int width, int height, bool windowMode = true)
    {
        return NativeInitialize(width, height, windowMode);
    }

    /// <summary>
    /// Windows メッセージ処理を行います。
    /// </summary>
    /// <returns>正常時は 0、終了要請時は 0 以外</returns>
    public static int ProcessMessage()
    {
        return NativeProcessMessage();
    }

    /// <summary>
    /// フレーム描画の開始処理（バックバッファのクリア等）を行います。
    /// </summary>
    public static void BeginFrame()
    {
        NativeBeginFrame();
    }

    /// <summary>
    /// フレーム描画の終了処理（ScreenFlip）を行います。
    /// </summary>
    /// <param name="deltaTime">フレーム経過時間（秒）</param>
    public static void EndFrame(float deltaTime)
    {
        NativeEndFrame(deltaTime);
    }

    /// <summary>
    /// アプリケーションの終了フラグが立っているか判定します。
    /// </summary>
    /// <returns>終了要求がある場合は true</returns>
    public static bool IsExitRequested()
    {
        return NativeIsExitRequested();
    }

    /// <summary>
    /// エンジンのシャットダウン処理を行います。
    /// </summary>
    public static void Shutdown()
    {
        NativeShutdown();
    }

    /// <summary>
    /// Native C++ レンダリングサービスのインスタンスポインタを取得します。
    /// </summary>
    /// <returns>IRenderService ポインタ</returns>
    public static IntPtr GetRenderService()
    {
        return NativeGetRenderService();
    }

    [LibraryImport(_dllName, EntryPoint = "Engine_Initialize")]
    private static partial int NativeInitialize(int width, int height, [MarshalAs(UnmanagedType.U1)] bool windowMode);

    [LibraryImport(_dllName, EntryPoint = "Engine_ProcessMessage")]
    [SuppressGCTransition]
    private static partial int NativeProcessMessage();

    [LibraryImport(_dllName, EntryPoint = "Engine_BeginFrame")]
    [SuppressGCTransition]
    private static partial void NativeBeginFrame();

    [LibraryImport(_dllName, EntryPoint = "Engine_EndFrame")]
    [SuppressGCTransition]
    private static partial void NativeEndFrame(float deltaTime);

    [LibraryImport(_dllName, EntryPoint = "Engine_IsExitRequested")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool NativeIsExitRequested();

    [LibraryImport(_dllName, EntryPoint = "Engine_Shutdown")]
    private static partial void NativeShutdown();

    [LibraryImport(_dllName, EntryPoint = "Engine_GetRenderService")]
    private static partial IntPtr NativeGetRenderService();
}
