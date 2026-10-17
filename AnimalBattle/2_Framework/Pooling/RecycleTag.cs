namespace AnimalBattle.Components;

/// <summary>
///     フレーム末尾（Ecs.PostUpdate）でオブジェクトプールへ回収または破棄される対象を示す ECS タグ。
///     各シミュレーションシステムはこのタグを付与するのみで、実際の回収処理は
///     単一責任の <see cref="AnimalBattle.Systems.CombatRecycleSystem" /> が一括遅延実行します。
/// </summary>
public struct RecycleTag { }
