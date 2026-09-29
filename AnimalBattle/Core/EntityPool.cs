using System;
using Flecs.NET.Core;

namespace AnimalBattle.Core;

/// <summary>
/// プール内エンティティの貸出状態および所属プールを追跡する内部コンポーネント。
/// </summary>
public struct PoolLeaseState
{
    /// <summary>所属するプールの固有識別子。</summary>
    public ulong PoolId;

    /// <summary>現在貸出中であるかフラグ。</summary>
    public bool IsRented;
}

/// <summary>
/// 高頻度・短命エンティティ（弾幕・パーティクル・衝撃波など）向けの高性能事前確保エンティティプール。
/// 実行時の <see cref="World.Entity()"/> によるメモリ再確保および <see cref="Entity.Destruct()"/> による Archetype 変動を廃し、
/// <see cref="Entity.Enable()"/> と <see cref="Entity.Disable()"/> によるゼロ GC・安定した Archetype 運用を提供します。
/// </summary>
public sealed class EntityPool : IDisposable
{
    private static ulong _nextPoolId = 1;
    private readonly ulong _poolId = System.Threading.Interlocked.Increment(ref _nextPoolId);
    private readonly World _world;
    private readonly Action<Entity>? _onInit;
    private readonly Action<Entity>? _onReset;
    private Entity[] _pool;
    private Entity[] _allAllocated;
    private int _top;
    private int _totalAllocated;
    private bool _isDisposed;

    /// <summary>プール固有の識別子を取得します。</summary>
    public ulong PoolId => _poolId;

    /// <summary>プール内の現在利用可能なエンティティ総数を取得します。</summary>
    public int AvailableCount => _top;

    /// <summary>現在確保済みのエンティティ総数（貸出中＋空き）を取得します。</summary>
    public int TotalAllocated => _totalAllocated;

    /// <summary>
    /// エンティティオブジェクトプールを初期化し、指定数のエンティティを事前確保します。
    /// </summary>
    /// <param name="world">エンティティが所属する Flecs.NET ワールド。</param>
    /// <param name="initialCapacity">初期事前確保エンティティ容量。</param>
    /// <param name="onInit">エンティティ生成時の初期化デリゲート（デフォルトコンポーネント付与など）。</param>
    /// <param name="onReset">エンティティ貸出時の状態リセット・クリーンアップデリゲート。</param>
    public EntityPool(World world, int initialCapacity = 64, Action<Entity>? onInit = null, Action<Entity>? onReset = null)
    {
        _world = world;
        _onInit = onInit;
        _onReset = onReset;
        initialCapacity = Math.Max(1, initialCapacity);

        _pool = new Entity[initialCapacity];
        _allAllocated = new Entity[initialCapacity];
        _top = 0;
        _totalAllocated = 0;

        Preallocate(initialCapacity);
    }

    private void Preallocate(int count)
    {
        EnsureArrayCapacity(_totalAllocated + count);
        for (int i = 0; i < count; i++)
        {
            Entity e = _world.Entity();
            e.Set(new PoolLeaseState { PoolId = _poolId, IsRented = false });
            e.Disable();
            _onInit?.Invoke(e);
            _pool[_top++] = e;
            _allAllocated[_totalAllocated++] = e;
        }
    }

    private void EnsureArrayCapacity(int required)
    {
        if (_pool.Length < required)
        {
            int newCap = Math.Max(_pool.Length * 2, required);
            Array.Resize(ref _pool, newCap);
            Array.Resize(ref _allAllocated, newCap);
        }
    }

    /// <summary>
    /// プールからエンティティを貸し出し、クリーンアップ（onReset）と有効化（Enable）を実行します。
    /// </summary>
    /// <returns>有効化されたエンティティ。</returns>
    public Entity Rent()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_top == 0)
        {
            Preallocate(Math.Max(16, _totalAllocated));
        }

        Entity e = _pool[--_top];
        if (e.Has<PoolLeaseState>())
        {
            e.GetMut<PoolLeaseState>().IsRented = true;
        }
        _onReset?.Invoke(e);
        e.Enable();
        return e;
    }

    /// <summary>
    /// 使用済みのエンティティをプールへ返却し、無効化（Disable）します。
    /// プール破棄後に返却されたエンティティは孤児化を防ぐため安全に Destruct されます。
    /// 既に返却済みのエンティティが渡された場合は多重プッシュを防ぐため無視されます。
    /// </summary>
    /// <param name="entity">返却するエンティティ。</param>
    public void Return(Entity entity)
    {
        if (!entity.IsAlive())
        {
            return;
        }

        if (_isDisposed)
        {
            entity.Destruct();
            return;
        }

        if (entity.Has<PoolLeaseState>())
        {
            ref PoolLeaseState state = ref entity.GetMut<PoolLeaseState>();
            if (state.PoolId != _poolId || !state.IsRented)
            {
                // 別プールのエンティティ、または既にプール内に返却済みの二重返却を安全に無視
                return;
            }
            state.IsRented = false;
        }

        entity.Disable();
        EnsureArrayCapacity(_top + 1);
        _pool[_top++] = entity;
    }

    /// <summary>
    /// プール内の全エンティティ（空きおよび貸出中）を破棄し、リソースを解放します。
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            for (int i = 0; i < _totalAllocated; i++)
            {
                if (_allAllocated[i].IsAlive())
                {
                    _allAllocated[i].Destruct();
                }
            }
            _top = 0;
            _totalAllocated = 0;
            _isDisposed = true;
        }
    }
}
