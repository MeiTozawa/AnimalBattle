using AnimalBattle.Ai;
using AnimalBattle.Core;

namespace AnimalBattle.Bosses.Components;

/// <summary>
///     Boss の戦闘状態（フェーズ、発動中スキル、タイマー、向き等）を一元管理するコンポーネント。
///     HUD、予兆描画、デバッグ等の外部システムはこのコンポーネントの値を参照します。
/// </summary>
public struct BossCombatState(BossType bossType = BossType.Boss1, Phase phase = Phase.Phase1) {
    /// <summary>Boss の種別（Boss1 / Boss2）。</summary>
    public BossType BossType = bossType;

    /// <summary>現在の Boss 進行フェーズ。</summary>
    public Phase Phase = phase;

    /// <summary>現在発動中のスキルタグ。</summary>
    public StringId CurrentSkillTag = StringId.Empty;

    /// <summary>現在の状態・スキルの経過時間（秒）。</summary>
    public float StateTimer = 0.0f;

    /// <summary>AI 目標地点・着地予想座標。</summary>
    public Vec2f TargetPosition = Vec2f.Zero;

    /// <summary>Boss が右向きかどうか。</summary>
    public bool FacingRight = true;
}