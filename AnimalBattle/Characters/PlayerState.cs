namespace AnimalBattle.Characters;

/// <summary>
///     プレイヤー自機の高レベル動作状態を表す状態マシン列挙型。
/// </summary>
public enum PlayerState {
    /// <summary>
    ///     死亡状態（HPは0以下）。
    /// </summary>
    Dead,

    /// <summary>被弾硬直・無敵点滅状態。</summary>
    Hurt,

    /// <summary>接地状態（立ち・歩行・射撃など）。</summary>
    Grounded,

    /// <summary>空中状態（上昇・落下など）。</summary>
    Airborne
}
