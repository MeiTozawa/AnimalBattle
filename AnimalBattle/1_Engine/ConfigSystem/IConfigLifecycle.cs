namespace AnimalBattle.Configs;

using ErrorOr;

/// <summary>
/// 設定オブジェクトの検証および後処理ライフサイクルを定義するインターフェース。
/// </summary>
public interface IConfigLifecycle
{
    /// <summary>
    /// 設定値のセマンティクス妥当性を検証します。
    /// </summary>
    /// <returns>検証成功時は Success、異常時は Error</returns>
    ErrorOr<Success> Validate();

    /// <summary>
    /// キャッシュ構築や派生パラメータの事前計算を実行します。
    /// </summary>
    void PostProcess() { }
}
