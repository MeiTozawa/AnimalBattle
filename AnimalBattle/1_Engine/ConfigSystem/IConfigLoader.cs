namespace AnimalBattle.Configs;

using ErrorOr;

/// <summary>
/// 設定データの初期読み込みおよび破棄を抽象化するローダーインターフェース。
/// </summary>
/// <typeparam name="T">対象の設定クラスの型</typeparam>
public interface IConfigLoader<T> : IDisposable where T : class
{
    /// <summary>
    /// 設定インスタンスの初期ロードを実行します。
    /// </summary>
    /// <returns>読み込み結果</returns>
    ErrorOr<T> LoadInitial();
}
