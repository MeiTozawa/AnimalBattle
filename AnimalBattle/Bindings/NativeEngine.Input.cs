using System;
using System.Runtime.InteropServices;

namespace AnimalBattle.Bindings;

public static partial class NativeEngine
{
    /// <summary>
    /// マウスカーソルを手の形（ポインター）に変更します。
    /// </summary>
    [LibraryImport(_dllName, EntryPoint = "Engine_SetCursorPointer")]
    [SuppressGCTransition]
    public static partial void SetCursorPointer();

    /// <summary>
    /// マウスカーソルを標準の矢印に変更します。
    /// </summary>
    [LibraryImport(_dllName, EntryPoint = "Engine_SetCursorArrow")]
    [SuppressGCTransition]
    public static partial void SetCursorArrow();

    /// <summary>
    /// 指定されたキーの押下状態を取得します。
    /// </summary>
    /// <param name="keyCode">キーコード</param>
    /// <return>押されている場合は 1、押されていない場合は 0</return>
    [LibraryImport(_dllName, EntryPoint = "Engine_CheckHitKey")]
    [SuppressGCTransition]
    public static partial int CheckHitKey(int keyCode);

    /// <summary>
    /// 現在のマウスカーソル座標を取得します。
    /// </summary>
    /// <param name="x">出力先 X 座標ポインタ</param>
    /// <param name="y">出力先 Y 座標ポインタ</param>
    [LibraryImport(_dllName, EntryPoint = "Engine_GetMousePoint")]
    [SuppressGCTransition]
    public static unsafe partial void GetMousePoint(int* x, int* y);

    /// <summary>
    /// マウスボタンの押下状態を取得します。
    /// </summary>
    /// <return>マウス入力フラグ</return>
    [LibraryImport(_dllName, EntryPoint = "Engine_GetMouseInput")]
    [SuppressGCTransition]
    public static partial int GetMouseInput();
}
