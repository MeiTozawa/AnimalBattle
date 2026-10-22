using System.Runtime.CompilerServices;

namespace AnimalBattle.Abilities;

/// <summary>
///     エンティティの戦闘状態フラグを一元管理する純粋なデータコンポーネント純粋データ。
///     動作・能力による常駐タグ：ActionMaskと時限効果タグ：TimedMaskを分離管理し、
///     破壊的覆写：Clobberingを防ぎながら型安全な合成ビットマスク：ActiveMaskを提供します。
/// </summary>
public struct GameplayTagState(GameplayTag initialActionTags = GameplayTag.None, GameplayTag initialTimedTags = GameplayTag.None)
{
    /// <summary>動作・能力・状態機から付与された常駐戦闘タグのビットマスク。</summary>
    public GameplayTag ActionMask = initialActionTags;

    /// <summary>時限エフェクト：TimedEffectsState / TimedEffectServiceから付与された時限タグのビットマスク。</summary>
    public GameplayTag TimedMask = initialTimedTags;

    /// <summary>現在有効なすべての戦闘タグの合成ビットマスク：単一の真実。</summary>
    public readonly GameplayTag ActiveMask => ActionMask | TimedMask;

    /// <summary>常駐・動作戦闘タグを安全に付与します。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddActionTag(GameplayTag tag) => ActionMask |= tag;

    /// <summary>常駐・動作戦闘タグを安全に解除します。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveActionTag(GameplayTag tag) => ActionMask &= ~tag;

    /// <summary>指定タグのフラグがすべて立っているか判定します。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool HasTag(GameplayTag tag) => (ActiveMask & tag) == tag;

    /// <summary>指定タグのいずれかが含まれているか判定します。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool HasAnyTag(GameplayTag tags) => (ActiveMask & tags) != GameplayTag.None;

    /// <summary>指定タグがすべて含まれているか判定します。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool HasAll(GameplayTag tags) => (ActiveMask & tags) == tags;

    /// <summary>必須タグと排他タグの制約を満たしているか判定します。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Matches(GameplayTag required, GameplayTag blocked) =>
        (required == GameplayTag.None || HasAll(required)) && !HasAnyTag(blocked);
}
