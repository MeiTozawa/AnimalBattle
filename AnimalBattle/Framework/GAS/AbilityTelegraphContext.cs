using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Abilities;

/// <summary>
///     アビリティの前兆：Telegraph描画時に必要な空間・進行状態コンテキストです。
///     GAS 汎用層として施法者：Sourceと対象：Targetの空間幾何情報、進行度、および描画・アリーナ設定を保持します。
/// </summary>
public readonly record struct AbilityTelegraphContext(
    Vec2f SourcePos,
    Vec2f TargetPos,
    bool FacingRight,
    float Progress,
    float StateTimer,
    float SourceHeight,
    BossEffectsRenderConfig? Effects = null,
    ArenaConfig? Arena = null);
