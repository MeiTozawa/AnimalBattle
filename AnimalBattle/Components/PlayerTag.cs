namespace AnimalBattle.Components;

/// <summary>
/// プレイヤーエンティティを識別するためのタグ構造体。
/// Bevy 風の Required Components 属性により、必要な物理・描画・入力コンポーネントが自動補完されます。
/// </summary>
[Require<Transform>]
[Require<Velocity>]
[Require<CollisionBox>]
[Require<Input>]
[Require<Sprite>]
[Require<Camera>]
[Require<Animation>]
public struct PlayerTag
{
}
