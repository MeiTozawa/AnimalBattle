using System;

namespace AnimalBattle.Characters;

/// <summary>
/// キャラクターのアクション状態を表す列挙型。
/// </summary>
public enum CharacterState
{
    /// <summary>
    /// 接地状態（地上移動可能）。
    /// </summary>
    Grounded,

    /// <summary>
    /// 空中状態（落下またはジャンプ中）。
    /// </summary>
    Airborne,

    /// <summary>
    /// 急降下状態。
    /// </summary>
    FastFalling,

    /// <summary>
    /// ヒットスタン（ふっとび硬直）状態。
    /// </summary>
    Hitstun
}
