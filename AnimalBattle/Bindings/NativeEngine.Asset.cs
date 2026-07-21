using System;
using System.Runtime.InteropServices;

namespace AnimalBattle.Bindings;

public static partial class NativeEngine
{
    /// <summary>
    /// 画像ファイルを読み込み、グラフィックハンドルを取得します。
    /// </summary>
    /// <param name="path">画像ファイルパス</param>
    /// <return>グラフィックハンドル（失敗時は -1）</return>
    [LibraryImport(_dllName, EntryPoint = "Engine_LoadGraph", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int LoadGraph(string path);

    /// <summary>
    /// 読み込まれたグラフィックハンドルを解放します。
    /// </summary>
    /// <param name="handle">グラフィックハンドル</param>
    /// <return>成功時は 0</return>
    [LibraryImport(_dllName, EntryPoint = "Engine_DeleteGraph")]
    public static partial int DeleteGraph(int handle);
}
