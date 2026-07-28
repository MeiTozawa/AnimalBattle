using System.Runtime.InteropServices;

namespace AnimalBattle.Core;

/// <summary>
/// 画像テクスチャ等のアセットの読み込みおよび解放操作を提供する静的クラス。
/// </summary>
public static partial class AssetManager
{
    private const string _dllName = "AnimalBattleCore.dll";

    /// <summary>
    /// 画像ファイルを読み込み、グラフィックハンドルを取得します。
    /// </summary>
    /// <param name="path">画像ファイルパス</param>
    /// <returns>グラフィックハンドル（失敗時は -1）</returns>
    public static int LoadGraph(string path)
    {
        return NativeLoadGraph(path);
    }

    /// <summary>
    /// 読み込まれたグラフィックハンドルを解放します。
    /// </summary>
    /// <param name="handle">グラフィックハンドル</param>
    /// <returns>成功時は 0</returns>
    public static int DeleteGraph(int handle)
    {
        return NativeDeleteGraph(handle);
    }

    [LibraryImport(_dllName, EntryPoint = "Engine_LoadGraph", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int NativeLoadGraph(string path);

    [LibraryImport(_dllName, EntryPoint = "Engine_DeleteGraph")]
    private static partial int NativeDeleteGraph(int handle);
}
