using AnimalBattle.Ai;

namespace AnimalBattle.Features.Tutorial.Components;

/// <summary>
///     チュートリアル終盤に配置されるボス挑戦扉を表すコンポーネント。
/// </summary>
public struct TutorialDoor
{
    /// <summary>この扉に入った際に挑戦するボスの種別。</summary>
    public BossType TargetBoss;

    /// <summary>インタラクション可能な近接半径（ピクセル）。</summary>
    public float InteractionRadius;

    /// <summary>プレイヤーが現在扉の範囲内にいるかどうか。</summary>
    public bool IsPlayerInRange;

    /// <summary>扉のアニメーション／呼吸演出用タイマー。</summary>
    public float AnimTimer;
}
