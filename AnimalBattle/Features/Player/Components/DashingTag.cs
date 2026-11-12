namespace AnimalBattle.Player.Components;

/// <summary>
///     自機が突進（ダッシュ）状態中であることを示す一時的 ECS タグ。
///     移動・跳躍システムによる通常キネマティクス更新を除外するために使用されます。
/// </summary>
public readonly struct DashingTag;
