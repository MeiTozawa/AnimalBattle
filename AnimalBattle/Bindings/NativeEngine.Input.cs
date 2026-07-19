using System;
using System.Runtime.InteropServices;

namespace AnimalBattle.Bindings;

public static partial class NativeEngine
{
    [LibraryImport(DllName, EntryPoint = "Engine_SetCursorPointer")]
    [SuppressGCTransition]
    public static partial void SetCursorPointer();

    [LibraryImport(DllName, EntryPoint = "Engine_SetCursorArrow")]
    [SuppressGCTransition]
    public static partial void SetCursorArrow();

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
