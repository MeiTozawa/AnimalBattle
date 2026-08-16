using System;
using AnimalBattle.Bindings;

namespace AnimalBattle.Core;
/// <summary>
/// スプライト画像リソースのロードおよびメモリ解放を管理するマネージャークラス。
/// </summary>
public static class AssetManager
{
    /// <summary>
    /// 画像ファイルを指定してスプライトグラフィックハンドルをロードします。
    /// </summary>
    /// <param name="filePath">画像ファイルパス</param>
    /// <returns>グラフィックハンドル（失敗時は -1）</returns>
    public static int LoadGraph(string filePath)
    {
        return NativeBindings.Engine_LoadGraph(filePath);
    }

    /// <summary>
    /// ロード済みのグラフィックハンドルを解放・削除します。
    /// </summary>
    /// <param name="handle">画像ハンドル</param>
    /// <returns>成功時 0、失敗時 -1</returns>
    public static int DeleteGraph(int handle)
    {
        return NativeBindings.Engine_DeleteGraph(handle);
    }
}
