namespace AnimalBattle.Ai.Contracts;

/// <summary>
///     Boss AI の診断および安全復帰で使用するエラー分類を定義します。
/// </summary>
public enum ErrorCode {
    /// <summary>
    ///     エラーがないことを示します。
    /// </summary>
    None = 0,

    /// <summary>
    ///     StateId が無効であることを示します。
    /// </summary>
    InvalidStateId = 1,

    /// <summary>
    ///     BossSkill が無効であることを示します。
    /// </summary>
    InvalidBossSkill = 2,


    /// <summary>
    ///     Phase が無効であることを示します。
    /// </summary>
    InvalidPhase = 3,

    /// <summary>
    ///     タイマーまたは経過時間が無効であることを示します。
    /// </summary>
    InvalidTimer = 4,

    /// <summary>
    ///     必須依存関係が不足していることを示します。
    /// </summary>
    MissingDependency = 5,

    /// <summary>
    ///     StateId または module が固定 map に対応していないことを示します。
    /// </summary>
    UnregisteredState = 6,

    /// <summary>
    ///     Dead 状態などの終端制約に違反したことを示します。
    /// </summary>
    TerminalStateViolation = 7,

    /// <summary>
    ///     許可されていない状態遷移を示します。
    /// </summary>
    TransitionNotAllowed = 8,

    /// <summary>
    ///     即時遷移の循環または再帰を検出したことを示します。
    /// </summary>
    TransitionCycle = 9,

    /// <summary>
    ///     ライフサイクルの呼び出し規則に違反したことを示します。
    /// </summary>
    LifecycleViolation = 10,

    /// <summary>
    ///     State module の処理が失敗したことを示します。
    /// </summary>
    ModuleExecutionFailed = 11,

    /// <summary>
    ///     設定値の検証に失敗したことを示します。
    /// </summary>
    InvalidConfiguration = 12,

    /// <summary>
    ///     診断または Observation の記録に失敗したことを示します。
    /// </summary>
    ObservationFailed = 13,

    /// <summary>
    ///     安全復帰処理が失敗したことを示します。
    /// </summary>
    SafetyRecoveryFailed = 14,

    /// <summary>
    ///     未分類の実行エラーを示します。
    /// </summary>
    ExecutionFailed = 15
}