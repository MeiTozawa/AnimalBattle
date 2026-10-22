using System.Runtime.CompilerServices;

namespace AnimalBattle.Abilities;

/// <summary>
///     時限エフェクトの単一スロットデータ純粋データ。
/// </summary>
public struct TimedEffectSlot
{
    /// <summary>スロットが稼働中かどうか。</summary>
    public bool IsActive;

    /// <summary>付与される戦闘タグ。</summary>
    public GameplayTag GrantedTags;

    /// <summary>残り持続秒数。</summary>
    public float Remaining;

    /// <summary>初期持続秒数。</summary>
    public float TotalDuration;

    /// <summary>付与元エンティティの ID または発生源ハンドル。</summary>
    public ulong SourceHandle;
}

/// <summary>
///     時限エフェクトスロットを保持する純粋なインライン定長データコンポーネント：POD、定長 6 スロット。
///     .NET の InlineArray により、ヒープ割り当てなしで直接インデックスアクセスが可能です。
/// </summary>
[InlineArray(MaxSlots)]
public struct TimedEffectsState
{
    public const int MaxSlots = 6;

    private TimedEffectSlot _element0;
}

/// <summary>
///     TimedEffectsState に対する安全な参照アクセス拡張メソッド。
/// </summary>
public static class TimedEffectsStateExtensions
{
    /// <summary>読み取り専用スロット参照を取得します：デフォルト。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly TimedEffectSlot GetSlot(this in TimedEffectsState effects, int index) =>
        ref effects[index];

    /// <summary>変更可能なスロット参照を取得します。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref TimedEffectSlot GetSlotMut(this ref TimedEffectsState effects, int index) =>
        ref effects[index];
}
