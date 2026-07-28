using System;

namespace AnimalBattle.Core;

/// <summary>
/// 当たり判定オブジェクトのカテゴリ領域を表すビットフラグ列挙型。
/// </summary>
[Flags]
public enum CollisionCategory : uint
{
    /// <summary>なし</summary>
    None = 0,

    /// <summary>キャラクター喰らい判定</summary>
    CharacterBody = 1 << 0,

    /// <summary>近接攻撃ヒット判定</summary>
    AttackHitbox = 1 << 1,

    /// <summary>飛び道具判定</summary>
    Projectile = 1 << 2,

    /// <summary>環境・壁障害物判定</summary>
    Environment = 1 << 3,

    /// <summary>アイテム・拾い物判定</summary>
    Item = 1 << 4,

    /// <summary>すべてのカテゴリ判定</summary>
    All = uint.MaxValue
}
