namespace AnimalBattle.Ai.Contracts;

/// <summary>
/// 攻撃 registry で使用する安定した攻撃識別子を表します。
/// </summary>
public readonly record struct AttackId
{
    /// <summary>
    /// 未指定攻撃を表す値です。
    /// </summary>
    public static AttackId Unknown => default;

    /// <summary>
    /// 攻撃識別子の整数値を取得します。
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// 識別子が登録可能な正の値かどうかを取得します。
    /// </summary>
    public bool IsValid => Value > 0;

    /// <summary>
    /// 指定した整数値から攻撃識別子を生成します。
    /// </summary>
    /// <param name="value">設定する安定した整数値。</param>
    public AttackId(int value)
    {
        Value = value;
    }

    /// <summary>
    /// 整数値を攻撃識別子へ変換します。
    /// </summary>
    /// <param name="value">変換する整数値。</param>
    public static implicit operator AttackId(int value) => new(value);

    /// <summary>
    /// 攻撃識別子を整数値へ変換します。
    /// </summary>
    /// <param name="attackId">変換する攻撃識別子。</param>
    public static explicit operator int(AttackId attackId) => attackId.Value;
}
