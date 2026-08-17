namespace AnimalBattle.Components;

/// <summary>
/// プレイヤーエンティティを識別するためのタグ構造体。
/// Bevy 風の Required Components 属性により、必要な物理・描画・体力・跳躍・状態コンポーネントが自動補完されます。
/// </summary>
[Require<Transform>]
[Require<Velocity>]
[Require<CollisionBox>]
[Require<Health>]
[Require<DoubleJump>]
[Require<PlayerState>]
[Require<Sprite>]
[Require<Animation>]
public struct PlayerTag
{
}
