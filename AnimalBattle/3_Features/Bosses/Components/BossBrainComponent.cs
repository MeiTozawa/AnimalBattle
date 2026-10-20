using AnimalBattle.Ai;

namespace AnimalBattle.Components;

/// <summary>
///     Boss の意思決定ロジック（Brain）を保持するコンポーネント。
///     BattleAiSystem が参照・更新を行い、描画や HUD 等の外部システムからの直接アクセスを防ぎます。
/// </summary>
public readonly struct BossBrainComponent(IBossBrain brain)
{
    /// <summary>Boss の Brain インスタンス（状態遷移、GAS スキルタイムライン、経路探索等）。</summary>
    public readonly IBossBrain Brain = brain;
}
