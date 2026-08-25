using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Runtime;

/// <summary>固定 enum と State_Module の対応を取得する初期化境界です。</summary>
public interface IEnumModuleMap
{
    /// <summary>固定 BossState 集合の完全性を検証します。</summary>
    EnumCompletenessResult ValidateComplete();
    /// <summary>固定状態に対応する State_Module を取得します。</summary>
    bool TryGet(BossState state, out IBossStateModule module);
}

/// <summary>旧 registry 名との互換境界です。実行時登録 API は提供しません。</summary>
[Obsolete("IEnumModuleMap を使用してください。")]
public interface IStateRegistry : IEnumModuleMap
{
    /// <summary>固定 module 集合を検証します。</summary>
    RegistryValidationResult Validate();
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
    public static EnumCompletenessResult Failure(ErrorCode errorCode, string message, BossState state = BossState.Unknown)
        => new(false, errorCode, message, state);
}

/// <summary>固定 module 集合の検証結果です。</summary>
public readonly record struct RegistryValidationResult(
    bool IsSuccess,
    ErrorCode ErrorCode,
    string Message,
    BossState State)
{
    /// <summary>検証が有効だったかどうかを取得します。</summary>
    public bool IsValid => IsSuccess;
    /// <summary>検証失敗かどうかを取得します。</summary>
    public bool IsFailure => !IsSuccess;
    /// <summary>旧プロパティ名です。</summary>
    [Obsolete("State を使用してください。")]
    public BossState StateId => State;

    /// <summary>成功結果を生成します。</summary>
    public static RegistryValidationResult Success(string message = "固定 State_Module は正常です。")
        => new(true, ErrorCode.None, message, BossState.Unknown);
    /// <summary>失敗結果を生成します。</summary>
    public static RegistryValidationResult Failure(ErrorCode errorCode, string message, BossState state = BossState.Unknown)
        => new(false, errorCode, message, state);
}
