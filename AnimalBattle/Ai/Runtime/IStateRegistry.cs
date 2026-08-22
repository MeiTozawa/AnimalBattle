using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Runtime;

/// <summary>
/// State_Id と State_Module の登録を検証して取得する境界です。
/// </summary>
public interface IStateRegistry
{
    /// <summary>現在の登録内容を検証します。</summary>
    RegistryValidationResult Validate();

    /// <summary>登録済みの State_Module を取得します。</summary>
    bool TryGet(StateId stateId, out IBossStateModule module);
}

/// <summary>
/// Registry の検証または登録結果を表します。
/// </summary>
public readonly record struct RegistryValidationResult
{
    /// <summary>成功結果を生成します。</summary>
    public static RegistryValidationResult Success(long generation, string message = "Registry は正常です。") => new(true, generation, ErrorCode.None, message, default);

    /// <summary>失敗結果を生成します。</summary>
    public static RegistryValidationResult Failure(ErrorCode errorCode, string message, StateId stateId = default, long generation = 0) => new(false, generation, errorCode, message, stateId);

    /// <summary>検証または登録が成功したかどうかを取得します。</summary>
    public bool IsSuccess { get; }

    /// <summary>検証または登録が有効だったかどうかを取得します。</summary>
    public bool IsValid => IsSuccess;

    /// <summary>検証または登録が失敗したかどうかを取得します。</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>登録世代を取得します。</summary>
    public long Generation { get; }

    /// <summary>失敗時のエラー分類を取得します。</summary>
    public ErrorCode ErrorCode { get; }

    /// <summary>簡潔な日本語メッセージを取得します。</summary>
    public string Message { get; }

    /// <summary>失敗対象の State_Id を取得します。</summary>
    public StateId StateId { get; }

    private RegistryValidationResult(bool isSuccess, long generation, ErrorCode errorCode, string message, StateId stateId)
    {
        ArgumentNullException.ThrowIfNull(message);
        IsSuccess = isSuccess;
        Generation = generation;
        ErrorCode = errorCode;
        Message = message;
        StateId = stateId;
    }
}
