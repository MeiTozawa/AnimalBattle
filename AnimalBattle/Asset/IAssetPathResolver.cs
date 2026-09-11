namespace AnimalBattle.Asset;

/// <summary>
/// アセットファイル（画像、スプライトシート等）の物理パスを解決するインターフェース。
/// </summary>
public interface IAssetPathResolver
{
    /// <summary>
    /// リソース相対パスから物理フルパスを解決します。
    /// </summary>
    /// <param name="relativePath">リソース相対パス（例: "Characters\1_Cat_Idle-Sheet.png"）</param>
    /// <returns>解決されたフルパス。見つからない場合は元の相対パス</returns>
    string ResolveAssetPath(string relativePath);
}
