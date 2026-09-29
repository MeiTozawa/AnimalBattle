namespace AnimalBattle.Ai.Contracts;

/// <summary>
///     状態遷移を発生させた理由の分類を定義します。
/// </summary>
public enum ReasonKind {
    /// <summary>
    ///     遷移理由が未指定であることを示します。
    /// </summary>
    Unknown = 0,

    /// <summary>
    ///     初期状態への入場を示します。
    /// </summary>
    InitialEntry = 1,

    /// <summary>
    ///     Phase 閾値に到達したことを示します。
    /// </summary>
    PhaseThreshold = 2,

    /// <summary>
    ///     攻撃が完了したことを示します。
    /// </summary>
    AttackCompleted = 3,

    /// <summary>
    ///     状態または攻撃のタイマーが満了したことを示します。
    /// </summary>
    TimerElapsed = 4,

    /// <summary>
    ///     プレイヤー観測に基づく判断を示します。
    /// </summary>
    PlayerObservation = 5,

    /// <summary>
    ///     設定値の境界で遷移したことを示します。
    /// </summary>
    Configuration = 6,

    /// <summary>
    ///     エラーからの復帰を示します。
    /// </summary>
    ErrorRecovery = 7,

    /// <summary>
    ///     外部から明示的に要求された遷移を示します。
    /// </summary>
    ExternalRequest = 8,

    /// <summary>
    ///     ライフサイクル違反への安全復帰を示します。
    /// </summary>
    LifecycleViolation = 9,

    /// <summary>
    ///     安全な代替状態への復帰を示します。
    /// </summary>
    SafetyFallback = 10,

    /// <summary>
    ///     Boss の死亡を示します。
    /// </summary>
    Death = 11
}