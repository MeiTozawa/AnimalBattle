using System;
using System.Runtime.InteropServices;

namespace AnimalBattle.Core;

/// <summary>
/// Native C++ <c>RenderService</c> の P/Invoke バインディングおよび高レベル C# 描画 API を提供するラッパークラス。
/// </summary>
public partial class RenderService
{
    private const string _dllName = "AnimalBattleCore.dll";

    private readonly IntPtr _nativePtr;

    /// <summary>
    /// 指定されたネイティブ C++ <c>IRenderService</c> インスタンスポインタを用いて <see cref="RenderService"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="nativePtr">ネイティブポインタ</param>
    public RenderService(IntPtr nativePtr)
    {
        _nativePtr = nativePtr;
    }

    /// <summary>
    /// 保持しているネイティブ C++ <c>IRenderService</c> ポインタを取得します。
    /// </summary>
    public IntPtr NativePtr => _nativePtr;

    /// <summary>
    /// カメラ座標系の適用・無効化を切り替えます。
    /// </summary>
    public void SetCameraEnabled(bool enabled)
    {
        NativeSetCameraEnabled(_nativePtr, enabled);
    }

    /// <summary>
    /// カメラパラメータ（位置 X, Y、ズーム率）をレンダリングサービスに送信・同期します。
    /// </summary>
    public void SetCameraState(float x, float y, float zoom)
    {
        NativeSetCameraState(_nativePtr, x, y, zoom);
    }

    /// <summary>
    /// 指定されたパラメータで矩形を描画します。
    /// </summary>
    public void DrawBox(Rect<int> rect, uint color, bool isFilled)
    {
        NativeDrawBox(_nativePtr, rect, color, isFilled);
    }

    /// <summary>
    /// 指定されたパラメータで文字列を描画します。
    /// </summary>
    public void DrawString(Point<int> position, string text, uint color)
    {
        NativeDrawString(_nativePtr, position, text, color);
    }

    /// <summary>
    /// 指定されたパラメータで枠線付き文字列を描画します。
    /// </summary>
    public void DrawStringWithBorder(Rect<int> rect, string text, BorderShape shape, uint textColor, uint borderColor)
    {
        NativeDrawStringWithBorder(_nativePtr, rect, text, shape, textColor, borderColor);
    }

    /// <summary>
    /// 指定された位置に画像スプライトを描画します。
    /// </summary>
    public void DrawGraph(Point<int> position, int handle, bool transFlag = true)
    {
        NativeDrawGraph(_nativePtr, position, handle, transFlag);
    }

    /// <summary>
    /// 指定された切り出し領域、拡大率、回転角度で画像スプライトを描画します。
    /// </summary>
    public void DrawRectRotaGraph(Point<int> position, Rect<int> srcRect, double extRate, double angle, int graphHandle, bool transFlag = true, bool turnFlag = false)
    {
        NativeDrawRectRotaGraph(_nativePtr, position, srcRect, extRate, angle, graphHandle, transFlag, turnFlag);
    }

    [LibraryImport(_dllName, EntryPoint = "Render_SetCameraEnabled")]
    [SuppressGCTransition]
    private static partial void NativeSetCameraEnabled(IntPtr service, [MarshalAs(UnmanagedType.U1)] bool enabled);

    [LibraryImport(_dllName, EntryPoint = "Render_SetCameraState")]
    [SuppressGCTransition]
    private static partial void NativeSetCameraState(IntPtr service, float x, float y, float scale);

    [LibraryImport(_dllName, EntryPoint = "Render_DrawBox")]
    [SuppressGCTransition]
    private static partial void NativeDrawBox(IntPtr service, Rect<int> r, uint color, [MarshalAs(UnmanagedType.U1)] bool fillFlag);

    [LibraryImport(_dllName, EntryPoint = "Render_DrawString", StringMarshalling = StringMarshalling.Utf16)]
    [SuppressGCTransition]
    private static partial void NativeDrawString(IntPtr service, Point<int> p, string text, uint color);

    [LibraryImport(_dllName, EntryPoint = "Render_DrawStringWithBorder", StringMarshalling = StringMarshalling.Utf16)]
    [SuppressGCTransition]
    private static partial void NativeDrawStringWithBorder(IntPtr service, Rect<int> r, string text, BorderShape shape, uint textColor, uint borderColor);

    [LibraryImport(_dllName, EntryPoint = "Render_DrawGraph")]
    [SuppressGCTransition]
    private static partial void NativeDrawGraph(IntPtr service, Point<int> p, int handle, [MarshalAs(UnmanagedType.U1)] bool transFlag);

    [LibraryImport(_dllName, EntryPoint = "Render_DrawRectRotaGraph")]
    [SuppressGCTransition]
    private static partial void NativeDrawRectRotaGraph(IntPtr service, Point<int> p, Rect<int> src, double extRate, double angle, int graphHandle, [MarshalAs(UnmanagedType.U1)] bool transFlag, [MarshalAs(UnmanagedType.U1)] bool turnFlag);
}
