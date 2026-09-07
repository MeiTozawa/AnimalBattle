using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AnimalBattle.Bindings;
using AnimalBattle.Components;
using AnimalBattle.Core;

namespace AnimalBattle.Compute;

/// <summary>
/// <see cref="NativeMemory.AlignedAlloc"/> により 64 バイト Cache Line 境界に整列された非托管連続メモリを C# 側で自律管理し、
/// C++ 原生拡張 (AnimalBattleExtension.dll) の純粋無状態計算核へ一括並列同期ディスパッチを行うバッチ計算キュー。
/// fixed ピン留め不要・GC アロケーションゼロで動作し、<see cref="IDisposable"/> による厳格な RAII 寿命管理を提供します。
/// </summary>
public sealed unsafe class NativeComputeQueue : IDisposable
{
    /// <summary>全システム間で共有可能な既定のバッチ計算キューインスタンス。</summary>
    public static readonly NativeComputeQueue Shared = new();

    private static readonly SpecialBulletBatchItem _dummyBullet = default;

    private EaseBatchItem* _easeBuffer;
    private SpecialBulletBatchItem* _bulletBuffer;
    private readonly int _capacity;
    private int _easeCount;
    private int _bulletCount;
    private bool _isDisposed;

    /// <summary>現在キューに蓄積されているイージング計算要求数。</summary>
    public int EaseCount => _easeCount;

    /// <summary>現在キューに蓄積されている特殊弾幕計算要求数。</summary>
    public int BulletCount => _bulletCount;

    /// <summary>固定バッファの最大収容要素数。</summary>
    public int Capacity => _capacity;

    /// <summary>リソースが破棄済みであるか否かを取得します。</summary>
    public bool IsDisposed => _isDisposed;

    /// <summary>イージングバッファの先頭アドレス（アライメント検証用）。</summary>
    internal void* EaseBufferPtr => _easeBuffer;

    /// <summary>特殊弾幕バッファの先頭アドレス（アライメント検証用）。</summary>
    internal void* BulletBufferPtr => _bulletBuffer;

    /// <summary>
    /// 指定された容量で 64 バイト物理対齐の非托管メモリを確保し、バッチ計算キューを初期化します。
    /// </summary>
    /// <param name="capacity">収容可能な最大要素数（既定値: 4096）。</param>
    public NativeComputeQueue(int capacity = 4096)
    {
        _capacity = Math.Max(16, capacity);
        _easeCount = 0;
        _bulletCount = 0;
        _isDisposed = false;

        nuint easeBytes = (nuint)(sizeof(EaseBatchItem) * _capacity);
        nuint bulletBytes = (nuint)(sizeof(SpecialBulletBatchItem) * _capacity);

        _easeBuffer = (EaseBatchItem*)NativeMemory.AlignedAlloc(easeBytes, 64);
        _bulletBuffer = (SpecialBulletBatchItem*)NativeMemory.AlignedAlloc(bulletBytes, 64);
    }

    /// <summary>
    /// アンマネージドリソースの解放漏れを防ぐファイナライザ。
    /// </summary>
    ~NativeComputeQueue()
    {
        Dispose(false);
    }

    /// <summary>
    /// イージング計算要求を非托管バッファへ直接登録し、結果回収用のハンドルインデックスを返します。
    /// </summary>
    /// <param name="easeType">イージング関数種別。</param>
    /// <param name="progress">進捗割合 (0.0f～1.0f)。</param>
    /// <returns>バッチ結果参照用のインデックスハンドル (容量上限超過時または破棄済み時は -1)。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int EnqueueEase(EaseType easeType, float progress)
    {
        if (_isDisposed || _easeCount >= _capacity)
        {
            return -1;
        }

        int index = _easeCount++;
        _easeBuffer[index] = new(easeType, progress);
        return index;
    }

    /// <summary>
    /// 特殊弾幕変調計算要求を非托管バッファへ直接登録し、結果回収用のハンドルインデックスを返します。
    /// </summary>
    /// <param name="type">特殊弾幕種別。</param>
    /// <param name="elapsedTimer">累積タイマー (秒)。</param>
    /// <param name="baseVx">基準 X 速度。</param>
    /// <param name="baseVy">基準 Y 速度。</param>
    /// <param name="baseRadius">基準半径 (px)。</param>
    /// <returns>バッチ結果参照用のインデックスハンドル (容量上限超過時または破棄済み時は -1)。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int EnqueueSpecialBullet(BulletType type, float elapsedTimer, float baseVx, float baseVy, float baseRadius)
    {
        if (_isDisposed || _bulletCount >= _capacity)
        {
            return -1;
        }

        int index = _bulletCount++;
        _bulletBuffer[index] = new(type, elapsedTimer, baseVx, baseVy, baseRadius);
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
        return (!_isDisposed && handle >= 0 && handle < _easeCount) ? _easeBuffer[handle].Result : 0.0f;
    }

    /// <summary>
    /// 指定ハンドルに対応する計算済み特殊弾幕変調結果を取得します。
    /// </summary>
    /// <param name="handle">登録時に返却されたインデックスハンドル。</param>
    /// <returns>計算後の変調結果への読み取り専用参照。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly SpecialBulletBatchItem GetBulletResult(int handle)
    {
        if (!_isDisposed && handle >= 0 && handle < _bulletCount)
        {
            return ref _bulletBuffer[handle];
        }

        return ref _dummyBullet;
    }

    /// <summary>
    /// バッファに蓄積された全計算要求を 1 回の P/Invoke で C++ 原生拡張へ引き渡し、一括同期並列実行します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispatch()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(NativeComputeQueue));
        }

        if (_easeCount == 0 && _bulletCount == 0)
        {
            return;
        }

        NativeExtensionBindings.Extension_ProcessBatchCompute(
            _easeBuffer,
            _easeCount,
            _bulletBuffer,
            _bulletCount);
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

    /// <summary>
    /// 確保された 64 バイト物理対齐の非托管メモリを直ちに解放します。
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        if (_easeBuffer != null)
        {
            NativeMemory.AlignedFree(_easeBuffer);
            _easeBuffer = null;
        }

        if (_bulletBuffer != null)
        {
            NativeMemory.AlignedFree(_bulletBuffer);
            _bulletBuffer = null;
        }

        _isDisposed = true;
    }
}
