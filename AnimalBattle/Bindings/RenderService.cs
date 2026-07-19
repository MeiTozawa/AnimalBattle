using System;
using System.Runtime.InteropServices;

namespace AnimalBattle.Bindings;

public enum BorderShape
{
    Null,
    Rectangle,
    DoubleRectangle,
    Pill,
    Underline,
}

[StructLayout(LayoutKind.Sequential)]
public struct Point<T> where T : struct
{
    public T X;
    public T Y;

    public Point(T x, T y)
    {
        X = x;
        Y = y;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct Rect<T> where T : struct
{
    public T X1;
    public T Y1;
    public T X2;
    public T Y2;

    public Rect(T x1, T y1, T x2, T y2)
    {
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
    }
}

public unsafe partial class RenderService
{
    private const string DllName = "AnimalBattleCore.dll";
    private readonly IntPtr _nativePtr;

    [LibraryImport(DllName, EntryPoint = "Render_DrawString", StringMarshalling = StringMarshalling.Utf16)]
    [SuppressGCTransition]
    private static partial void NativeDrawString(IntPtr service, Point<int> p, string text, uint color);

    [LibraryImport(DllName, EntryPoint = "Render_DrawBox")]
    [SuppressGCTransition]
    private static partial void NativeDrawBox(IntPtr service, Rect<int> r, uint color, [MarshalAs(UnmanagedType.U1)] bool fillFlag);

    [LibraryImport(DllName, EntryPoint = "Render_DrawStringWithBorder", StringMarshalling = StringMarshalling.Utf16)]
    [SuppressGCTransition]
    private static partial void NativeDrawStringWithBorder(IntPtr service, Rect<int> r, string text, BorderShape shape, uint textColor, uint borderColor);

    [LibraryImport(DllName, EntryPoint = "Render_DrawGraph")]
    [SuppressGCTransition]
    private static partial void NativeDrawGraph(IntPtr service, Point<int> p, int handle, [MarshalAs(UnmanagedType.U1)] bool transFlag);

    public RenderService(IntPtr nativePtr)
    {
        _nativePtr = nativePtr;
        if (_nativePtr == IntPtr.Zero)
        {
            throw new ArgumentNullException(nameof(nativePtr), "Native service pointer cannot be null.");
        }
    }

    public void DrawString(Point<int> p, string text, uint color)
    {
        NativeDrawString(_nativePtr, p, text, color);
    }

    public void DrawBox(Rect<int> r, uint color, bool fillFlag)
    {
        NativeDrawBox(_nativePtr, r, color, fillFlag);
    }

    public void DrawStringWithBorder(Rect<int> r, string text, BorderShape shape = BorderShape.Null, uint textColor = 0xFFFFFF, uint borderColor = 0xFFFFFF)
    {
        NativeDrawStringWithBorder(_nativePtr, r, text, shape, textColor, borderColor);
    }

    public void DrawGraph(Point<int> p, int handle, bool transFlag)
    {
        NativeDrawGraph(_nativePtr, p, handle, transFlag);
    }
}
