using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AnimalBattle.Bindings;

namespace AnimalBattle.Compute;

/// <summary>
///     <see cref="NativeMemory.AlignedAlloc" /> により 64 バイト Cache Line 境界に整列された非托管連続メモリを C# 側で自律管理し、
///     C++ 原生拡張 (AnimalBattleExtension.dll) の純粋無状態計算核へ一括並列同期ディスパッチを行うバッチ計算キュー。
///     fixed ピン留め不要・GC アロケーションゼロで動作し、<see cref="IDisposable" /> による厳格な RAII 寿命管理を提供します。
/// </summary>
public sealed unsafe class NativeComputeQueue : IDisposable
{
    private static NativeComputeQueue? _shared;
    private static readonly Lock SyncLock = new();

    private static readonly SpecialBulletBatchItem DummyBullet = default;

    private SpecialBulletBatchItem* _bulletBuffer;

    /// <summary>
    ///     指定された容量で 64 バイト物理対齐の非托管メモリを確保し、バッチ計算キューを初期化します。
    /// </summary>
    /// <param name="capacity">収容可能な最大要素数（既定値: 4096）。</param>
    private NativeComputeQueue(int capacity = 4096)
    {
        Capacity = Math.Max(16, capacity);
        BulletCount = 0;
        IsDisposed = false;

        var bulletBytes = (nuint)(sizeof(SpecialBulletBatchItem) * Capacity);

        _bulletBuffer = (SpecialBulletBatchItem*)NativeMemory.AlignedAlloc(bulletBytes, 64);
    }

    /// <summary>全システム間で共有可能な既定のバッチ計算キューインスタンス。</summary>
    public static NativeComputeQueue Shared
    {
        get
        {
            lock (SyncLock)
            {
                if (_shared == null || _shared.IsDisposed)
                {
                    _shared = new NativeComputeQueue();
                }

                return _shared;
            }
        }
    }

    /// <summary>現在キューに蓄積されている特殊弾幕計算要求数。</summary>
    private int BulletCount { get; set; }

    /// <summary>固定バッファの最大収容要素数。</summary>
    private int Capacity { get; }

    /// <summary>リソースが破棄済みであるか否かを取得します。</summary>
    private bool IsDisposed { get; set; }

    /// <summary>特殊弾幕バッファの先頭アドレス（アライメント検証用）。</summary>
    internal void* BulletBufferPtr => _bulletBuffer;

    /// <summary>
    ///     確保された 64 バイト物理対齐の非托管メモリを直ちに解放します。
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     共有バッチ計算キューの物理メモリを直ちに破棄し、ライフサイクルを終了します。
    /// </summary>
    public static void ShutdownShared()
    {
        lock (SyncLock)
        {
            if (_shared is not { IsDisposed: false }) return;
            _shared.Dispose();
            _shared = null;
        }
    }

    /// <summary>
    ///     アンマネージドリソースの解放漏れを防ぐファイナライザ。
    /// </summary>
    ~NativeComputeQueue()
    {
        Dispose(false);
    }

    /// <summary>
    ///     特殊弾幕変調計算要求を非托管バッファへ直接登録し、結果回収用のハンドルインデックスを返します。
    /// </summary>
    /// <param name="type">特殊弾幕種別。</param>
    /// <param name="elapsedTimer">累積タイマー (秒)。</param>
    /// <param name="baseVx">基準 X 速度。</param>
    /// <param name="baseVy">基準 Y 速度。</param>
    /// <param name="baseRadius">基準半径 (px)。</param>
    /// <returns>バッチ結果参照用のインデックスハンドル (容量上限超過时または破棄済み時は -1)。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int EnqueueSpecialBullet(BulletType type, float elapsedTimer, float baseVx, float baseVy, float baseRadius)
    {
        if (IsDisposed || BulletCount >= Capacity) return -1;

        var index = BulletCount++;
        _bulletBuffer[index] = new SpecialBulletBatchItem(type, elapsedTimer, baseVx, baseVy, baseRadius);
        return index;
    }

    /// <summary>
    ///     指定ハンドルに対応する計算済み特殊弾幕変調結果を取得します。
    /// </summary>
    /// <param name="handle">登録時に返却されたインデックスハンドル。</param>
    /// <returns>計算後の変調結果への読み取り専用参照。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly SpecialBulletBatchItem GetBulletResult(int handle)
    {
        if (!IsDisposed && handle >= 0 && handle < BulletCount) return ref _bulletBuffer[handle];

        return ref DummyBullet;
    }

    /// <summary>
    ///     バッファに蓄積された全計算要求を 1 回の P/Invoke で C++ 原生拡張へ引き渡し、一括同期並列実行します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispatch()
    {
        if (IsDisposed) throw new ObjectDisposedException(nameof(NativeComputeQueue));

        if (BulletCount == 0) return;

        NativeExtensionBindings.Extension_ProcessBatchCompute(
            _bulletBuffer,
            BulletCount);
    }

    /// <summary>
    ///     バッファメモリを解放することなく、蓄積要素数を 0 にリセットします。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        BulletCount = 0;
    }

    private void Dispose(bool disposing)
    {
        if (IsDisposed) return;

        if (_bulletBuffer != null)
        {
            NativeMemory.AlignedFree(_bulletBuffer);
            _bulletBuffer = null;
        }

        IsDisposed = true;
    }
}