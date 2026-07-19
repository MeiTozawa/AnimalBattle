using System;
using System.Runtime.InteropServices;

namespace AnimalBattle.Bindings;

public unsafe partial class RenderService
{
    private const string DllName = "AnimalBattleCore.dll";
    private readonly IntPtr _nativePtr;

    [LibraryImport(DllName, EntryPoint = "Render_DrawString", StringMarshalling = StringMarshalling.Utf16)]
    [SuppressGCTransition]
    private static partial void NativeDrawString(IntPtr service, int x, int y, string text, uint color);

    [LibraryImport(DllName, EntryPoint = "Render_DrawBoxAA")]
    [SuppressGCTransition]
    private static partial void NativeDrawBoxAA(IntPtr service, float x1, float y1, float x2, float y2, uint color, [MarshalAs(UnmanagedType.U1)] bool fillFlag);

    [LibraryImport(DllName, EntryPoint = "Render_DrawGraph")]
    [SuppressGCTransition]
    private static partial void NativeDrawGraph(IntPtr service, int x, int y, int handle, [MarshalAs(UnmanagedType.U1)] bool transFlag);

    public RenderService(IntPtr nativePtr)
    {
        _nativePtr = nativePtr;
        if (_nativePtr == IntPtr.Zero)
        {
            throw new ArgumentNullException(nameof(nativePtr), "Native service pointer cannot be null.");
        }
    }

    public void DrawString(int x, int y, string text, uint color)
    {
        NativeDrawString(_nativePtr, x, y, text, color);
    }

    public void DrawBoxAA(float x1, float y1, float x2, float y2, uint color, bool fillFlag)
    {
        NativeDrawBoxAA(_nativePtr, x1, y1, x2, y2, color, fillFlag);
    }

    public void DrawGraph(int x, int y, int handle, bool transFlag)
    {
        NativeDrawGraph(_nativePtr, x, y, handle, transFlag);
    }
}
