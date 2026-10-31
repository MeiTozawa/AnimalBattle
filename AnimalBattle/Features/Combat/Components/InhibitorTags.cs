namespace AnimalBattle.Combat.Components;

/// <summary>
///     能動的攻撃判定・スキル発動を抑制する阻害タグコンポーネント。
///     このタグを保持するエンティティは、攻撃関連システムによりアーキタイプ単位で除外されます。
/// </summary>
public readonly struct CannotAttackTag;

/// <summary>
///     能動的移動・経路追跡を抑制する阻害タグコンポーネント。
///     このタグを保持するエンティティは、能動的移動システムによりアーキタイプ単位で除外されます（重力等の受動的物理は維持）。
/// </summary>
public readonly struct CannotMoveTag;
