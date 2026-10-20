using System.Runtime.CompilerServices;

namespace AnimalBattle.Abilities;

/// <summary>
///     エンティティの戦闘状態フラグを保持する純粋なデータコンポーネント（POD）。
///     ヒープ割り当てを生じさせない 64 ビット型安全ビットマスクとして機能します。
/// </summary>
public struct GameplayTagState(GameplayTag initialTags = GameplayTag.None) {
    /// <summary>現在有効な戦闘タグのビットマスク。</summary>
    public GameplayTag ActiveMask = initialTags;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool HasTag(GameplayTag tag) => (ActiveMask & tag) == tag;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool HasAnyTag(GameplayTag tags) => (ActiveMask & tags) != GameplayTag.None;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool HasAll(GameplayTag tags) => (ActiveMask & tags) == tags;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Matches(GameplayTag required, GameplayTag blocked) =>
        (required == GameplayTag.None || HasAll(required)) && !HasAnyTag(blocked);
}
