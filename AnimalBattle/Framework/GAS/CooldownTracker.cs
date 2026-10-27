using AnimalBattle.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using AnimalBattle.Core;

namespace AnimalBattle.Abilities;

/// <summary>
///     個別能力の冷却スロットデータです。
/// </summary>
public struct CooldownSlot
{
    /// <summary>能力識別タグ。</summary>
    public StringId Tag;

    /// <summary>残り冷却秒数。</summary>
    public float Remaining;
}

/// <summary>
///     16要素のインライン固定長スロットを保持する冷却データコンポーネントです。
///     .NET の InlineArray により、ヒープ割り当てなしで直接インデックスアクセスが可能です。
/// </summary>
[InlineArray(MaxSlots)]
public struct CooldownSlots
{
    public const int MaxSlots = 16;

    private CooldownSlot _element0;
}

/// <summary>
///     能力ごとの冷却時間を一元管理する高速追跡器です。
///     ヒープ割り当てを生じさせない零 GC 構造として動作します。
/// </summary>
public sealed class CooldownTracker {
    private CooldownSlots _slots;
    private int _count;

    /// <summary>直前に実行された能力識別タグを取得します。</summary>
    public StringId LastExecutedTag { get; private set; } = StringId.Empty;

    /// <summary>同一能力の連続実行回数を取得します。</summary>
    public int ConsecutiveUseCount { get; private set; }

    /// <summary>
    ///     経過時間に基づいてすべての能力の冷却時間を減衰更新します。
    ///     インライン配列の直接インデックス走査により、辞書反復時の InvalidOperationException や二次ハッシュ検索を完全に排除します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過秒数。</param>
    public void Update(float deltaTime)
    {
        if (deltaTime <= 0.0f || _count == 0) return;

        for (var i = 0; i < _count; i++)
        {
            ref var slot = ref _slots[i];
            if (slot.Remaining > 0.0f)
            {
                slot.Remaining = MathF.Max(0.0f, slot.Remaining - deltaTime);
            }
        }
    }

    /// <summary>
    ///     指定能力タグの冷却を適用します。
    /// </summary>
    public void Trigger(StringId tag, float cooldown)
    {
        if (tag.IsEmpty) return;

        if (tag == LastExecutedTag)
            ConsecutiveUseCount++;
        else
        {
            LastExecutedTag = tag;
            ConsecutiveUseCount = 1;
        }

        if (cooldown > 0.0f)
        {
            for (var i = 0; i < _count; i++)
            {
                ref var slot = ref _slots[i];
                if (slot.Tag == tag)
                {
                    slot.Remaining = cooldown;
                    return;
                }
            }

            if (_count < CooldownSlots.MaxSlots)
            {
                ref var slot = ref _slots[_count++];
                slot.Tag = tag;
                slot.Remaining = cooldown;
            }
            else
            {
                Log.Error("CooldownTracker", $"クールダウンスロットの最大数 ({CooldownSlots.MaxSlots}) を超過しました: {tag}");
                Debug.Assert(false, $"[CooldownTracker] 冷却スロットの最大数（{CooldownSlots.MaxSlots}）を超過しました: {tag}");
            }
        }
    }

    /// <summary>
    ///     指定能力タグの冷却が完了し使用可能かを判定します。
    /// </summary>
    public bool IsReady(StringId tag)
    {
        if (tag.IsEmpty) return true;

        for (var i = 0; i < _count; i++)
        {
            ref readonly var slot = ref _slots[i];
            if (slot.Tag == tag)
            {
                return slot.Remaining <= 0.0f;
            }
        }

        return true;
    }
}
