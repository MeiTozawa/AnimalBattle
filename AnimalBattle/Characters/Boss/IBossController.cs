using AnimalBattle.Ai.Contracts;
using Flecs.NET.Core;

namespace AnimalBattle.Characters.Boss;

/// <summary>
///     多態 Boss 領域コントローラーの統一抽象契約。
///     高レベル AI 状態遷移、行動評価、および指令適用ライフサイクルを統括します。
/// </summary>
public interface IBossController : IDisposable
{
    /// <summary>Boss の種別を取得します。</summary>
    BossType BossType { get; }

    /// <summary>関連する受控キャラクター領域エンティティ契約を取得します。</summary>
    ICharacter Character { get; }

    /// <summary>現在の HP を取得します。</summary>
    float CurrentHp => Character.CurrentHp;

    /// <summary>最大 HP を取得します。</summary>
    float MaxHp => Character.MaxHp;

    /// <summary>生存中かどうかを取得します。</summary>
    bool IsAlive => Character.IsAlive;

    /// <summary>対応する Flecs エンティティ句柄を取得します。</summary>
    Entity Entity { get; }

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

    /// <summary>Boss が右向きであるかを取得します。</summary>
    bool FacingRight { get; }

    /// <summary>被弾時白閃演出中であるかを取得します。</summary>
    bool IsFlashing { get; }

    /// <summary>
    ///     フレームごとの Boss AI 評価、移動・弾幕・演出コマンド適用を実行します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間（秒）。</param>
    /// <param name="playerPosition">プレイヤー自機の現在位置。</param>
    /// <param name="playerVelocity">プレイヤー自機の現在移動速度。</param>
    /// <param name="world">弾丸・エフェクト生成用の Flecs ワールド。</param>
    /// <param name="platformQuery">プラットフォーム足場検索クエリ。</param>
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
