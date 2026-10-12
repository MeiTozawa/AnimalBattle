using AnimalBattle.Bindings;

namespace AnimalBattle.Core;

/// <summary>
///     スプライト画像リソースのロードおよびメモリ解放を管理するマネージャークラス。
///     ファイル存在確認および DxLib ネイティブハンドルの有効性を Debug.Assert で厳格に検証します。
/// </summary>
public static class AssetService {
    /// <summary>
    ///     画像ファイルを指定してスプライトグラフィックハンドルをロードします。
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
        return handle;
    }

    /// <summary>
    ///     ロード済みのグラフィックハンドルを解放・削除します。
    /// </summary>
    /// <param name="handle">画像ハンドル</param>
    /// <returns>解放成功時は true、失敗時は false</returns>
    public static bool DeleteGraph(int handle)
    {
        Debug.Assert(handle >= 0, $"[AssetService 警告] 無効なグラフィックハンドルです (Handle: {handle})。");
        if (handle < 0) return false;

        var success = NativeBindings.Engine_DeleteGraph(handle);
        Debug.Assert(success, $"[AssetService 警告] グラフィックハンドルの解放に失敗しました (Handle: {handle})。");
        return success;
    }
}