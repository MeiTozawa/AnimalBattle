namespace AnimalBattle.Combat;

/// <summary>
///     判定盒 (Hitbox/Hurtbox) の所属陣営（味方・敵・中立）を識別する列挙型。
///     異なる陣営間でのみ戦闘ダメージ判定が発生します。
/// </summary>
public enum Faction {
    /// <summary>プレイヤー・味方陣営</summary>
    Friendly,

    /// <summary>Boss・敵性陣営</summary>
    Hostile,

    /// <summary>環境・中立陣営（誰に対してもダメージを与える／受ける）</summary>
    Neutral
}
