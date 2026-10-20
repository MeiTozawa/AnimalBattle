using AnimalBattle.Ai;
using AnimalBattle.Core;

namespace AnimalBattle.Components;

/// <summary>
///     Boss の戦闘状態（フェーズ、状態、発動中スキル、タイマー、向き等）を一元管理するコンポーネント。
///     HUD、予兆描画、デバッグ等の外部システムはこのコンポーネントの値を参照します。
/// </summary>
public struct BossCombatState
{
    /// <summary>Boss の種別（Boss1 / Boss2）。</summary>
    public BossType BossType;

    /// <summary>現在の Boss 進行フェーズ。</summary>
    public Phase Phase;

    /// <summary>現在の Boss の統一マクロ状態。</summary>
    public BossState State;

    /// <summary>現在発動中のスキルタグ。</summary>
    public StringId CurrentSkillTag;

    /// <summary>現在の状態・スキルの経過時間（秒）。</summary>
    public float StateTimer;

    /// <summary>AI 目標地点・着地予想座標。</summary>
    public Vec2f TargetPosition;

    /// <summary>Boss が右向きかどうか。</summary>
    public bool FacingRight;

    public BossCombatState(Phase phase = Phase.Phase1) : this(BossType.Boss1, phase)
    {
    }

    public BossCombatState(BossType bossType, Phase phase = Phase.Phase1)
    {
        BossType = bossType;
        Phase = phase;
        State = BossState.Unknown;
        CurrentSkillTag = StringId.Empty;
        StateTimer = 0.0f;
        TargetPosition = Vec2f.Zero;
        FacingRight = true;
    }
}