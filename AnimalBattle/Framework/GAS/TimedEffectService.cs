using System.Runtime.CompilerServices;

namespace AnimalBattle.Abilities;

/// <summary>
///     時限エフェクトの適用およびスロット割り当てを行うステートレスなドメインサービス。
/// </summary>
public static class TimedEffectService
{
    /// <summary>
    ///     時限エフェクトをスロットへ登録し、タグ状態を更新します。
    /// </summary>
    public static void ApplyEffect(ref TimedEffectsState effects,
        ref GameplayTagState tagState,
        GameplayTag grantedTags,
        float duration,
        ulong sourceHandle = 0)
    {
        if (duration <= 0.0f || grantedTags == GameplayTag.None) return;

        // 1. 同一発生源かつ同一タグの既存エフェクトの持続時間を更新
        for (var i = 0; i < TimedEffectsState.MaxSlots; i++)
        {
            ref var slot = ref effects.GetSlotMut(i);
            if (slot.IsActive && slot.SourceHandle == sourceHandle && slot.GrantedTags == grantedTags)
            {
                slot.Remaining = MathF.Max(slot.Remaining, duration);
                slot.TotalDuration = MathF.Max(slot.TotalDuration, duration);
                return;
            }
        }

        // 2. 空きスロットへ新規割り当て
        for (var i = 0; i < TimedEffectsState.MaxSlots; i++)
        {
            ref var slot = ref effects.GetSlotMut(i);
            if (!slot.IsActive)
            {
                slot.IsActive = true;
                slot.GrantedTags = grantedTags;
                slot.Remaining = duration;
                slot.TotalDuration = duration;
                slot.SourceHandle = sourceHandle;
                tagState.TimedMask |= grantedTags;
                return;
            }
        }

        // 3. スロット満杯時：残り時間が最短のスロットを上書き
        var shortestIdx = 0;
        var shortestTime = float.MaxValue;
        for (var i = 0; i < TimedEffectsState.MaxSlots; i++)
        {
            ref readonly var slot = ref effects.GetSlot(i);
            if (slot.Remaining < shortestTime)
            {
                shortestTime = slot.Remaining;
                shortestIdx = i;
            }
        }

        ref var victim = ref effects.GetSlotMut(shortestIdx);
        victim.IsActive = true;
        victim.GrantedTags = grantedTags;
        victim.Remaining = duration;
        victim.TotalDuration = duration;
        victim.SourceHandle = sourceHandle;
        RecalculateTagMask(ref effects, ref tagState);
    }

    /// <summary>
    ///     指定されたタグの最大残り時間を秒単位で取得します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetTagRemainingTime(in TimedEffectsState effects, GameplayTag tag)
    {
        var maxRemaining = 0.0f;
        for (var i = 0; i < TimedEffectsState.MaxSlots; i++)
        {
            ref readonly var slot = ref effects.GetSlot(i);
            if (slot.IsActive && (slot.GrantedTags & tag) != GameplayTag.None)
            {
                maxRemaining = MathF.Max(maxRemaining, slot.Remaining);
            }
        }
        return maxRemaining;
    }

    /// <summary>
    ///     ActionMaskを保持したまま全アクティブスロットから時限タグマスクを再計算します。
    /// </summary>
    public static void RecalculateTagMask(ref TimedEffectsState effects, ref GameplayTagState tagState)
    {
        var mask = GameplayTag.None;
        for (var i = 0; i < TimedEffectsState.MaxSlots; i++)
        {
            ref readonly var slot = ref effects.GetSlot(i);
            if (slot.IsActive)
            {
                mask |= slot.GrantedTags;
            }
        }
        tagState.TimedMask = mask;
    }
}
