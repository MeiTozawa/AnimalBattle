namespace AnimalBattle.Components;

/// <summary>
/// 弾幕 Boss エンティティを識別するためのタグ構造体。
/// </summary>
[Require<Transform>]
[Require<Velocity>]
[Require<CollisionBox>]
[Require<Health>]
[Require<BossStats>]
[Require<BossStateMachine>]
public struct BossTag
{
}
