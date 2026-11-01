namespace AnimalBattle.Framework.Pooling;

/// <summary>
///     弾幕やパーティクルなどの短命オブジェクト向けの高性能エンティティプールです。
///     プレハブ定義と連携してテーブル構造を固定化し、ゼロアロケーションでの安全な貸出と返却を提供します。
/// </summary>
public sealed class EntityPool : IDisposable {
    private static ulong _nextPoolId = 1;
    private Entity _prefab;
    private readonly Action<Entity>? _onReset;
    private World _world;
    private readonly int _maxCapacity;
    private int _availableCount;
    private int _totalAllocated;
    private Entity[] _allAllocated;
    private bool[] _managedRentalFlags;
    private int[] _activePrev;
    private int[] _activeNext;
    private int _activeHead = -1;
    private int _activeTail = -1;
    private bool _isDisposed;
    private Entity[] _pool;

    /// <summary>
    ///     プレハブ定義を指定してエンティティオブジェクトプールを初期化します。
    /// </summary>
    /// <param name="world">エンティティが所属する Flecs.NET ワールド。</param>
    /// <param name="prefab">プールの基準となるプレハブエンティティ</param>
    /// <param name="initialCapacity">初期事前確保エンティティ容量。</param>
    /// <param name="onReset">エンティティ貸出時の状態リセット・クリーンアップデリゲート。</param>
    /// <param name="maxCapacity">メモリ枯渇防止用の最大許容上限容量</param>
    public EntityPool(
        World world,
        Entity prefab,
        int initialCapacity = 64,
        Action<Entity>? onReset = null,
        int maxCapacity = 2048)
    {
        _world = world;
        _prefab = prefab;
        _onReset = onReset;
        initialCapacity = Math.Max(1, initialCapacity);
        _maxCapacity = Math.Max(initialCapacity, maxCapacity);

        _pool = new Entity[initialCapacity];
        _allAllocated = new Entity[initialCapacity];
        _managedRentalFlags = new bool[initialCapacity];
        _activePrev = new int[initialCapacity];
        _activeNext = new int[initialCapacity];
        Array.Fill(_activePrev, -1);
        Array.Fill(_activeNext, -1);
        _activeHead = -1;
        _activeTail = -1;
        _availableCount = 0;
        _totalAllocated = 0;

        Prewarm(initialCapacity);
    }

    /// <summary>プール固有の識別子を取得します。</summary>
    public ulong PoolId { get; } = Interlocked.Increment(ref _nextPoolId);

    /// <summary>
    ///     プール内の全エンティティを破棄し、リソースを解放します。
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            for (var i = 0; i < _totalAllocated; i++)
                if (_allAllocated[i].IsAlive())
                    _allAllocated[i].Destruct();

            _availableCount = 0;
            _totalAllocated = 0;
            _activeHead = -1;
            _activeTail = -1;
            _isDisposed = true;
        }
    }

    /// <summary>
    ///     初期化時にエンティティを事前確保し、非アクティブ状態でプールへ装填します。
    /// </summary>
    private void Prewarm(int count)
    {
        EnsureArrayCapacity(_totalAllocated + count);
        for (var i = 0; i < count; i++)
        {
            var slot = _totalAllocated++;
            var e = CreateAndInitializeEntity(slot, false);
            e.Disable();
            _managedRentalFlags[slot] = false;
            _pool[_availableCount++] = e;
            _allAllocated[slot] = e;
        }
    }

    private Entity CreateAndInitializeEntity(int slot, bool isRented)
    {
        _world.DeferSuspend();
        try
        {
            var e = CreateBaseEntity();
            e.Set(new PoolLeaseState { PoolId = PoolId, IsRented = isRented, SlotIndex = slot });
            return e;
        }
        finally
        {
            _world.DeferResume();
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
    ///     空きがある場合はプールから取り出して有効化し、
    ///     容量に余裕がある枯渇時は即座にアクティブなエンティティを原地生成して返却します。
    ///     最大容量上限に到達した場合はFIFO方式に基づき最も古く貸出されたエンティティを
    ///     強制回収して上書き再利用します。
    /// </summary>
    /// <returns>有効化されたエンティティ。</returns>
    public Entity Rent()
    {
        Debug.Assert(!_isDisposed, "[EntityPool] 破棄済みのプールからエンティティを借出することはできません。");
        if (_isDisposed) return default;

        if (_availableCount == 0)
        {
            if (_totalAllocated >= _maxCapacity)
            {
                // 最大容量上限に到達：OOM 防止とゲーム継続稼働のため、
                // FIFO方式に基づき最も古く貸出されたアクティブエンティティを強制回収して再利用します
                // 安全 FIFO 抢占：跳过已经挂上 RecycleTag 待回收的实体，防止帧末被 CombatRecycleSystem 误杀
                var slot = _activeHead;
                var curr = _activeHead;
                while (curr != -1)
                {
                    var cand = _allAllocated[curr];
                    if (cand.IsAlive() && !cand.Has<RecycleTag>())
                    {
                        slot = curr;
                        break;
                    }
                    curr = _activeNext[curr];
                }

                if (slot < 0 || slot >= _totalAllocated)
                {
                    slot = 0;
                }

                RemoveFromActiveList(slot);
                AddToActiveTail(slot);

                var recycledEntity = _allAllocated[slot];
                if (!recycledEntity.IsAlive())
                {
                    recycledEntity = CreateAndInitializeEntity(slot, true);
                    _allAllocated[slot] = recycledEntity;
                }
                else
                {
                    ref var lease = ref recycledEntity.GetMut<PoolLeaseState>();
                    lease.IsRented = true;
                }

                _managedRentalFlags[slot] = true;
                _onReset?.Invoke(recycledEntity);
                recycledEntity.Enable();
                return recycledEntity;
            }

            // 実行時動的拡張：無効化と再有効化の反転処理を廃止し、
            // デフォルトでアクティブな単一エンティティを原地生成して即座に貸出
            EnsureArrayCapacity(_totalAllocated + 1);
            var newSlot = _totalAllocated++;
            var newEntity = CreateAndInitializeEntity(newSlot, true);

            _allAllocated[newSlot] = newEntity;
            _managedRentalFlags[newSlot] = true;
            AddToActiveTail(newSlot);

            _onReset?.Invoke(newEntity);
            newEntity.Enable();
            return newEntity;
        }

        var e = _pool[--_availableCount];
        ref var leaseState = ref e.GetMut<PoolLeaseState>();
        leaseState.IsRented = true;
        var rentSlot = leaseState.SlotIndex;
        _managedRentalFlags[rentSlot] = true;
        AddToActiveTail(rentSlot);

        _onReset?.Invoke(e);
        e.Enable();
        return e;
    }

    /// <summary>
    ///     使用済みのエンティティをプールへ返却して非アクティブ化します。
    ///     C# 側のマネージド状態ビットにより、System 実行中の Deferred 段階における同一フレーム内の二重返却を即座に遮断します。
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

        // C#マネージド層での即時二重返却判定により遅延段階での不整合を防止します
        var slot = state.SlotIndex;
        if (slot >= 0 && slot < _managedRentalFlags.Length)
        {
            if (!_managedRentalFlags[slot])
            {
                // 同一フレーム内の二重返却を安全に無視します
                return;
            }

            _managedRentalFlags[slot] = false;
            RemoveFromActiveList(slot);
        }

        state.IsRented = false;

        entity.Disable();
        EnsureArrayCapacity(_availableCount + 1);
        _pool[_availableCount++] = entity;
    }
}
