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
///     高頻度・短命エンティティ（弾幕・パーティクル・衝撃波など）向けの高性能事前確保エンティティプール。
///     実行時の <see cref="World.Entity()" /> によるメモリ再確保および <see cref="Entity.Destruct()" /> による Archetype 変動を廃し、
///     <see cref="Entity.Enable()" /> と <see cref="Entity.Disable()" /> によるゼロ GC・安定した Archetype 運用を提供します。
/// </summary>
public sealed class EntityPool : IDisposable {
    private static ulong _nextPoolId = 1;
    private readonly Action<Entity>? _onInit;
    private readonly Action<Entity>? _onReset;
    private World _world;
    private Entity[] _allAllocated;
    private bool _isDisposed;
    private Entity[] _pool;

    /// <summary>
    ///     エンティティオブジェクトプールを初期化し、指定数のエンティティを事前確保します。
    /// </summary>
    /// <param name="world">エンティティが所属する Flecs.NET ワールド。</param>
    /// <param name="initialCapacity">初期事前確保エンティティ容量。</param>
    /// <param name="onInit">エンティティ生成時の初期化デリゲート（デフォルトコンポーネント付与など）。</param>
    /// <param name="onReset">エンティティ貸出時の状態リセット・クリーンアップデリゲート。</param>
    /// <param name="maxCapacity">プールの最大許容上限容量（OOM 防止用熔断上限）。</param>
    public EntityPool(
        World world,
        int initialCapacity = 64,
        Action<Entity>? onInit = null,
        Action<Entity>? onReset = null,
        int maxCapacity = 2048)
    {
        _world = world;
        _onInit = onInit;
        _onReset = onReset;
        initialCapacity = Math.Max(1, initialCapacity);
        MaxCapacity = Math.Max(initialCapacity, maxCapacity);

        _pool = new Entity[initialCapacity];
        _allAllocated = new Entity[initialCapacity];
        AvailableCount = 0;
        TotalAllocated = 0;

        Preallocate(initialCapacity);
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
    ///     プール内の全エンティティ（空きおよび貸出中）を破棄し、リソースを解放します。
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

    private void Preallocate(int count)
    {
        EnsureArrayCapacity(TotalAllocated + count);
        for (var i = 0; i < count; i++)
        {
            var e = _world.Entity();
            e.Set(new PoolLeaseState { PoolId = PoolId, IsRented = false });
            e.Disable();
            _onInit?.Invoke(e);
            _pool[AvailableCount++] = e;
            _allAllocated[TotalAllocated++] = e;
        }
    }

    private void EnsureArrayCapacity(int required)
    {
        if (_pool.Length < required)
        {
            var newCap = Math.Max(_pool.Length * 2, required);
            Array.Resize(ref _pool, newCap);
            Array.Resize(ref _allAllocated, newCap);
        }
    }

    /// <summary>
    ///     プールからエンティティを貸し出し、クリーンアップ（onReset）と有効化（Enable）を実行します。
    ///     上限に達した場合は OOM 防止のため空エンティティを返却します。
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

            var toAllocate = Math.Min(Math.Max(16, TotalAllocated), MaxCapacity - TotalAllocated);
            Preallocate(toAllocate);
            Metrics.ExpansionCount++;
        }

        var e = _pool[--AvailableCount];
        ref var leaseState = ref e.GetMut<PoolLeaseState>();
        leaseState.IsRented = true;
        leaseState.Version++;

        Metrics.ActiveRentals++;
        if (Metrics.ActiveRentals > Metrics.PeakUsage) Metrics.PeakUsage = Metrics.ActiveRentals;

        _onReset?.Invoke(e);
        e.Enable();
        return e;
    }

    /// <summary>
    ///     使用済みのエンティティをプールへ返却し、無効化（Disable）します。
    ///     プール破棄後に返却されたエンティティは孤児化を防ぐため安全に Destruct されます。
    ///     既に返却済みのエンティティが渡された場合は多重プッシュを防ぐため無視されます。
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
            // 異なる世代または外部プールの孤児エンティティは画面残留を防ぐため安全に破棄
            entity.Destruct();
            return;
        }

        Debug.Assert(state.IsRented, "[EntityPool] 既にプールへ返却済みのエンティティが二重返却（Double Free）されました。");
        if (!state.IsRented)
            // 既にプール内に返却済みの二重返却を安全に無視
            return;

        state.IsRented = false;

        Metrics.ActiveRentals = Math.Max(0, Metrics.ActiveRentals - 1);

        entity.Disable();
        EnsureArrayCapacity(AvailableCount + 1);
        _pool[AvailableCount++] = entity;
    }
}
