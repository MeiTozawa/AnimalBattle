using AnimalBattle.Ai.Abilities;

namespace AnimalBattle.Ai.Contracts;

/// <summary>
///     Boss 意思決定ブレインの共通インターフェースです。
/// </summary>
public interface IBossBrain
{
    /// <summary>現在アクティブな能力システムコンポーネントを取得します。</summary>
    AbilitySystemComponent AbilitySystem { get; }

    /// <summary>現在の戦闘フェーズを取得します。</summary>
    Phase Phase { get; }

    /// <summary>現在の Boss 統一マクロ状態を取得します。</summary>
    BossState CurrentState { get; }

    /// <summary>現在発動中の技能を取得します。</summary>
    BossSkill CurrentSkill { get; }

    /// <summary>現在状態の経過時間を取得します。</summary>
    float StateTimer { get; }

    /// <summary>現在状態の目標持続時間を取得します。</summary>
    float StateDuration { get; }

    /// <summary>AI 目標地点座標を取得します。</summary>
    Vec2f TargetPosition { get; }

    /// <summary>AI 目標地点 X 座標を取得します。</summary>
    float TargetX => TargetPosition.X;

    /// <summary>AI 目標地点 Y 座標を取得します。</summary>
    float TargetY => TargetPosition.Y;

    /// <summary>現在の Boss 座標を取得します。</summary>
    Vec2f Position { get; }

    /// <summary>Boss が後隙による易傷状態であるかを取得します。</summary>
    bool IsVulnerable { get; }

    /// <summary>Boss が右向きであるかを取得します。</summary>
    bool FacingRight => true;

    /// <summary>死亡状態かどうかを取得します。</summary>
    bool IsDead { get; }

    /// <summary>弾幕波次発射イベント。</summary>
    event Action<BossSkill, Vec2f, Vec2f>? OnEmitBarrageWave;

    /// <summary>全弾幕消去イベント。</summary>
    event Action? OnClearBullets;

    /// <summary>死亡処理を実行します。</summary>
    void Kill();
}
