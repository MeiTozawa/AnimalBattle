using Flecs.NET.Core;

namespace AnimalBattle.Components;

/// <summary>
///     自機が所持する常駐近接攻撃エンティティへの参照コンポーネント。
/// </summary>
public readonly struct PlayerMeleeWeapon(Entity entity)
{
    /// <summary>常駐する近接斬撃エンティティ句柄。</summary>
    public readonly Entity Entity = entity;
}
