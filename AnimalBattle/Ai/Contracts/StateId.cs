namespace AnimalBattle.Ai.Contracts;

/// <summary>
/// 状態機で使用する安定した状態識別子を表します。
/// </summary>
public readonly record struct StateId
{
    /// <summary>
    /// 未指定状態を表す値です。
    /// </summary>
    public static StateId Unknown => default;

    /// <summary>
    /// 状態識別子の整数値を取得します。
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// 識別子が登録可能な正の値かどうかを取得します。
    /// </summary>
    public bool IsValid => Value > 0;

    /// <summary>
    /// 指定した整数値から状態識別子を生成します。
    /// </summary>
    /// <param name="value">設定する安定した整数値。</param>
    public StateId(int value)
    {
        Value = value;
    }

    /// <summary>
    /// 整数値を状態識別子へ変換します。
    /// </summary>
    /// <param name="value">変換する整数値。</param>
    public static implicit operator StateId(int value) => new(value);

    /// <summary>
    /// 状態識別子を整数値へ変換します。
    /// </summary>
    /// <param name="stateId">変換する状態識別子。</param>
    public static explicit operator int(StateId stateId) => stateId.Value;
}
