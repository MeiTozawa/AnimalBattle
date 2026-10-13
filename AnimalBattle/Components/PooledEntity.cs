using AnimalBattle.CombatPool;

namespace AnimalBattle.Components;

/// <summary>
///     プール管理対象エンティティであることを示す識別コンポーネント。
///     リサイクル時に対応するプールへの返却を O(1) で判定します。
/// </summary>
public readonly record struct PooledEntity(PooledEntityType PoolType);
