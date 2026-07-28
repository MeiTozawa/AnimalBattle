using System;

namespace AnimalBattle.Core;

/// <summary>
/// 物理オブジェクトの環境衝突状態（床、天井、壁など）を表すビットフラグ列挙型。
/// </summary>
[Flags]
public enum CollisionFlags : int
{
    /// <summary>衝突なし</summary>
    None = 0,

    /// <summary>床（着地）判定</summary>
    Floor = 1 << 0,

    /// <summary>天井（頭ぶつけ）判定</summary>
    Ceiling = 1 << 1,

    /// <summary>左壁判定</summary>
    WallLeft = 1 << 2,

    /// <summary>右壁判定</summary>
    WallRight = 1 << 3,

    /// <summary>半実体足場の上に乗っている判定</summary>
    OnSemiSolid = 1 << 4
}
