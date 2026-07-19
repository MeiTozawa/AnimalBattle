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
}
