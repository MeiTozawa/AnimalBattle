using AnimalBattle.Ai.Contracts;
using Flecs.NET.Core;

namespace AnimalBattle.Characters.Boss;

/// <summary>
///     多態 Boss 自治エージェントの統一抽象契約。
///     Pawn 基底クラスと連携し、AI 状態機械・行動評価・指令適用のライフサイクルを統括します。
/// </summary>
public interface IBossAgent : IDisposable
{
    /// <summary>Boss の種別を取得します。</summary>
    BossType BossType { get; }

    /// <summary>現在の Phase を取得します。</summary>
    Phase Phase { get; }

    /// <summary>現在の Boss 状態を取得します。</summary>
    BossState CurrentState { get; }

    /// <summary>現在選択されている攻撃技能種別を取得します。</summary>
    BossSkill CurrentSkill { get; }

    /// <summary>現在選択されている攻撃技能名を取得します。</summary>
    string SkillName { get; }

    /// <summary>現在状態の経過時間（秒）を取得します。</summary>
    float StateTimer { get; }

    /// <summary>現在状態の目標持続時間（秒）を取得します。</summary>
    float StateDuration { get; }

    /// <summary>Boss 1 の浮遊タイマー値を取得します。</summary>
    float HoverTimer { get; }

    /// <summary>AI 目標地点 X 座標を取得します。</summary>
    float TargetX { get; }

    /// <summary>AI 目標地点 Y 座標を取得します。</summary>
    float TargetY { get; }

    /// <summary>Boss が後隙による易傷状態であるかを取得します。</summary>
    bool IsVulnerable { get; }

    /// <summary>Boss が右向きであるかを取得します。</summary>
    bool FacingRight { get; }

    /// <summary>
    ///     フレームごとの Boss AI 評価、移動・弾幕・演出コマンド適用を実行します。
    /// </summary>
    void Update(
        float deltaTime,
        Vec2f playerPosition,
        Vec2f playerVelocity,
        World world,
        Query<Transform, CollisionBox> platformQuery);

    /// <summary>
    ///     Boss の状態を初期位置および初期パラメータへリセットします。
    /// </summary>
    void Reset();
}
