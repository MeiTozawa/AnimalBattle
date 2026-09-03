namespace AnimalBattle.Ai.Contracts;

/// <summary>
/// Boss AI の処理結果と、必要な診断および安全復帰情報を表します。
/// </summary>
public readonly record struct DiagnosticResult
{
    /// <summary>
    /// 成功した診断結果を生成します。
    /// </summary>
    /// <param name="message">成功内容を表す簡潔な日本語メッセージ。</param>
    /// <returns>成功を表す診断結果。</returns>
    public static DiagnosticResult Success(string message = "正常終了") => new(
        true,
        DiagnosticSeverity.Information,
        ErrorCode.None,
        message,
        default,
        default,
        Phase.Unknown,
        0,
        0,
        UpdateSource.NewRuntime,
        default);

    /// <summary>
    /// 処理を継続できる警告を表す診断結果を生成します。
    /// </summary>
    /// <param name="errorCode">警告の分類。</param>
    /// <param name="message">原因、影響、継続処理を表す簡潔な日本語メッセージ。</param>
    /// <param name="stateId">診断対象の状態識別子。</param>
    /// <param name="skill">診断対象の技能識別子。</param>
    /// <param name="phase">診断時点の Phase。</param>
    /// <param name="entityId">診断対象の Entity 識別子。</param>
    /// <param name="frameUpdateNumber">診断時点のフレーム更新番号。</param>
    /// <param name="updateSource">固定した値の更新元。</param>
    /// <param name="recoveryBossState">継続処理に使用する安全状態識別子。</param>
    /// <returns>継続可能な警告を表す診断結果。</returns>
    public static DiagnosticResult Warning(
        ErrorCode errorCode,
        string message,
        BossState stateId = default,
        BossSkill skill = default,
        Phase phase = Phase.Unknown,
        ulong entityId = 0,
        long frameUpdateNumber = 0,
        UpdateSource updateSource = UpdateSource.NewRuntime,
        BossState recoveryBossState = default) => new(
            true,
            DiagnosticSeverity.Warning,
            errorCode,
            message,
            stateId,
            skill,
            phase,
            entityId,
            frameUpdateNumber,
            updateSource,
            recoveryBossState);

    /// <summary>
    /// 失敗または警告を表す診断結果を生成します。
    /// </summary>
    /// <param name="severity">診断の重大度。</param>
    /// <param name="errorCode">エラー分類。</param>
    /// <param name="message">原因を表す簡潔な日本語メッセージ。</param>
    /// <param name="stateId">診断対象の状態識別子。</param>
    /// <param name="skill">診断対象の技能識別子。</param>
    /// <param name="phase">診断時点の Phase。</param>
    /// <param name="entityId">診断対象の Entity 識別子。</param>
    /// <param name="frameUpdateNumber">診断時点のフレーム更新番号。</param>
    /// <param name="updateSource">値を更新した責務の出所。</param>
    /// <param name="recoveryBossState">安全復帰先として使用する状態識別子。</param>
    /// <returns>指定された診断情報を持つ結果。</returns>
    public static DiagnosticResult Failure(
        DiagnosticSeverity severity,
        ErrorCode errorCode,
        string message,
        BossState stateId = default,
        BossSkill skill = default,
        Phase phase = Phase.Unknown,
        ulong entityId = 0,
        long frameUpdateNumber = 0,
        UpdateSource updateSource = UpdateSource.NewRuntime,
        BossState recoveryBossState = default) => new(
            false,
            severity,
            errorCode,
            message,
            stateId,
            skill,
            phase,
            entityId,
            frameUpdateNumber,
            updateSource,
            recoveryBossState);

    /// <summary>
    /// 処理が成功したかどうかを取得します。
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// 処理が失敗したかどうかを取得します。
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// 診断の重大度を取得します。
    /// </summary>
    public DiagnosticSeverity Severity { get; }

    /// <summary>
    /// 診断に対応するエラー分類を取得します。
    /// </summary>
    public ErrorCode ErrorCode { get; }

    /// <summary>
    /// 簡潔な日本語の診断メッセージを取得します。
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// 診断対象の状態識別子を取得します。
    /// </summary>
    public BossState State { get; }

    /// <summary>
    /// 診断対象の技能識別子を取得します。
    /// </summary>
    public BossSkill Skill { get; }

    /// <summary>
    /// 診断時点の Phase を取得します。
    /// </summary>
    public Phase Phase { get; }

    /// <summary>
    /// 診断対象の Entity 識別子を取得します。
    /// </summary>
    public ulong EntityId { get; }

    /// <summary>
    /// 診断時点のフレーム更新番号を取得します。
    /// </summary>
    public long FrameUpdateNumber { get; }

    /// <summary>
    /// 値を更新した責務の出所を取得します。
    /// </summary>
    public UpdateSource UpdateSource { get; }

    /// <summary>
    /// 異常時に使用する安全復帰先の状態識別子を取得します。
    /// </summary>
    public BossState RecoveryState { get; }

    private DiagnosticResult(
        bool isSuccess,
        DiagnosticSeverity severity,
        ErrorCode errorCode,
        string message,
        BossState stateId,
        BossSkill skill,
        Phase phase,
        ulong entityId,
        long frameUpdateNumber,
        UpdateSource updateSource,
        BossState recoveryBossState)
    {
        ArgumentNullException.ThrowIfNull(message);
        IsSuccess = isSuccess;
        Severity = severity;
        ErrorCode = errorCode;
        Message = message;
        State = stateId;
        Skill = skill;
        Phase = phase;
        EntityId = entityId;
        FrameUpdateNumber = frameUpdateNumber;
        UpdateSource = updateSource;
        RecoveryState = recoveryBossState;
    }

}
