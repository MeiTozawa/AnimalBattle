using System;
using System.Runtime.InteropServices;

namespace AnimalBattle.Bindings;

/// <summary>
/// Native C++ レンダリングサービスへの描画コマンド呼び出しを仲介するラッパークラス。
/// </summary>
public unsafe partial class RenderService
{
    private const string _dllName = "AnimalBattleCore.dll";
    private readonly IntPtr _nativePtr;

    /// <summary>
    /// Native C++ サービスポインタを指定して新しい <see cref="RenderService"/> インスタンスを初期化します。
    /// </summary>
    /// <param name="nativePtr">Native C++ レンダリングサービスのインスタンスポインタ</param>
    /// <exception cref="ArgumentNullException">nativePtr が Zero の場合に発生します。</exception>
    public RenderService(IntPtr nativePtr)
    {
        _nativePtr = nativePtr;
        if (_nativePtr == IntPtr.Zero)
        {
            throw new ArgumentNullException(nameof(nativePtr), "Native サービスポインタを null にすることはできません。");
        }
    }

    /// <summary>
    /// カメラ座標変換の有効・無効を設定します。
    /// </summary>
    /// <param name="enabled">有効にする場合は true</param>
    public void SetCameraEnabled(bool enabled)
    {
        NativeSetCameraEnabled(_nativePtr, enabled);
    }

    /// <summary>
    /// カメラの注視点座標および表示倍率（ズーム）を設定します。
    /// </summary>
    /// <param name="x">カメラ中心 X 座標</param>
    /// <param name="y">カメラ中心 Y 座標</param>
    /// <param name="scale">表示倍率</param>
    public void SetCameraState(float x, float y, float scale)
    {
        NativeSetCameraState(_nativePtr, x, y, scale);
    }

    /// <summary>
    /// 指定された座標に文字列を描画します。
    /// </summary>
    /// <param name="p">描画座標</param>
    /// <param name="text">描画文字列</param>
    /// <param name="color">描画色 (ARGB)</param>
    public void DrawString(Point<int> p, string text, uint color)
    {
        NativeDrawString(_nativePtr, p, text, color);
    }

    /// <summary>
    /// 指定された矩形領域を描画（または塗りつぶし）します。
    /// </summary>
    /// <param name="r">矩形領域</param>
    /// <param name="color">描画色 (ARGB)</param>
    /// <param name="fillFlag">塗りつぶす場合は true、枠線のみの場合は false</param>
    public void DrawBox(Rect<int> r, uint color, bool fillFlag)
    {
        NativeDrawBox(_nativePtr, r, color, fillFlag);
    }

    /// <summary>
    /// 指定された装飾枠線付きで文字列を描画します。
    /// </summary>
    /// <param name="r">描画領域</param>
    /// <param name="text">描画文字列</param>
    /// <param name="shape">枠線の形状</param>
    /// <param name="textColor">文字色 (ARGB)</param>
    /// <param name="borderColor">枠線色 (ARGB)</param>
    public void DrawStringWithBorder(Rect<int> r, string text, BorderShape shape = BorderShape.Null, uint textColor = 0xFFFFFF, uint borderColor = 0xFFFFFF)
    {
        NativeDrawStringWithBorder(_nativePtr, r, text, shape, textColor, borderColor);
    }

    /// <summary>
    /// グラフィックハンドルを指定して画像を描画します。
    /// </summary>
    /// <param name="p">描画座標</param>
    /// <param name="handle">画像ハンドル</param>
    /// <param name="transFlag">透過処理を行う場合は true</param>
    public void DrawGraph(Point<int> p, int handle, bool transFlag)
    {
        NativeDrawGraph(_nativePtr, p, handle, transFlag);
    }

    [LibraryImport(_dllName, EntryPoint = "Render_DrawString", StringMarshalling = StringMarshalling.Utf16)]
    [SuppressGCTransition]
    private static partial void NativeDrawString(IntPtr service, Point<int> p, string text, uint color);

    [LibraryImport(_dllName, EntryPoint = "Render_DrawBox")]
    [SuppressGCTransition]
    private static partial void NativeDrawBox(IntPtr service, Rect<int> r, uint color, [MarshalAs(UnmanagedType.U1)] bool fillFlag);

    [LibraryImport(_dllName, EntryPoint = "Render_DrawStringWithBorder", StringMarshalling = StringMarshalling.Utf16)]
    [SuppressGCTransition]
    private static partial void NativeDrawStringWithBorder(IntPtr service, Rect<int> r, string text, BorderShape shape, uint textColor, uint borderColor);

    [LibraryImport(_dllName, EntryPoint = "Render_DrawGraph")]
    [SuppressGCTransition]
    private static partial void NativeDrawGraph(IntPtr service, Point<int> p, int handle, [MarshalAs(UnmanagedType.U1)] bool transFlag);

    [LibraryImport(_dllName, EntryPoint = "Render_SetCameraEnabled")]
    [SuppressGCTransition]
    private static partial void NativeSetCameraEnabled(IntPtr service, [MarshalAs(UnmanagedType.U1)] bool enabled);

    [LibraryImport(_dllName, EntryPoint = "Render_SetCameraState")]
    [SuppressGCTransition]
    private static partial void NativeSetCameraState(IntPtr service, float x, float y, float scale);
}
