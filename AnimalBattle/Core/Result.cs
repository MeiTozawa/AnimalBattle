namespace AnimalBattle.Core;

/// <summary>
/// 成功時の値または失敗時のエラーのいずれかを保持する Result パターン構造体。
/// </summary>
/// <typeparam name="T">成功時の値の型</typeparam>
public readonly record struct Result<T>
{
    private Result(T value)
    {
        Value = value;
        Error = default;
        IsOk = true;
    }

    private Result(SimpleError error)
    {
        Value = default;
        Error = error;
        IsOk = false;
    }

    /// <summary>
    /// 成功時の値を取得します。失敗時は default が返されます。
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// 失敗時のエラーオブジェクトを取得します。成功時は default が返されます。
    /// </summary>
    public SimpleError? Error { get; }

    /// <summary>
    /// 処理が成功したかどうかを示す値を取得します。
    /// </summary>
    public bool IsOk { get; }

    /// <summary>
    /// 成功状態の <see cref="Result{T}"/> インスタンスを生成します。
    /// </summary>
    /// <param name="value">成功時の値</param>
    /// <returns>成功の Result インスタンス</returns>
    public static Result<T> Ok(T value)
    {
        return new Result<T>(value);
    }

    /// <summary>
    /// 失敗状態の <see cref="Result{T}"/> インスタンスを生成します。
    /// </summary>
    /// <param name="error">失敗時のエラー情報</param>
    /// <returns>失敗の Result インスタンス</returns>
    public static Result<T> Err(SimpleError error)
    {
        return new Result<T>(error);
    }

    /// <summary>
    /// 成功時は保持している値を返し、失敗時は指定されたデフォルト値を返します。
    /// </summary>
    /// <param name="defaultValue">失敗時の代替値</param>
    /// <returns>成功値またはデフォルト値</returns>
    public T ValueOrDefault(T defaultValue = default!)
    {
        return IsOk && Value != null ? Value : defaultValue;
    }

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(SimpleError error) => new(error);
}