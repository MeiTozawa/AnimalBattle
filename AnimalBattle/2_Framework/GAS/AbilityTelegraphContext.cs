namespace AnimalBattle.Abilities;

/// <summary>
///     アビリティの前兆（Telegraph）描画時に必要な空間・進行状態コンテキストです。
///     GAS 汎用層として施法者（Source）と対象（Target）の空間幾何情報および進行度を保持します。
/// </summary>
public readonly record struct AbilityTelegraphContext(
    float SourceX,
    float SourceY,
    float TargetX,
    float TargetY,
    bool FacingRight,
    float Progress,
    float StateTimer,
    float SourceHeight = 0.0f);

