using System.Diagnostics;
using AnimalBattle.Components;
using Flecs.NET.Core;

namespace AnimalBattle.CombatPool;

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
    private readonly Entity _prefab;
    private readonly Action<Entity>? _onInit;
    private readonly Action<Entity>? _onReset;
    private World _world;
    private Entity[] _allAllocated;
    private bool[] _managedRentalFlags;
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

    /// <summary>
    ///     カスタム初期化デリゲートを指定してエンティティオブジェクトプールを初期化します（後方互換対応）。
    /// </summary>
    /// <param name="world">エンティティが所属する Flecs.NET ワールド。</param>
    /// <param name="initialCapacity">初期事前確保エンティティ容量。</param>
    /// <param name="onInit">エンティティ生成時の初期化デリゲート。</param>
    /// <param name="onReset">エンティティ貸出時の状態リセット・クリーンアップデリゲート。</param>
    /// <param name="maxCapacity">プールの最大許容上限容量。</param>
    public EntityPool(
        World world,
        int initialCapacity = 64,
        Action<Entity>? onInit = null,
        Action<Entity>? onReset = null,
        int maxCapacity = 2048)
        : this(world, initialCapacity, onInit, onReset, maxCapacity, default)
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
            var newCap = Math.Max(_pool.Length * 2, required);
            Array.Resize(ref _pool, newCap);
            Array.Resize(ref _allAllocated, newCap);
            Array.Resize(ref _managedRentalFlags, newCap);
        }
    }

    /// <summary>
    ///     プールからエンティティを貸し出します。
    ///     空きがある場合はプールから取り出して有効化（Enable）し、
    ///     実行時枯渇時は単一の即時アクティブエンティティを原地生成して返却します（Disable/Enable の反転を回避）。
    /// </summary>
    /// <returns>有効化されたエンティティ（上限到達時は無効な空エンティティ）。</returns>
    public Entity Rent()
    {
        Debug.Assert(!_isDisposed, "[EntityPool] 破棄済みのプールからエンティティを借出することはできません。");

        if (AvailableCount == 0)
        {
            if (TotalAllocated >= MaxCapacity)
            {
                // 最大容量上限に到達：OOM 防止のため熔断（サーキットブレーカー発動）
                Metrics.OverflowCount++;
                return default;
            }

            // 実行時動的拡張：Disable() -> プール格納 -> Enable() の反転を廃止し、
            // デフォルトでアクティブな単一エンティティを原地生成して即座に貸出
            EnsureArrayCapacity(TotalAllocated + 1);
            var slot = TotalAllocated++;
            var newEntity = CreateBaseEntity();
            newEntity.Set(new PoolLeaseState { PoolId = PoolId, IsRented = true, Version = 1, SlotIndex = slot });
            _onInit?.Invoke(newEntity);

            _allAllocated[slot] = newEntity;
            _managedRentalFlags[slot] = true;

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
        _managedRentalFlags[leaseState.SlotIndex] = true;

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
        }

        state.IsRented = false;
        Metrics.ActiveRentals = Math.Max(0, Metrics.ActiveRentals - 1);

        entity.Disable();
        EnsureArrayCapacity(AvailableCount + 1);
        _pool[AvailableCount++] = entity;
    }
}
