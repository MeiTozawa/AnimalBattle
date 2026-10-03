using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Components;

/// <summary>
///     Boss の易傷状態および現在の戦闘 Phase を管理するコンポーネント。
///     無敵状態は Health.InvincibleTimer を唯一の真実の源として管理するため本コンポーネントからは削除されています。
/// </summary>
public struct BossCombatState(bool isVulnerable = false, Phase phase = Phase.Phase1)
{
    /// <summary>易傷状態であるか（後隙などの被ダメージ倍加特効）。</summary>
    public bool IsVulnerable = isVulnerable;

    /// <summary>現在の Boss 進行 Phase。</summary>
    public Phase Phase = phase;
}