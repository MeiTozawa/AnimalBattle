using System.Diagnostics;
using System.Runtime.CompilerServices;
using AnimalBattle.Components;
using AnimalBattle.Prefabs;
using Flecs.NET.Core;

namespace AnimalBattle.CombatPool;

/// <summary>
///     弾幕・エフェクト・近接判定等の戦闘エンティティプールを集中管理するシーン級ドメインサービス。
///     Flecs Prefab と 1 対 1 に紐づく同構プール（Isomorphic Pool）マトリクスを保持し、
///     実行時の Archetype 変動（Table Move）を完全に排除したゼロ GC 借出・返却を提供します。
/// </summary>
public sealed class CombatPoolService : IDisposable
{
    private static CombatPoolService? _current;
    private readonly EntityPool?[] _poolsByPrefabKey = new EntityPool?[Enum.GetValues<PrefabKey>().Length];
    private readonly Dictionary<ulong, EntityPool> _poolsById = new();

    /// <summary>現在アクティブな戦闘エンティティプールサービスインスタンスを取得します。</summary>
    public static CombatPoolService Current => _current!;

    /// <summary>自機通常弾用エンティティプールを取得します。</summary>
    public EntityPool PlayerBullets => _poolsByPrefabKey[(int)PrefabKey.PlayerBullet]!;

    /// <summary>Boss 通常弾幕用エンティティプールを取得します。</summary>
    public EntityPool BossBullets => _poolsByPrefabKey[(int)PrefabKey.BossNormalBullet]!;

    /// <summary>点粒子スパーク用エンティティプールを取得します。</summary>
    public EntityPool SparkEffects => _poolsByPrefabKey[(int)PrefabKey.SparkVfx]!;

    /// <summary>リング演出用エンティティプールを取得します。</summary>
    public EntityPool RingEffects => _poolsByPrefabKey[(int)PrefabKey.RingVfx]!;

    /// <summary>判定付与エリア演出用エンティティプールを取得します。</summary>
    public EntityPool AreaEffects => _poolsByPrefabKey[(int)PrefabKey.AreaVfx]!;

    /// <summary>
    ///     戦闘エンティティプールサービスを初期化し、各種 Prefab に対応する同構プールを事前確保します。
    /// </summary>
    /// <param name="world">エンティティが所属する Flecs.NET ワールド。</param>
    public CombatPoolService(World world)
    {
        RegisterPool(PrefabKey.PlayerBullet, new EntityPool(world, PrefabRegistry.GetPrefab(PrefabKey.PlayerBullet), 128, ResetBulletEntity));
        RegisterPool(PrefabKey.BossNormalBullet, new EntityPool(world, PrefabRegistry.GetPrefab(PrefabKey.BossNormalBullet), 64, ResetBulletEntity));
        RegisterPool(PrefabKey.BossMagentaBullet, new EntityPool(world, PrefabRegistry.GetPrefab(PrefabKey.BossMagentaBullet), 64, ResetBulletEntity));
        RegisterPool(PrefabKey.BossYellowBullet, new EntityPool(world, PrefabRegistry.GetPrefab(PrefabKey.BossYellowBullet), 64, ResetBulletEntity));
        RegisterPool(PrefabKey.BossYellowSatellite, new EntityPool(world, PrefabRegistry.GetPrefab(PrefabKey.BossYellowSatellite), 64, ResetBulletEntity));
        RegisterPool(PrefabKey.BossBlueBullet, new EntityPool(world, PrefabRegistry.GetPrefab(PrefabKey.BossBlueBullet), 64, ResetBulletEntity));
        RegisterPool(PrefabKey.BossBlackBullet, new EntityPool(world, PrefabRegistry.GetPrefab(PrefabKey.BossBlackBullet), 64, ResetBulletEntity));
        RegisterPool(PrefabKey.SparkVfx, new EntityPool(world, PrefabRegistry.GetPrefab(PrefabKey.SparkVfx), 256, ResetEffectEntity));
        RegisterPool(PrefabKey.RingVfx, new EntityPool(world, PrefabRegistry.GetPrefab(PrefabKey.RingVfx), 64, ResetEffectEntity));
        RegisterPool(PrefabKey.AreaVfx, new EntityPool(world, PrefabRegistry.GetPrefab(PrefabKey.AreaVfx), 32, ResetEffectEntity));
    }

    private void RegisterPool(PrefabKey key, EntityPool pool)
    {
        _poolsByPrefabKey[(int)key] = pool;
        _poolsById[pool.PoolId] = pool;
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

    /// <summary>
    ///     指定された PrefabKey に対応する同構プールからエンティティを借出します。
    /// </summary>
    /// <param name="key">対象の PrefabKey。</param>
    /// <returns>有効化されたエンティティ。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Entity Rent(PrefabKey key)
    {
        var pool = Current._poolsByPrefabKey[(int)key];
        if (pool != null) return pool.Rent();

        Debug.Fail($"[CombatPoolService 警告] PrefabKey {key} に対応するプールが登録されていません。");
        return default;
    }

    /// <summary>自機通常弾エンティティをプールから借出します。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Entity RentPlayerBullet() => Rent(PrefabKey.PlayerBullet);

    /// <summary>Boss 通常弾幕エンティティをプールから借出します。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Entity RentBossBullet() => Rent(PrefabKey.BossNormalBullet);

    /// <summary>スパーク演出用エンティティをプールから借出します。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Entity RentSparkEffect() => Rent(PrefabKey.SparkVfx);

    /// <summary>リング演出用エンティティをプールから借出します。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Entity RentRingEffect() => Rent(PrefabKey.RingVfx);

    /// <summary>エリア演出用エンティティをプールから借出します。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Entity RentAreaEffect() => Rent(PrefabKey.AreaVfx);

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
        Array.Clear(_poolsByPrefabKey);

        if (_current == this) _current = null;
    }

    private static void ResetBulletEntity(Entity e)
    {
        e.Remove<RecycleTag>();
        e.Remove<HitFlashEffect>();
        e.Remove<IndestructibleBulletTag>();
        if (e.Has<OrbitSatellite>()) e.Remove<OrbitSatellite>();
    }

    private static void ResetEffectEntity(Entity e)
    {
        e.Remove<RecycleTag>();
        e.Remove<HitFlashEffect>();
    }
}
