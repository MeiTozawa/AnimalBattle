using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Components;

/// <summary>
///     Boss エンティティの無敵状態、易傷特効状態、および現在の戦闘 Phase を管理する汎用戦闘コンポーネント。
///     具体的な Boss 種類や AI 状態機械から衝突判定ロジックを分離します。
/// </summary>
public struct BossCombatState(bool isInvulnerable = false, bool isVulnerable = false, Phase phase = Phase.Phase1) {
    /// <summary>無敵状態であるか（ダメージ無効化）。</summary>
    public bool IsInvulnerable = isInvulnerable;

    /// <summary>易傷状態であるか（後隙などの被ダメージ増加特効）。</summary>
    public bool IsVulnerable = isVulnerable;

    /// <summary>現在の Boss 進行 Phase。</summary>
    public Phase Phase = phase;
}