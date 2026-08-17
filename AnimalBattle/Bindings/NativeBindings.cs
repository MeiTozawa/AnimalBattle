using System;
using System.Runtime.InteropServices;

namespace AnimalBattle.Bindings;

/// <summary>
/// Native C++ コアライブラリ (AnimalBattleCore.dll) との P/Invoke バインディングを提供する静的クラス。
/// </summary>
internal static unsafe partial class NativeBindings
{
    private const string _dllName = "AnimalBattleCore.dll";

    #region Engine Control

    [LibraryImport(_dllName, EntryPoint = "Engine_Init")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Engine_Init(IntPtr hwnd, int width, int height);

    [LibraryImport(_dllName, EntryPoint = "Engine_Shutdown")]
    public static partial void Engine_Shutdown();

    [LibraryImport(_dllName, EntryPoint = "Engine_ProcessMessages")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Engine_ProcessMessages();

    [LibraryImport(_dllName, EntryPoint = "Engine_BeginDraw")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Engine_BeginDraw();

    [LibraryImport(_dllName, EntryPoint = "Engine_EndDraw")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Engine_EndDraw();

    [LibraryImport(_dllName, EntryPoint = "Engine_RequestExit")]
    [SuppressGCTransition]
    public static partial void Engine_RequestExit();

    [LibraryImport(_dllName, EntryPoint = "Engine_IsExitRequested")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Engine_IsExitRequested();

    #endregion

    #region Input

    [LibraryImport(_dllName, EntryPoint = "Input_IsKeyHit")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Input_IsKeyHit(int keyCode);

    [LibraryImport(_dllName, EntryPoint = "Input_GetJoypadState")]
    [SuppressGCTransition]
    public static partial int Input_GetJoypadState(int padIndex);

    [LibraryImport(_dllName, EntryPoint = "Engine_SetCursorPointer")]
    [SuppressGCTransition]
    public static partial void Engine_SetCursorPointer();

    [LibraryImport(_dllName, EntryPoint = "Engine_SetCursorArrow")]
    [SuppressGCTransition]
    public static partial void Engine_SetCursorArrow();

    [LibraryImport(_dllName, EntryPoint = "Engine_CheckHitKey")]
    [SuppressGCTransition]
    public static partial int Engine_CheckHitKey(int keyCode);

    [LibraryImport(_dllName, EntryPoint = "Engine_GetJoypadInputState")]
    [SuppressGCTransition]
    public static partial int Engine_GetJoypadInputState(int inputType);

    [LibraryImport(_dllName, EntryPoint = "Engine_GetJoypadNum")]
    [SuppressGCTransition]
    public static partial int Engine_GetJoypadNum();

    [LibraryImport(_dllName, EntryPoint = "Engine_GetMousePoint")]
    [SuppressGCTransition]
    public static partial void Engine_GetMousePoint(int* x, int* y);

    #endregion

    #region Assets

    [LibraryImport(_dllName, EntryPoint = "Engine_LoadGraph", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int Engine_LoadGraph(string path);

    [LibraryImport(_dllName, EntryPoint = "Engine_DeleteGraph")]
    public static partial int Engine_DeleteGraph(int handle);

    #endregion

    #region Drawing

    [LibraryImport(_dllName, EntryPoint = "Draw_Circle")]
    [SuppressGCTransition]
    public static partial void Draw_Circle(int x, int y, int radius, uint color, [MarshalAs(UnmanagedType.U1)] bool fill);

    [LibraryImport(_dllName, EntryPoint = "Draw_Box")]
    [SuppressGCTransition]
    public static partial void Draw_Box(int x1, int y1, int x2, int y2, uint color, [MarshalAs(UnmanagedType.U1)] bool fill);

    [LibraryImport(_dllName, EntryPoint = "Draw_String", StringMarshalling = StringMarshalling.Utf16)]
    public static partial void Draw_String(int x, int y, string text, uint color);

    [LibraryImport(_dllName, EntryPoint = "Draw_Graph")]
    [SuppressGCTransition]
    public static partial void Draw_Graph(int x, int y, int handle, [MarshalAs(UnmanagedType.U1)] bool trans);

    [LibraryImport(_dllName, EntryPoint = "Draw_RectRotaGraph")]
    [SuppressGCTransition]
    public static partial void Draw_RectRotaGraph(int x, int y, int srcX, int srcY, int width, int height, double scale, double angle, int handle, [MarshalAs(UnmanagedType.U1)] bool trans, [MarshalAs(UnmanagedType.U1)] bool turnX);

    #endregion
}
