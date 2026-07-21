using System;
using System.Runtime.InteropServices;

namespace AnimalBattle.Bindings;

/// <summary>
/// Native C++ コアエンジン (AnimalBattleCore.dll) の P/Invoke インポート定義を提供するクラス。
/// </summary>
public static partial class NativeEngine
{
    private const string _dllName = "AnimalBattleCore.dll";

    /// <summary>
    /// エンジンを初期化します。
    /// </summary>
    /// <param name="width">ウィンドウ幅</param>
    /// <param name="height">ウィンドウ高さ</param>
    /// <param name="windowMode">ウィンドウモードで起動する場合は true</param>
    /// <return>成功時は 0、失敗時は -1</return>
    [LibraryImport(_dllName, EntryPoint = "Engine_Initialize")]
    public static partial int Initialize(int width, int height, [MarshalAs(UnmanagedType.U1)] bool windowMode);

    /// <summary>
    /// Windows メッセージ処理を行います。
    /// </summary>
    /// <return>正常時は 0、終了要請時は 0 以外</return>
    [LibraryImport(_dllName, EntryPoint = "Engine_ProcessMessage")]
    [SuppressGCTransition]
    public static partial int ProcessMessage();

    /// <summary>
    /// フレーム描画の開始処理（バックバッファのクリア等）を行います。
    /// </summary>
    [LibraryImport(_dllName, EntryPoint = "Engine_BeginFrame")]
    [SuppressGCTransition]
    public static partial void BeginFrame();

    /// <summary>
    /// フレーム描画の終了処理（ScreenFlip）を行います。
    /// </summary>
    /// <param name="deltaTime">フレーム経過時間（秒）</param>
    [LibraryImport(_dllName, EntryPoint = "Engine_EndFrame")]
    [SuppressGCTransition]
    public static partial void EndFrame(float deltaTime);

    /// <summary>
    /// アプリケーションの終了フラグが立っているか判定します。
    /// </summary>
    /// <return>終了要求がある場合は true</return>
    [LibraryImport(_dllName, EntryPoint = "Engine_IsExitRequested")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool IsExitRequested();

    /// <summary>
    /// エンジンのシャットダウン処理を行います。
    /// </summary>
    [LibraryImport(_dllName, EntryPoint = "Engine_Shutdown")]
    public static partial void Shutdown();

    /// <summary>
    /// Native C++ レンダリングサービスのインスタンスポインタを取得します。
    /// </summary>
    /// <return>IRenderService ポインタ</return>
    [LibraryImport(_dllName, EntryPoint = "Engine_GetRenderService")]
    public static partial IntPtr GetRenderService();
}
