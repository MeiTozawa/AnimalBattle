using System;
using Flecs.NET.Core;

namespace AnimalBattle.Core;

/// <summary>
/// 高頻度・短命エンティティ（弾幕・パーティクル・衝撃波など）向けの高性能事前確保エンティティプール。
/// 実行時の <see cref="World.Entity()"/> によるメモリ再確保および <see cref="Entity.Destruct()"/> による Archetype 変動を廃し、
/// <see cref="Entity.Enable()"/> と <see cref="Entity.Disable()"/> によるゼロ GC・安定した Archetype 運用を提供します。
/// </summary>
public sealed class EntityPool : IDisposable
{
    private readonly World _world;
    private readonly Action<Entity>? _onInit;
    private Entity[] _pool;
    private int _top;
    private int _totalAllocated;
    private bool _isDisposed;

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
    public EntityPool(World world, int initialCapacity = 64, Action<Entity>? onInit = null)
    {
        _world = world;
        _onInit = onInit;
        initialCapacity = Math.Max(1, initialCapacity);

        _pool = new Entity[initialCapacity];
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
            e.Disable();
            _onInit?.Invoke(e);
            _pool[_top++] = e;
            _totalAllocated++;
        }
    }

    private void EnsureArrayCapacity(int required)
    {
        if (_pool.Length < required)
        {
            int newCap = Math.Max(_pool.Length * 2, required);
            Array.Resize(ref _pool, newCap);
        }
    }

    /// <summary>
    /// プールからエンティティを貸し出し、有効化（Enable）します。
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
        e.Enable();
        return e;
    }

    /// <summary>
    /// 使用済みのエンティティをプールへ返却し、無効化（Disable）します。
    /// </summary>
    /// <param name="entity">返却するエンティティ。</param>
    public void Return(Entity entity)
    {
        if (_isDisposed || !entity.IsAlive())
        {
            return;
        }

        entity.Disable();
        EnsureArrayCapacity(_top + 1);
        _pool[_top++] = entity;
    }

    /// <summary>
    /// プール内の全エンティティを破棄し、リソースを解放します。
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            for (int i = 0; i < _top; i++)
            {
                if (_pool[i].IsAlive())
                {
                    _pool[i].Destruct();
                }
            }
            _top = 0;
            _totalAllocated = 0;
            _isDisposed = true;
        }
    }
}
