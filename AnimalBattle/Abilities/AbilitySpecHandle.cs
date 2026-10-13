namespace AnimalBattle.Abilities;

/// <summary>
///     アビリティシステムコンポーネント（ASC）内で付与された技能スペックを一意に識別する
///     UE GAS 準拠の軽量な値型ハンドル構造体です。
/// </summary>
public readonly record struct AbilitySpecHandle(int Value) {
    private static int _nextHandle;

    /// <summary>無効なハンドルを表します。</summary>
    public static readonly AbilitySpecHandle Invalid = new(0);

    /// <summary>ハンドルが有効（1 以上）であるかを取得します。</summary>
    public bool IsValid => Value > 0;

    /// <summary>新しい一意のスペックハンドルを生成します。</summary>
    public static AbilitySpecHandle Generate() => new(Interlocked.Increment(ref _nextHandle));

    public override string ToString() => $"AbilityHandle({Value})";
}
