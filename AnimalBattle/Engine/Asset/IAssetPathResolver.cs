namespace AnimalBattle.Asset;

/// <summary>
///     画像やスプライトシートなどのアセットファイルの物理パスを解決するインターフェースです。
/// </summary>
public interface IAssetPathResolver {
    /// <summary>
    ///     リソース相対パスから物理フルパスを解決します。
    /// </summary>
    /// <param name="relativePath">Characters\1_Cat_Idle-Sheet.pngなどのリソース相対パス</param>
    /// <returns>解決されたフルパス。見つからない場合は元の相対パスを返します</returns>
    string ResolveAssetPath(string relativePath);
}