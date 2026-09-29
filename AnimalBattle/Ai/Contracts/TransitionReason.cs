namespace AnimalBattle.Ai.Contracts;

/// <summary>
///     状態遷移を発生させた理由と、その理由を再構成せず保存する payload を表します。
/// </summary>
public readonly record struct TransitionReason {
    /// <summary>
    ///     指定した理由と payload を持つ遷移理由を生成します。
    /// </summary>
    /// <param name="kind">遷移理由の分類。</param>
    /// <param name="threshold">遷移判断に使用した閾値。</param>
    /// <param name="skill">関連する技能識別子。</param>
    /// <param name="errorCode">関連するエラー分類。</param>
    /// <param name="originalInput">遷移を発生させた元入力。</param>
    /// <param name="supplementaryValue">理由に付随する補足値。</param>
    public TransitionReason(
        ReasonKind kind,
        float threshold = 0,
        BossSkill skill = default,
        ErrorCode errorCode = ErrorCode.None,
        long originalInput = 0,
        long supplementaryValue = 0)
    {
        Kind = kind;
        Threshold = threshold;
        Skill = skill;
        ErrorCode = errorCode;
        OriginalInput = originalInput;
        SupplementaryValue = supplementaryValue;
    }

    /// <summary>
    ///     理由が未指定であることを示す値です。
    /// </summary>
    public static TransitionReason None => default;

    /// <summary>
    ///     遷移理由の分類を取得します。
    /// </summary>
    public ReasonKind Kind { get; }

    /// <summary>
    ///     遷移判断に使用した Phase 閾値を取得します。
    /// </summary>
    public float Threshold { get; }

    /// <summary>
    ///     遷移理由に関連する技能識別子を取得します。
    /// </summary>
    public BossSkill Skill { get; }

    /// <summary>
    ///     遷移に関連するエラー分類を取得します。
    /// </summary>
    public ErrorCode ErrorCode { get; }

    /// <summary>
    ///     遷移を発生させた元入力の値を取得します。
    /// </summary>
    public long OriginalInput { get; }

    /// <summary>
    ///     理由に付随する補足値を取得します。
    /// </summary>
    public long SupplementaryValue { get; }


    /// <summary>
    ///     理由に保持された閾値が有限値かどうかを取得します。
    /// </summary>
    public bool HasValidThreshold => !float.IsNaN(Threshold) && !float.IsInfinity(Threshold);
}