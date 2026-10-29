using System.Runtime.CompilerServices;
using AnimalBattle.Components;

namespace AnimalBattle.Framework.Pooling;

/// <summary>
///     弾幕・エフェクト・近接判定等の戦闘エンティティプールを集中管理するシーン級ドメインサービス。
///     プレハブ定義と1対1に対応するプールマトリクスを保持し、
///     実行時のテーブル移動を排除してゼロアロケーションでの貸出と返却を提供します。
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

    /// <summary>Boss 近接斬撃判定用エンティティプールを取得します。</summary>
    public EntityPool BossSlashHitboxes { get; }

    /// <summary>
    ///     戦闘エンティティプールサービスを初期化し、各種 Prefab に対応する同構プールを事前確保します。
    /// </summary>
    /// <param name="world">エンティティが所属する Flecs.NET ワールド。</param>
    public CombatPoolService(World world)
    {
        PlayerBullets     = RegisterPool(new EntityPool(world, CombatPrefabs.PlayerBullet, 64, ResetPlayerBulletEntity));
        BossBullets       = RegisterPool(new EntityPool(world, CombatPrefabs.BossBulletBase, 512, ResetBossBulletEntity));
        SparkEffects      = RegisterPool(new EntityPool(world, VfxPrefabs.SparkVfx, 256, ResetEffectEntity));
        RingEffects       = RegisterPool(new EntityPool(world, VfxPrefabs.RingVfx, 64, ResetEffectEntity));
        AreaEffects       = RegisterPool(new EntityPool(world, VfxPrefabs.AreaVfx, 32, ResetEffectEntity));
        BossSlashHitboxes = RegisterPool(new EntityPool(world, CombatPrefabs.BossSlashHitbox, 16, ResetBossSlashHitboxEntity));
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

    /// <summary>Boss 近接斬撃判定エンティティをプールから借出します。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Entity RentBossSlashHitbox() => Current.BossSlashHitboxes.Rent();

    /// <summary>
    ///     エンティティを安全に回収します。
    ///     PoolIdに基づき所属元プールへ即時返却し、プール外エンティティは安全に破棄します。
    /// </summary>
    /// <param name="entity">回収するエンティティ。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Recycle(Entity entity)
    {
        if (!entity.IsAlive()) return;

        entity.Remove<OrbitOf>(Ecs.Wildcard);
        entity.Remove<RecycleTag>();

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
        e.Remove<InvincibleBlinkEffect>();
        e.Remove<OrbitOf>(Ecs.Wildcard);

        e.Set(new Transform(Vec2f.Zero));
        e.Set(new Velocity(Vec2f.Zero));

        ref var hitbox = ref e.GetMut<Hitbox>();
        hitbox.IsActive = true;
    }

    private static void ResetBossBulletEntity(Entity e)
    {
        e.Remove<RecycleTag>();
        e.Remove<HitFlashEffect>();
        e.Remove<InvincibleBlinkEffect>();
        e.Remove<OrbitOf>(Ecs.Wildcard);

        e.Set(new Transform(Vec2f.Zero));
        e.Set(new Velocity(Vec2f.Zero));

        var world = e.CsWorld();
        var defaultRadius = 0.0f;
        var defaultDamage = 0.0f;
        if (world.Has<CombatPrefabConfig>())
        {
            var cfg = world.Get<CombatPrefabConfig>().BossNormalBullet;
            defaultRadius = cfg.Radius;
            defaultDamage = cfg.Damage;
        }

        ref var bullet = ref e.GetMut<Bullet>();
        bullet.Radius = defaultRadius;
        bullet.IsIndestructible = false;

        ref var hitbox = ref e.GetMut<Hitbox>();
        hitbox.Shape = new CollisionShape.Circle(defaultRadius);
        hitbox.Damage = defaultDamage;
        hitbox.IsActive = true;
        hitbox.Faction = Faction.Hostile;
        hitbox.DestroyOnHit = true;

        ref var mod = ref e.GetMut<BulletModifier>();
        mod.Type = BulletType.Normal;
        mod.ElapsedTimer = 0.0f;
        mod.BaseRadius = 0.0f;
        mod.BaseVel = Vec2f.Zero;

        ref var sat = ref e.GetMut<OrbitSatellite>();
        sat.OrbitDistance = 0.0f;
        sat.InitialAngle = 0.0f;
    }

    private static void ResetEffectEntity(Entity e)
    {
        e.Remove<RecycleTag>();
        e.Remove<HitFlashEffect>();
        e.Remove<InvincibleBlinkEffect>();
        e.Remove<OrbitOf>(Ecs.Wildcard);

        e.Set(new Transform(Vec2f.Zero));
        e.Set(new Velocity(Vec2f.Zero));

        ref var fade = ref e.GetMut<FadeEffect>();
        fade.ElapsedTimer = 0.0f;
        fade.CurrentAlpha = fade.StartAlpha;
    }

    private static void ResetBossSlashHitboxEntity(Entity e)
    {
        e.Remove<RecycleTag>();
        e.Remove<HitFlashEffect>();
        e.Remove<InvincibleBlinkEffect>();
        e.Remove<OrbitOf>(Ecs.Wildcard);

        e.Set(new Transform(Vec2f.Zero));
        e.Set(new Velocity(Vec2f.Zero));

        ref var hist = ref e.GetMut<HitHistory>();
        hist.ResetHits();

        ref var hitbox = ref e.GetMut<Hitbox>();
        hitbox.IsActive = true;

        ref var lt = ref e.GetMut<HitboxLifetime>();
        lt.ElapsedTimer = 0.0f;
    }
}
