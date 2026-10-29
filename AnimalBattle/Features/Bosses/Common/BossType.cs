namespace AnimalBattle.Ai;

/// <summary>
///     状態機ランタイムが扱う Boss の種別を定義します。
/// </summary>
public enum BossType {
    /// <summary>
    ///     Boss 種別が未指定であることを示します。
    /// </summary>
    Unknown = 0,

    /// <summary>
    ///     BossSnake を示します。
    /// </summary>
    BossSnake = 1,

    /// <summary>
    ///     BossLion を示します。
    /// </summary>
    BossLion = 2,

    /// <summary>
    ///     Boss不在の空シーン（トレーニングモード）を示します。
    /// </summary>
    Empty = 3
}