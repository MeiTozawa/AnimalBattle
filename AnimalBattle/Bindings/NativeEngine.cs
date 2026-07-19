using System;
using System.Runtime.InteropServices;

namespace AnimalBattle.Bindings;

public static partial class NativeEngine
{
    private const string DllName = "AnimalBattleCore.dll";

    [LibraryImport(DllName, EntryPoint = "Engine_Initialize")]
    public static partial int Initialize(int width, int height, [MarshalAs(UnmanagedType.U1)] bool windowMode);

    [LibraryImport(DllName, EntryPoint = "Engine_ProcessMessage")]
    [SuppressGCTransition]
    public static partial int ProcessMessage();

    [LibraryImport(DllName, EntryPoint = "Engine_BeginFrame")]
    [SuppressGCTransition]
    public static partial void BeginFrame();

    [LibraryImport(DllName, EntryPoint = "Engine_EndFrame")]
    [SuppressGCTransition]
    public static partial void EndFrame(float deltaTime);

    [LibraryImport(DllName, EntryPoint = "Engine_IsExitRequested")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool IsExitRequested();

    [LibraryImport(DllName, EntryPoint = "Engine_Shutdown")]
    public static partial void Shutdown();

    [LibraryImport(DllName, EntryPoint = "Engine_GetRenderService")]
    public static partial IntPtr GetRenderService();

    // Asset loading APIs (managed by C# triggering)
    [LibraryImport(DllName, EntryPoint = "Engine_LoadGraph", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int LoadGraph(string path);

    [LibraryImport(DllName, EntryPoint = "Engine_DeleteGraph")]
    public static partial int DeleteGraph(int handle);

    // --- Cursor Control ---
    [LibraryImport(DllName, EntryPoint = "Engine_SetCursorPointer")]
    [SuppressGCTransition]
    public static partial void SetCursorPointer();

    [LibraryImport(DllName, EntryPoint = "Engine_SetCursorArrow")]
    [SuppressGCTransition]
    public static partial void SetCursorArrow();

    // --- Generic Audio ---
    [LibraryImport(DllName, EntryPoint = "Engine_PlaySoundMem")]
    [SuppressGCTransition]
    public static partial int PlaySoundMem(int handle, int playType, [MarshalAs(UnmanagedType.U1)] bool topPositionFlag);

    [LibraryImport(DllName, EntryPoint = "Engine_StopSoundMem")]
    [SuppressGCTransition]
    public static partial int StopSoundMem(int handle);

    [LibraryImport(DllName, EntryPoint = "Engine_ChangeVolumeSoundMem")]
    [SuppressGCTransition]
    public static partial int ChangeVolumeSoundMem(int volumePercent, int handle);

    // --- Generic Input ---
    [LibraryImport(DllName, EntryPoint = "Engine_CheckHitKey")]
    [SuppressGCTransition]
    public static partial int CheckHitKey(int keyCode);

    [LibraryImport(DllName, EntryPoint = "Engine_GetMousePoint")]
    [SuppressGCTransition]
    public static unsafe partial void GetMousePoint(int* x, int* y);

    [LibraryImport(DllName, EntryPoint = "Engine_GetMouseInput")]
    [SuppressGCTransition]
    public static partial int GetMouseInput();
}
