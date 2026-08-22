namespace AnimalBattle.Ai.Contracts;

/// <summary>
/// 診断結果の重大度を定義します。
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>
    /// 通常の情報を示します。
    /// </summary>
    Information = 0,

    /// <summary>
    /// 設定や入力に関する警告を示します。
    /// </summary>
    Warning = 1,

    /// <summary>
    /// 遷移が拒否されたことを示します。
    /// </summary>
    TransitionRejected = 2,

    /// <summary>
    /// 実行処理が失敗したことを示します。
    /// </summary>
    ExecutionError = 3
}
