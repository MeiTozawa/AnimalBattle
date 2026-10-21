using System.Diagnostics;
using AnimalBattle.Components;
using Flecs.NET.Core;

namespace AnimalBattle.Framework.Pooling;

/// <summary>
///     エンティティオブジェクトプールの運用テレメトリおよび健全性メトリクス。
/// </summary>
public sealed class EntityPoolMetrics {
    /// <summary>過去最大の同時貸出使用量（ピーク使用量）。</summary>
    public int PeakUsage { get; internal set; }

    /// <summary>プールの動的拡張が発生した回数。</summary>
    public int ExpansionCount { get; internal set; }

    /// <summary>最大容量上限に達して貸出要求が拒否された回数。</summary>
    public int OverflowCount { get; internal set; }

    /// <summary>現在アクティブに貸出中のエンティティ数。</summary>
    public int ActiveRentals { get; internal set; }
}

/// <summary>
///     高頻度・短命エンティティ（弾幕・パーティクル・衝撃波・斬撃判定など）向けの高性能エンティティプール。
///     Flecs.NET の Prefab 原型と連携し、Archetype の固定化とゼロ GC・遅延実行安全な貸出/返却を提供します。
/// </summary>
public sealed class EntityPool : IDisposable {
    private static ulong _nextPoolId = 1;
    private Entity _prefab;
    private readonly Action<Entity>? _onInit;
    private readonly Action<Entity>? _onReset;
    private World _world;
    private Entity[] _allAllocated;
    private bool[] _managedRentalFlags;
    private int[] _activePrev;
    private int[] _activeNext;
    private int _activeHead = -1;
    private int _activeTail = -1;
    private bool _isDisposed;
    private Entity[] _pool;

    /// <summary>
    ///     Prefab 原型を指定してエンティティオブジェクトプールを初期化します。
    /// </summary>
    /// <param name="world">エンティティが所属する Flecs.NET ワールド。</param>
    /// <param name="prefab">プールの原型となる Prefab エンティティ。</param>
    /// <param name="initialCapacity">初期事前確保エンティティ容量。</param>
    /// <param name="onReset">エンティティ貸出時の状態リセット・クリーンアップデリゲート。</param>
    /// <param name="maxCapacity">プールの最大許容上限容量（OOM 防止用熔断上限）。</param>
    public EntityPool(
        World world,
        Entity prefab,
        int initialCapacity = 64,
        Action<Entity>? onReset = null,
        int maxCapacity = 2048)
        : this(world, initialCapacity, null, onReset, maxCapacity, prefab)
    {
    }

    private EntityPool(
        World world,
        int initialCapacity,
        Action<Entity>? onInit,
        Action<Entity>? onReset,
        int maxCapacity,
        Entity prefab)
    {
        _world = world;
        _prefab = prefab;
        _onInit = onInit;
        _onReset = onReset;
        initialCapacity = Math.Max(1, initialCapacity);
        MaxCapacity = Math.Max(initialCapacity, maxCapacity);

        _pool = new Entity[initialCapacity];
        _allAllocated = new Entity[initialCapacity];
        _managedRentalFlags = new bool[initialCapacity];
        _activePrev = new int[initialCapacity];
        _activeNext = new int[initialCapacity];
        Array.Fill(_activePrev, -1);
        Array.Fill(_activeNext, -1);
        _activeHead = -1;
        _activeTail = -1;
        AvailableCount = 0;
        TotalAllocated = 0;

        Prewarm(initialCapacity);
    }

    /// <summary>プール固有の識別子を取得します。</summary>
    public ulong PoolId { get; } = Interlocked.Increment(ref _nextPoolId);

    /// <summary>プールの運用メトリクス統計を取得します。</summary>
    public EntityPoolMetrics Metrics { get; } = new();

    /// <summary>プールの最大許容エンティティ容量を取得します。</summary>
    public int MaxCapacity { get; }

    /// <summary>プール内の現在利用可能なエンティティ総数を取得します。</summary>
    public int AvailableCount { get; private set; }

    /// <summary>現在確保済みのエンティティ総数（貸出中＋空き）を取得します。</summary>
    public int TotalAllocated { get; private set; }

    /// <summary>
    ///     プール内の全エンティティを破棄し、リソースを解放します。
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            for (var i = 0; i < TotalAllocated; i++)
                if (_allAllocated[i].IsAlive())
                    _allAllocated[i].Destruct();

            AvailableCount = 0;
            TotalAllocated = 0;
            _activeHead = -1;
            _activeTail = -1;
            _isDisposed = true;
        }
    }

    /// <summary>
    ///     初期化時（非 Deferred 段階）にエンティティを事前確保し、非アクティブ状態でプールへ装填します。
    /// </summary>
    private void Prewarm(int count)
    {
        EnsureArrayCapacity(TotalAllocated + count);
        for (var i = 0; i < count; i++)
        {
            var e = CreateBaseEntity();
            var slot = TotalAllocated++;
            e.Set(new PoolLeaseState { PoolId = PoolId, IsRented = false, Version = 0, SlotIndex = slot });
            e.Disable();
            _onInit?.Invoke(e);
            _managedRentalFlags[slot] = false;
            _pool[AvailableCount++] = e;
            _allAllocated[slot] = e;
        }
    }

    private Entity CreateBaseEntity()
    {
        return _prefab.IsAlive() ? _world.Entity().IsA(_prefab) : _world.Entity();
    }

    private void EnsureArrayCapacity(int required)
    {
        if (_pool.Length < required)
        {
            var oldLen = _pool.Length;
            var newCap = Math.Max(_pool.Length * 2, required);
            Array.Resize(ref _pool, newCap);
            Array.Resize(ref _allAllocated, newCap);
            Array.Resize(ref _managedRentalFlags, newCap);
            Array.Resize(ref _activePrev, newCap);
            Array.Resize(ref _activeNext, newCap);
            Array.Fill(_activePrev, -1, oldLen, newCap - oldLen);
            Array.Fill(_activeNext, -1, oldLen, newCap - oldLen);
        }
    }

    private void AddToActiveTail(int slot)
    {
        _activePrev[slot] = _activeTail;
        _activeNext[slot] = -1;
        if (_activeTail != -1)
        {
            _activeNext[_activeTail] = slot;
        }
        else
        {
            _activeHead = slot;
        }
        _activeTail = slot;
    }

    private void RemoveFromActiveList(int slot)
    {
        if (slot < 0 || slot >= _activePrev.Length) return;

        var prev = _activePrev[slot];
        var next = _activeNext[slot];

        if (prev == -1 && next == -1 && _activeHead != slot && _activeTail != slot)
        {
            return;
        }

        if (prev != -1)
            _activeNext[prev] = next;
        else if (_activeHead == slot)
            _activeHead = next;

        if (next != -1)
            _activePrev[next] = prev;
        else if (_activeTail == slot)
            _activeTail = prev;

        _activePrev[slot] = -1;
        _activeNext[slot] = -1;
    }

    /// <summary>
    ///     プールからエンティティを貸し出します。
    ///     空きがある場合はプールから取り出して有効化（Enable）し、
    ///     容量に余裕がある枯渇時は即座にアクティブなエンティティを原地生成して返却します。
    ///     最大容量上限に到達した場合は、Ring Buffer 策略（FIFO）に基づき最も古く借出されたアクティブエンティティを
    ///     強制回収・上書き再利用して返却します（default による全局汚染・クラッシュを根絶）。
    /// </summary>
    /// <returns>有効化されたエンティティ。</returns>
    public Entity Rent()
    {
        Debug.Assert(!_isDisposed, "[EntityPool] 破棄済みのプールからエンティティを借出することはできません。");
        if (_isDisposed) return default;

        if (AvailableCount == 0)
        {
            if (TotalAllocated >= MaxCapacity)
            {
                // 最大容量上限に到達：OOM 防止とゲーム継続稼働のため、
                // Ring Buffer 策略（FIFO）に基づき最も古く借出されたアクティブエンティティを強制上書き回収・再利用
                Metrics.OverflowCount++;

                var slot = _activeHead;
                if (slot < 0 || slot >= TotalAllocated)
                {
                    slot = 0;
                }

                RemoveFromActiveList(slot);
                AddToActiveTail(slot);

                var recycledEntity = _allAllocated[slot];
                if (!recycledEntity.IsAlive())
                {
                    recycledEntity = CreateBaseEntity();
                    recycledEntity.Set(new PoolLeaseState { PoolId = PoolId, IsRented = true, Version = 1, SlotIndex = slot });
                    _onInit?.Invoke(recycledEntity);
                    _allAllocated[slot] = recycledEntity;
                }
                else
                {
                    ref var lease = ref recycledEntity.GetMut<PoolLeaseState>();
                    lease.IsRented = true;
                    lease.Version++;
                }

                _managedRentalFlags[slot] = true;
                _onReset?.Invoke(recycledEntity);
                recycledEntity.Enable();
                return recycledEntity;
            }

            // 実行時動的拡張：Disable() -> プール格納 -> Enable() の反転を廃止し、
            // デフォルトでアクティブな単一エンティティを原地生成して即座に貸出
            EnsureArrayCapacity(TotalAllocated + 1);
            var newSlot = TotalAllocated++;
            var newEntity = CreateBaseEntity();
            newEntity.Set(new PoolLeaseState { PoolId = PoolId, IsRented = true, Version = 1, SlotIndex = newSlot });
            _onInit?.Invoke(newEntity);

            _allAllocated[newSlot] = newEntity;
            _managedRentalFlags[newSlot] = true;
            AddToActiveTail(newSlot);

            Metrics.ActiveRentals++;
            if (Metrics.ActiveRentals > Metrics.PeakUsage) Metrics.PeakUsage = Metrics.ActiveRentals;
            Metrics.ExpansionCount++;

            _onReset?.Invoke(newEntity);
            return newEntity;
        }

        var e = _pool[--AvailableCount];
        ref var leaseState = ref e.GetMut<PoolLeaseState>();
        leaseState.IsRented = true;
        leaseState.Version++;
        var rentSlot = leaseState.SlotIndex;
        _managedRentalFlags[rentSlot] = true;
        AddToActiveTail(rentSlot);

        Metrics.ActiveRentals++;
        if (Metrics.ActiveRentals > Metrics.PeakUsage) Metrics.PeakUsage = Metrics.ActiveRentals;

        _onReset?.Invoke(e);
        e.Enable();
        return e;
    }

    /// <summary>
    ///     使用済みのエンティティをプールへ返却し、無効化（Disable）します。
    ///     C# 側の托管状態ビットにより、System 実行中の Deferred 段階における同一フレーム内の二重返却を即座に遮断します。
    /// </summary>
    /// <param name="entity">返却するエンティティ。</param>
    public void Return(Entity entity)
    {
        if (!entity.IsAlive()) return;

        if (_isDisposed)
        {
            entity.Destruct();
            return;
        }

        Debug.Assert(entity.Has<PoolLeaseState>(), "[EntityPool] PoolLeaseState は必須です。");
        ref var state = ref entity.GetMut<PoolLeaseState>();
        Debug.Assert(state.PoolId == PoolId, $"[EntityPool] 返却対象エンティティの所属プールが一致しません。(期待PoolId: {PoolId}, 実際PoolId: {state.PoolId})");
        if (state.PoolId != PoolId)
        {
            entity.Destruct();
            return;
        }

        // C# 托管層での即時二重返却判定（Deferred 段階での読み抜けを物理防止）
        var slot = state.SlotIndex;
        if (slot >= 0 && slot < _managedRentalFlags.Length)
        {
            if (!_managedRentalFlags[slot])
            {
                // 同一フレーム内の二重返却（Double Free）を安全に無視
                return;
            }

            _managedRentalFlags[slot] = false;
            RemoveFromActiveList(slot);
        }

        state.IsRented = false;
        Metrics.ActiveRentals = Math.Max(0, Metrics.ActiveRentals - 1);

        entity.Disable();
        EnsureArrayCapacity(AvailableCount + 1);
        _pool[AvailableCount++] = entity;
    }
}
