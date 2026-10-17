using System.Diagnostics;
using System.Runtime.CompilerServices;
using AnimalBattle.Combat;
using AnimalBattle.Components;
using AnimalBattle.Presentation;
using Flecs.NET.Core;

namespace AnimalBattle.Framework.Pooling;

/// <summary>
///     弾幕・エフェクト・近接判定等の戦闘エンティティプールを集中管理するシーン級ドメインサービス。
///     Flecs Prefab と 1 対 1 に紐づく同構プール（Isomorphic Pool）マトリクスを保持し、
///     実行時の Archetype 変動（Table Move）を完全に排除したゼロ GC 借出・返却を提供します。
/// </summary>
public sealed class CombatPoolService : IDisposable
{
    private static CombatPoolService? _current;
    private readonly Dictionary<ulong, EntityPool> _poolsById = new();

    /// <summary>現在アクティブな戦闘エンティティプールサービスインスタンスを取得します。</summary>
    public static CombatPoolService Current => _current!;

    /// <summary>自機通常弾用エンティティプールを取得します。</summary>
    public EntityPool PlayerBullets { get; }

    /// <summary>Boss 通常弾幕用エンティティプールを取得します。</summary>
    public EntityPool BossBullets { get; }

    /// <summary>点粒子スパーク用エンティティプールを取得します。</summary>
    public EntityPool SparkEffects { get; }

    /// <summary>リング演出用エンティティプールを取得します。</summary>
    public EntityPool RingEffects { get; }

    /// <summary>判定付与エリア演出用エンティティプールを取得します。</summary>
    public EntityPool AreaEffects { get; }

    /// <summary>
    ///     戦闘エンティティプールサービスを初期化し、各種 Prefab に対応する同構プールを事前確保します。
    /// </summary>
    /// <param name="world">エンティティが所属する Flecs.NET ワールド。</param>
    public CombatPoolService(World world)
    {
        PlayerBullets = RegisterPool(new EntityPool(world, CombatPrefabs.PlayerBullet, 64, ResetPlayerBulletEntity));
        BossBullets   = RegisterPool(new EntityPool(world, CombatPrefabs.BossBulletBase, 256, ResetBossBulletEntity));
        SparkEffects  = RegisterPool(new EntityPool(world, VfxPrefabs.SparkVfx, 256, ResetEffectEntity));
        RingEffects   = RegisterPool(new EntityPool(world, VfxPrefabs.RingVfx, 64, ResetEffectEntity));
        AreaEffects   = RegisterPool(new EntityPool(world, VfxPrefabs.AreaVfx, 32, ResetEffectEntity));
    }

    private EntityPool RegisterPool(EntityPool pool)
    {
        _poolsById[pool.PoolId] = pool;
        return pool;
    }

    /// <summary>
    ///     戦闘エンティティプールサービスを初期化し、現在のアクティブインスタンスとして登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド。</param>
    /// <returns>初期化されたサービスインスタンス。</returns>
    public static CombatPoolService Initialize(World world)
    {
        _current?.Dispose();
        _current = new CombatPoolService(world);
        return _current;
    }

    /// <summary>自機通常弾エンティティをプールから借出します。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Entity RentPlayerBullet() => Current.PlayerBullets.Rent();

    /// <summary>Boss 通常弾幕エンティティをプールから借出します。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Entity RentBossBullet() => Current.BossBullets.Rent();

    /// <summary>スパーク演出用エンティティをプールから借出します。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Entity RentSparkEffect() => Current.SparkEffects.Rent();

    /// <summary>リング演出用エンティティをプールから借出します。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Entity RentRingEffect() => Current.RingEffects.Rent();

    /// <summary>エリア演出用エンティティをプールから借出します。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Entity RentAreaEffect() => Current.AreaEffects.Rent();

    /// <summary>
    ///     エンティティを安全に回収します。
    ///     PoolId に基づき所属元の同構プールへ O(1) で即時返却（Disable）し、非プールエンティティは Destruct します。
    /// </summary>
    /// <param name="entity">回収するエンティティ。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Recycle(Entity entity)
    {
        if (!entity.IsAlive()) return;

        if (entity.Has<PoolLeaseState>())
        {
            var poolId = entity.Get<PoolLeaseState>().PoolId;
            if (Current._poolsById.TryGetValue(poolId, out var pool))
            {
                pool.Return(entity);
                return;
            }
        }

        entity.Destruct();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var pool in _poolsById.Values)
        {
            pool.Dispose();
        }

        _poolsById.Clear();
        if (_current == this) _current = null;
    }

    private static void ResetPlayerBulletEntity(Entity e)
    {
        e.Remove<RecycleTag>();
        e.Remove<HitFlashEffect>();
    }

    private static void ResetBossBulletEntity(Entity e)
    {
        e.Remove<RecycleTag>();
        e.Remove<HitFlashEffect>();

        ref var bullet = ref e.GetMut<Bullet>();
        bullet.IsIndestructible = false;

        ref var mod = ref e.GetMut<BulletModifier>();
        mod.Type = BulletType.Normal;
        mod.ElapsedTimer = 0.0f;

        ref var sat = ref e.GetMut<OrbitSatellite>();
        sat.IsActive = false;
        sat.Parent = default;
        sat.ParentVersion = 0;
    }

    private static void ResetEffectEntity(Entity e)
    {
        e.Remove<RecycleTag>();
        e.Remove<HitFlashEffect>();
    }
}
