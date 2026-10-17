namespace AnimalBattle.Abilities;

/// <summary>
///     アビリティの前兆（Telegraph）描画時に必要な空間・進行状態コンテキストです。
/// </summary>
public readonly record struct AbilityTelegraphContext(
    float BossX,
    float BossY,
    float TargetX,
    float TargetY,
    bool FacingRight,
    float Progress,
    float StateTimer);
