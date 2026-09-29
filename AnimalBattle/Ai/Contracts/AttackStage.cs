namespace AnimalBattle.Ai.Contracts;

/// <summary>
///     攻撃ライフサイクルの段階を定義します。
/// </summary>
public enum AttackStage {
    /// <summary>
    ///     攻撃段階が未指定であることを示します。
    /// </summary>
    None = 0,

    /// <summary>
    ///     攻撃の予兆を表示する段階を示します。
    /// </summary>
    Telegraph = 1,

    /// <summary>
    ///     攻撃判定や弾を一度だけ実行する段階を示します。
    /// </summary>
    Execute = 2,

    /// <summary>
    ///     攻撃後の隙を処理する段階を示します。
    /// </summary>
    Recovery = 3
}