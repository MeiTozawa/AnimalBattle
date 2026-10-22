namespace AnimalBattle.Configs;

/// <summary>
/// 設定ファイルの探索パスを解決するインターフェース。
/// </summary>
public interface IConfigPathResolver {
    /// <summary>
    /// 設定ファイルの物理フルパスを探索します。
    /// </summary>
    /// <param name="fileName">対象ファイル名または相対パス</param>
    /// <returns>解決されたフルパス。見つからない場合は null</returns>
    string? ResolveConfigPath(string fileName);
}
