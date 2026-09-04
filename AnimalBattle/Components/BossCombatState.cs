namespace AnimalBattle.Components;

/// <summary>
/// Boss エンティティの無敵状態および易傷特効状態を管理する汎用戦闘コンポーネント。
/// 具体的な Boss 種類や AI 状態機械から衝突判定ロジックを分離します。
/// </summary>
public struct BossCombatState(bool isInvulnerable = false, bool isVulnerable = false)
{
    /// <summary>無敵状態であるか（ダメージ無効化）。</summary>
    public bool IsInvulnerable = isInvulnerable;

    /// <summary>易傷状態であるか（後隙などの被ダメージ増加特効）。</summary>
    public bool IsVulnerable = isVulnerable;
}
