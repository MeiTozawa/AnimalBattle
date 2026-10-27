using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Combat.Events;

/// <summary>
///     被弾判定イベントデータ。ヒープ割り当てゼロの値型構造体。
/// </summary>
public readonly record struct HitEvent
{
    /// <summary>被弾対象エンティティ。</summary>
    public required Entity Victim { get; init; }

    /// <summary>基本ダメージ量。</summary>
    public required float Damage { get; init; }

    /// <summary>攻撃元の所属陣営。</summary>
    public required Faction SourceFaction { get; init; }

    /// <summary>ノックバック物理インパルス。</summary>
    public required Vec2f Knockback { get; init; }

    /// <summary>強打撃・重撃判定フラグ。</summary>
    public required bool IsHeavy { get; init; }
}
