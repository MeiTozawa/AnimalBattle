using System.Runtime.InteropServices;

namespace AnimalBattle.Compute;

/// <summary>
/// C++ 原生拡張 (AnimalBattleExtension.dll) とバイナリ互換な統合計算バッチパケット構造体。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct BatchComputePacket
{
    /// <summary>イージング計算バッチ配列へのポインタ。</summary>
    public EaseBatchItem* EaseItems;

    /// <summary>イージング計算要求数。</summary>
    public int EaseCount;

    /// <summary>特殊弾幕計算バッチ配列へのポインタ。</summary>
    public SpecialBulletBatchItem* BulletItems;

    /// <summary>特殊弾幕計算要求数。</summary>
    public int BulletCount;
}
