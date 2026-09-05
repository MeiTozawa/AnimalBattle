using System;
using System.Runtime.CompilerServices;
using AnimalBattle.Bindings;
using AnimalBattle.Components;
using AnimalBattle.Core;

namespace AnimalBattle.Compute;

/// <summary>
/// イージング評価および特殊弾幕変調計算をフレーム内で蓄積し、
/// 1 回の P/Invoke で C++ 原生拡張 (AnimalBattleExtension.dll) へ一括転送・並列処理する同期バッチ計算キュー。
/// 事前確保バッファの再利用により GC アロケーションゼロで動作します。
/// </summary>
public sealed class NativeComputeQueue
{
    private const int DefaultInitialCapacity = 256;

    /// <summary>全システム間で共有可能な既定のバッチ計算キューインスタンス。</summary>
    public static readonly NativeComputeQueue Shared = new(DefaultInitialCapacity);

    private EaseBatchItem[] _easeBuffer;
    private int _easeCount;
    private SpecialBulletBatchItem[] _bulletBuffer;
    private int _bulletCount;

    /// <summary>現在キューに蓄積されているイージング計算要求数。</summary>
    public int EaseCount => _easeCount;

    /// <summary>現在キューに蓄積されている特殊弾幕計算要求数。</summary>
    public int BulletCount => _bulletCount;

    /// <summary>
    /// 指定初期容量でバッチ計算キューを初期化します。
    /// </summary>
    /// <param name="initialCapacity">内部配列の初期容量。</param>
    public NativeComputeQueue(int initialCapacity = DefaultInitialCapacity)
    {
        int capacity = Math.Max(16, initialCapacity);
        _easeBuffer = new EaseBatchItem[capacity];
        _bulletBuffer = new SpecialBulletBatchItem[capacity];
        _easeCount = 0;
        _bulletCount = 0;
    }

    /// <summary>
    /// イージング計算要求をキューへ登録し、結果回収用のハンドルインデックスを返します。
    /// </summary>
    /// <param name="easeType">イージング関数種別。</param>
    /// <param name="progress">進捗割合 (0.0f～1.0f)。</param>
    /// <returns>バッチ結果参照用のインデックスハンドル。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int EnqueueEase(EaseType easeType, float progress)
    {
        if (_easeCount >= _easeBuffer.Length)
        {
            Array.Resize(ref _easeBuffer, _easeBuffer.Length * 2);
        }

        int index = _easeCount++;
        _easeBuffer[index] = new EaseBatchItem(easeType, progress);
        return index;
    }

    /// <summary>
    /// 特殊弾幕変調計算要求をキューへ登録し、結果回収用のハンドルインデックスを返します。
    /// </summary>
    /// <param name="type">特殊弾幕種別。</param>
    /// <param name="elapsedTimer">累積タイマー (秒)。</param>
    /// <param name="baseVx">基準 X 速度。</param>
    /// <param name="baseVy">基準 Y 速度。</param>
    /// <param name="baseRadius">基準半径 (px)。</param>
    /// <returns>バッチ結果参照用のインデックスハンドル。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int EnqueueSpecialBullet(BulletType type, float elapsedTimer, float baseVx, float baseVy, float baseRadius)
    {
        if (_bulletCount >= _bulletBuffer.Length)
        {
            Array.Resize(ref _bulletBuffer, _bulletBuffer.Length * 2);
        }

        int index = _bulletCount++;
        _bulletBuffer[index] = new SpecialBulletBatchItem(type, elapsedTimer, baseVx, baseVy, baseRadius);
        return index;
    }

    /// <summary>
    /// 指定ハンドルに対応する計算済みイージング補間値を取得します。
    /// </summary>
    /// <param name="handle">登録時に返却されたインデックスハンドル。</param>
    /// <returns>計算後の補間値。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetEaseResult(int handle)
    {
        return _easeBuffer[handle].Result;
    }

    /// <summary>
    /// 指定ハンドルに対応する計算済み特殊弾幕変調結果を取得します。
    /// </summary>
    /// <param name="handle">登録時に返却されたインデックスハンドル。</param>
    /// <returns>計算後の変調結果への読み取り専用参照。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly SpecialBulletBatchItem GetBulletResult(int handle)
    {
        return ref _bulletBuffer[handle];
    }

    /// <summary>
    /// キューに蓄積された全計算要求を 1 回の P/Invoke で C++ 原生拡張へ転送し、
    /// AVX2 SIMD / 並列計算を一括同期実行して結果を内部バッファへ書き戻します。
    /// </summary>
    public unsafe void Dispatch()
    {
        if (_easeCount == 0 && _bulletCount == 0)
        {
            return;
        }

        fixed (EaseBatchItem* pEase = _easeBuffer)
        fixed (SpecialBulletBatchItem* pBullets = _bulletBuffer)
        {
            BatchComputePacket packet = new()
            {
                EaseItems = pEase,
                EaseCount = _easeCount,
                BulletItems = pBullets,
                BulletCount = _bulletCount
            };

            NativeExtensionBindings.Extension_ProcessBatchComputePacket(&packet);
        }
    }

    /// <summary>
    /// バッファメモリを解放することなく、蓄積要素数を 0 にリセットします。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        _easeCount = 0;
        _bulletCount = 0;
    }
}
