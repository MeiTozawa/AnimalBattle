using System;
using System.IO;
using AnimalBattle.Bindings;

namespace AnimalBattle.Core;

/// <summary>
/// スプライト画像リソースのロードおよびメモリ解放を管理するマネージャークラス。
/// ファイル存在確認および DxLib ネイティブハンドルの有効性を検証し、<see cref="Result{T}"/> による型安全な結果を返します。
/// </summary>
public static class AssetManager
{
    /// <summary>
    /// 画像ファイルを指定してスプライトグラフィックハンドルをロードします。
    /// </summary>
    /// <param name="filePath">画像ファイルパス</param>
    /// <returns>成功時はグラフィックハンドル、失敗時は <see cref="SimpleError"/></returns>
    public static Result<int> LoadGraph(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Result<int>.Err(SimpleError.NotValid("アセットファイルパスが null または空白です。"));
        }

        if (!File.Exists(filePath))
        {
            return Result<int>.Err(SimpleError.NotFound($"画像アセットファイルが存在しません: {filePath}"));
        }

        int handle = NativeBindings.Engine_LoadGraph(filePath);
        if (handle < 0)
        {
            return Result<int>.Err(SimpleError.NotValid($"ネイティブエンジンによる画像ロードに失敗しました (Handle: {handle}): {filePath}"));
        }

        return Result<int>.Ok(handle);
    }

    /// <summary>
    /// ロード済みのグラフィックハンドルを解放・削除します。
    /// </summary>
    /// <param name="handle">画像ハンドル</param>
    /// <returns>成功時は true、失敗時は <see cref="SimpleError"/></returns>
    public static Result<bool> DeleteGraph(int handle)
    {
        if (handle < 0)
        {
            return Result<bool>.Err(SimpleError.ArgumentOutOfRange($"無効なグラフィックハンドルです (Handle: {handle})。"));
        }

        int result = NativeBindings.Engine_DeleteGraph(handle);
        if (result != 0)
        {
            return Result<bool>.Err(SimpleError.NotValid($"グラフィックハンドルの解放に失敗しました (Handle: {handle}, ResultCode: {result})。"));
        }

        return Result<bool>.Ok(true);
    }
}
