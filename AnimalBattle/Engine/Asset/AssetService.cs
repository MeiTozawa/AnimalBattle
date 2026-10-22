using System.Collections.Concurrent;
using AnimalBattle.Bindings;

namespace AnimalBattle.Asset;

/// <summary>
/// スプライト画像リソースのロードおよびメモリ解放を管理するマネージャークラス。
/// ファイル存在確認および DxLib ネイティブハンドルの有効性を検証し、画像サイズキャッシュを保持します。
/// </summary>
public static class AssetService {
    private static readonly ConcurrentDictionary<int, Vec2i> _graphSizes = [];

    /// <summary>
    /// 画像ファイルを指定してスプライトグラフィックハンドルをロードし、サイズをキャッシュします。
    /// </summary>
    /// <param name="filePath">画像ファイルパス</param>
    /// <returns>成功時は正のグラフィックハンドル、失敗時は -1</returns>
    public static int LoadGraph(string filePath)
    {
        Debug.Assert(!string.IsNullOrWhiteSpace(filePath), "[AssetService 警告] アセットファイルパスが null または空白です。");
        if (string.IsNullOrWhiteSpace(filePath)) return -1;

        Debug.Assert(File.Exists(filePath), $"[AssetService 警告] 画像アセットファイルが存在しません: {filePath}");
        if (!File.Exists(filePath)) return -1;

        var handle = NativeBindings.Engine_LoadGraph(filePath);
        Debug.Assert(handle >= 0, $"[AssetService 警告] ネイティブエンジンによる画像ロードに失敗しました (Handle: {handle}): {filePath}");
        if (handle >= 0)
        {
            var size = Vec2i.Zero;
            unsafe
            {
                NativeBindings.Engine_GetGraphSize(handle, &size);
            }
            _graphSizes[handle] = size;
        }

        return handle;
    }

    /// <summary>
    /// ロード済みのグラフィックハンドルを解放・削除し、キャッシュを破棄します。
    /// </summary>
    /// <param name="handle">画像ハンドル</param>
    /// <returns>解放成功時は true、失敗時は false</returns>
    public static bool DeleteGraph(int handle)
    {
        Debug.Assert(handle >= 0, $"[AssetService 警告] 無効なグラフィックハンドルです (Handle: {handle})。");

        _graphSizes.TryRemove(handle, out _);
        var success = NativeBindings.Engine_DeleteGraph(handle);
        Debug.Assert(success, $"[AssetService 警告] グラフィックハンドルの解放に失敗しました (Handle: {handle})。");
        return success;
    }

    /// <summary>
    /// キャッシュ済みの画像サイズ取得を試行します。
    /// </summary>
    public static bool TryGetGraphSize(int handle, out Vec2i size)
    {
        return _graphSizes.TryGetValue(handle, out size);
    }

    /// <summary>
    /// 指定画像ハンドルの幅と高さを取得します。
    /// 双スレッドレンダリング競合を防止するため、キャッシュされた画像サイズを安全に返却します。
    /// </summary>
    /// <param name="handle">画像ハンドル</param>
    /// <returns>画像サイズ (<see cref="Vec2i" />)</returns>
    public static Vec2i GetGraphSize(int handle)
    {
        if (_graphSizes.TryGetValue(handle, out var cachedSize))
        {
            return cachedSize;
        }

        Debug.Assert(handle >= 0, $"[AssetService 警告] 無効なグラフィックハンドルです (Handle: {handle})。");
        var size = Vec2i.Zero;
        unsafe
        {
            NativeBindings.Engine_GetGraphSize(handle, &size);
        }

        _graphSizes[handle] = size;
        return size;
    }
}
