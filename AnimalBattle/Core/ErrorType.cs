namespace AnimalBattle.Core;

/// <summary>
/// エラーの種別を定義する列挙型。
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// システム例外が発生したことを示します。
    /// </summary>
    SystemException,

    /// <summary>
    /// 入力パラメータや状態が無効であることを示します。
    /// </summary>
    NotValid,

    /// <summary>
    /// 引数の値が許容範囲外であることを示します。
    /// </summary>
    ArgumentOutOfRange,

    /// <summary>
    /// 指定されたリソースやファイルが見つからないことを示します。
    /// </summary>
    NotFound
}
