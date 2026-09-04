using System;

namespace AnimalBattle.Components;

/// <summary>
/// Boss エンティティの受撃判定・易傷特効・被弾白閃状態を管理する汎用戦闘コンポーネント。
/// 具体的な Boss 種類や AI 状態機械から衝突判定ロジックを分離します。
/// </summary>
public struct BossCombatState
{
    /// <summary>無敵状態であるか（ダメージ無効化）。</summary>
    public bool IsInvulnerable;

    /// <summary>易傷状態であるか（後隙などの被ダメージ増加特効）。</summary>
    public bool IsVulnerable;

    /// <summary>被弾時白閃エフェクトの残り時間（秒）。</summary>
    public float HitFlashTimer;

    /// <summary>被弾時白闪エフェクトの規定持続時間（秒）。</summary>
    public float HitFlashDuration;

    /// <summary>白閃中であるかを取得します。</summary>
    public readonly bool IsFlashing => HitFlashTimer > 0.0f;

    /// <summary>
    /// <see cref="BossCombatState"/> 構造体の新しいインスタンスを既定値（白閃時間 0.1 秒）で初期化します。
    /// </summary>
    public BossCombatState()
        : this(false, false, 0.1f, 0.0f)
    {
    }

    /// <summary>
    /// <see cref="BossCombatState"/> 構造体の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="isInvulnerable">無敵フラグ。</param>
    /// <param name="isVulnerable">易傷フラグ。</param>
    /// <param name="hitFlashDuration">被弾白閃持続時間（秒）。</param>
    /// <param name="hitFlashTimer">初期白閃タイマー（秒）。</param>
    public BossCombatState(
        bool isInvulnerable = false,
        bool isVulnerable = false,
        float hitFlashDuration = 0.1f,
        float hitFlashTimer = 0.0f)
    {
        IsInvulnerable = isInvulnerable;
        IsVulnerable = isVulnerable;
        HitFlashDuration = hitFlashDuration;
        HitFlashTimer = hitFlashTimer;
    }
}
