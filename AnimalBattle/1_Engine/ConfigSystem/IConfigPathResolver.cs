namespace AnimalBattle.Configs;

/// <summary>
///     設定ファイルおよびベイクドバイナリの探索パスを解決するインターフェース。
/// </summary>
public interface IConfigPathResolver {
    /// <summary>
    ///     TOML 設定ファイルの物理フルパスを探索します。
    /// </summary>
    /// <param name="fileName">対象ファイル名または相対パス</param>
    /// <returns>解決されたフルパス。見つからない場合は null</returns>
    string? ResolveConfigPath(string fileName);

    /// <summary>
    ///     ベイクドバイナリ設定ファイル (baked_configs.bin) のストリームを開きます。
    ///     物理ファイルを優先し、存在しない場合は埋め込みリソースから取得します。
    /// </summary>
    /// <param name="overridePath">明示的なファイルパス（省略可能）</param>
    /// <returns>オープンされたストリーム。見つからない場合は null</returns>
    Stream? OpenBinaryStream(string? overridePath = null);
}