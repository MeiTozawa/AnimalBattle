using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Components;

/// <summary>
///     Boss の戦闘 Phase を管理するコンポーネント。
///     易傷および無敵状態は GameplayTag / AbilitySystemComponent を唯一の真実の源として管理します。
/// </summary>
public struct BossCombatState(Phase phase = Phase.Phase1)
{
    /// <summary>現在の Boss 進行 Phase。</summary>
    public Phase Phase = phase;
}