using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Runtime;

/// <summary>遷移の検証または適用結果です。</summary>
public readonly record struct TransitionResult
{
    /// <summary>遷移が受理された結果を生成します。</summary>
    public static TransitionResult Accepted(in TransitionRequest request, BossState currentStateId, string message = "遷移しました。")
        => new(true, currentStateId, request.ToStateId, request.SafeRecoveryStateId, request.Reason, ErrorCode.None, message);
    /// <summary>遷移が拒否された結果を生成します。</summary>
    public static TransitionResult Rejected(in TransitionRequest request, BossState currentStateId, ErrorCode errorCode, string message)
        => new(false, currentStateId, request.ToStateId, request.SafeRecoveryStateId, request.Reason, errorCode, message);
    /// <summary>遷移が成功したかどうかを取得します。</summary>
    public bool IsSuccess => IsAccepted;
    /// <summary>遷移が受理されたかどうかを取得します。</summary>
    public bool IsAccepted { get; }
    /// <summary>遷移が拒否されたかどうかを取得します。</summary>
    public bool IsRejected => !IsAccepted;
    /// <summary>拒否時にも保持する有効な現在状態です。</summary>
    public BossState CurrentStateId { get; }
    /// <summary>互換用の現在状態名です。</summary>
    [Obsolete("CurrentStateId を使用してください。")]
    public BossState CurrentBossState => CurrentStateId;
    /// <summary>遷移要求先状態です。</summary>
    public BossState RequestedStateId { get; }
    /// <summary>安全復帰先です。</summary>
    public BossState SafeRecoveryStateId { get; }
    /// <summary>要求された完全な遷移理由です。</summary>
    public TransitionReason Reason { get; }
    /// <summary>失敗時のエラー分類です。</summary>
    public ErrorCode ErrorCode { get; }
    /// <summary>簡潔な日本語の結果メッセージです。</summary>
    public string Message { get; }

    private TransitionResult(bool isAccepted, BossState currentStateId, BossState requestedStateId, BossState safeRecoveryStateId, TransitionReason reason, ErrorCode errorCode, string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        IsAccepted = isAccepted;
        CurrentStateId = currentStateId;
        RequestedStateId = requestedStateId;
        SafeRecoveryStateId = safeRecoveryStateId;
        Reason = reason;
        ErrorCode = errorCode;
        Message = message;
    }
}
