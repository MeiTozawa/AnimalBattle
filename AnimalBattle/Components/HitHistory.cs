using System.Runtime.CompilerServices;

namespace AnimalBattle.Components;

/// <summary>
///     貫通・近接攻撃等の多フレーム持続判定において、同一対象への重複多段ヒットを防止するための 16 スロット命中履歴コンポーネント。
///     Unity DOTS の [InternalBufferCapacity(16)] 準拠で 128 バイト固定長インライン展開され、GC 割り当てを一切発生させません。
/// </summary>
public unsafe struct HitHistory
{
    /// <summary>単一攻撃判定が記憶可能な最大命中対象数</summary>
    public const int MaxTargets = 16;

    /// <summary>同一判定内で既に命中した対象エンティティ ID の固定長バッファ</summary>
    public fixed ulong HitEntities[MaxTargets];

    /// <summary>既に命中した対象エンティティの登録数</summary>
    public byte HitCount;

    /// <summary>
    ///     指定した対象エンティティが既に本攻撃判定に命中済みかどうかを検査します。
    /// </summary>
    /// <param name="entityId">検査対象のエンティティ ID</param>
    /// <returns>既に命中済みの場合は true</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool HasHit(ulong entityId)
    {
        var count = HitCount;
        if (count == 0) return false;

        fixed (ulong* ptr = HitEntities)
        {
            for (var i = 0; i < count; i++)
            {
                if (ptr[i] == entityId) return true;
            }
        }
        return false;
    }

    /// <summary>
    ///     命中対象エンティティ ID を履歴バッファへ記録します。
    /// </summary>
    /// <param name="entityId">記録するエンティティ ID</param>
    /// <returns>新規記録に成功した場合は true、既に存在または容量上限到達時は false</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRecordHit(ulong entityId)
    {
        if (HasHit(entityId) || HitCount >= MaxTargets) return false;

        fixed (ulong* ptr = HitEntities)
        {
            ptr[HitCount++] = entityId;
        }
        return true;
    }

    /// <summary>
    ///     命中履歴バッファを初期化します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetHits()
    {
        HitCount = 0;
    }
}
