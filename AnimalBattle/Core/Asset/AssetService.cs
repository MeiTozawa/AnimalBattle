using System;
using System.IO;
using AnimalBattle.Bindings;
using ErrorOr;

namespace AnimalBattle.Core;

/// <summary>
/// スプライト画像リソースのロードおよびメモリ解放を管理するマネージャークラス。
/// ファイル存在確認および DxLib ネイティブハンドルの有効性を検証し、<see cref="ErrorOr{T}"/> による型安全な結果を返します。
/// </summary>
public static class AssetService
{
    /// <summary>
    /// 画像ファイルを指定してスプライトグラフィックハンドルをロードします。
    /// </summary>
    /// <param name="filePath">画像ファイルパス</param>
    /// <returns>成功時はグラフィックハンドル、失敗時は <see cref="Error"/></returns>
    public static ErrorOr<int> LoadGraph(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Error.Validation("Asset.EmptyPath", "アセットファイルパスが null または空白です。");
        }

        if (!File.Exists(filePath))
        {
            return Error.NotFound("Asset.NotFound", $"画像アセットファイルが存在しません: {filePath}");
        }

        int handle = NativeBindings.Engine_LoadGraph(filePath);
        if (handle < 0)
        {
            return Error.Failure("Asset.LoadFailed", $"ネイティブエンジンによる画像ロードに失敗しました (Handle: {handle}): {filePath}");
        }

        return handle;
    }

    /// <summary>
    /// ロード済みのグラフィックハンドルを解放・削除します。
    /// </summary>
    /// <param name="handle">画像ハンドル</param>
    /// <returns>成功時は <see cref="Success"/>、失敗時は <see cref="Error"/></returns>
    public static ErrorOr<Success> DeleteGraph(int handle)
    {
        if (handle < 0)
        {
            return Error.Validation("Asset.InvalidHandle", $"無効なグラフィックハンドルです (Handle: {handle})。");
        }

        bool success = NativeBindings.Engine_DeleteGraph(handle);
        if (!success)
        {
            return Error.Failure("Asset.DeleteFailed", $"グラフィックハンドルの解放に失敗しました (Handle: {handle})。");
        }

        return Result.Success;
    }
}
