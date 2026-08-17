using System;

namespace AnimalBattle.Core;

/// <summary>
/// 失敗時のエラー情報（説明およびエラー種別）を保持するレコード構造体。
/// </summary>
/// <param name="Description">エラーの詳細説明</param>
/// <param name="Type">エラーの種別</param>
public readonly record struct SimpleError(string Description, ErrorType Type)
{
    /// <summary>
    /// 対象が見つからない場合（NotFound）のエラーオブジェクトを生成します。
    /// </summary>
    /// <param name="description">エラーの説明</param>
    /// <returns><see cref="SimpleError"/> インスタンス</returns>
    public static SimpleError NotFound(string description)
    {
        return new SimpleError(description, ErrorType.NotFound);
    }

    /// <summary>
    /// 値や状態が無効である場合（NotValid）のエラーオブジェクトを生成します。
    /// </summary>
    /// <param name="description">エラーの説明</param>
    /// <returns><see cref="SimpleError"/> インスタンス</returns>
    public static SimpleError NotValid(string description)
    {
        return new SimpleError(description, ErrorType.NotValid);
    }

    /// <summary>
    /// 引数が有効範囲外である場合（ArgumentOutOfRange）のエラーオブジェクトを生成します。
    /// </summary>
    /// <param name="description">エラーの説明</param>
    /// <returns><see cref="SimpleError"/> インスタンス</returns>
    public static SimpleError ArgumentOutOfRange(string description)
    {
        return new SimpleError(description, ErrorType.ArgumentOutOfRange);
    }

    /// <summary>
    /// システム例外（SystemException）によるエラーオブジェクトを生成します。
    /// </summary>
    /// <param name="ex">発生した例外オブジェクト</param>
    /// <returns><see cref="SimpleError"/> インスタンス</returns>
    public static SimpleError Exception(Exception ex)
    {
        return new SimpleError(ex.Message, ErrorType.SystemException);
    }
}
