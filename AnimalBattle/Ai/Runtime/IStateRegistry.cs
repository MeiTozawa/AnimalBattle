using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Runtime;

/// <summary>固定 enum と State_Module の対応を取得する初期化境界です。</summary>
/// <remarks>
/// この契約には登録、削除、差し替え、世代管理の API を定義しません。
/// 実装は初期化時に完全性を検査し、検査に成功した対応だけを実行中に公開します。
/// </remarks>
public interface IEnumModuleMap
{
    /// <summary>固定 BossState 集合の完全性を検証した結果を取得します。</summary>
    EnumCompletenessResult ValidateComplete();

    /// <summary>固定状態に対応する State_Module を取得します。</summary>
    /// <remarks>初期化検査に失敗した map は、いかなる module も返しません。</remarks>
    bool Get(BossState state, out IBossStateModule module);
}

/// <summary>固定 enum/module 完全性検査の結果です。</summary>
public readonly record struct EnumCompletenessResult(
    bool IsSuccess,
    ErrorCode ErrorCode,
    string Message,
    BossState State)
{
    /// <summary>検査が成功したかどうかを取得します。</summary>
    public bool IsValid => IsSuccess;

    /// <summary>検査失敗かどうかを取得します。</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>成功結果を生成します。</summary>
    public static EnumCompletenessResult Success(string message = "固定 BossState の対応は完全です。")
        => new(true, ErrorCode.None, message, BossState.Unknown);

    /// <summary>失敗結果を生成します。</summary>
    public static EnumCompletenessResult Failure(
        ErrorCode errorCode,
        string message,
        BossState state = BossState.Unknown)
        => new(false, errorCode, message, state);
}
