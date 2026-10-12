using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using AnimalBattle.Components;
using Flecs.NET.Core;

namespace AnimalBattle.CombatPool;

/// <summary>
///     弾幕・エフェクト用の戦闘エンティティプールを統括管理するシーン級ドメインサービス。
///     ECS コンポーネント（world.Get）のルックアップオーバーヘッドおよび P/Invoke を根絶し、
///     直接メモリ参照による高速借出・返却を提供します。
/// </summary>
public sealed class CombatPoolService : IDisposable {
    private static CombatPoolService? _current;

    /// <summary>現在アクティブな戦闘エンティティプールサービスインスタンスを取得します。</summary>
    public static CombatPoolService Current => _current!;

    /// <summary>自機通常弾用エンティティプールを取得します。</summary>
    public EntityPool PlayerBullets { get; }

    /// <summary>Boss 弾幕用エンティティプールを取得します。</summary>
    public EntityPool BossBullets { get; }

    /// <summary>点粒子スパーク用エンティティプールを取得します。</summary>
    public EntityPool SparkEffects { get; }

    /// <summary>リング演出用エンティティプールを取得します。</summary>
    public EntityPool RingEffects { get; }

    /// <summary>判定付与エリア演出用エンティティプールを取得します。</summary>
    public EntityPool AreaEffects { get; }

    /// <summary>
    ///     <see cref="CombatPoolService" /> の新しいインスタンスを初期化し、各種同構エンティティを事前確保します。
    /// </summary>
    /// <param name="world">エンティティが所属する Flecs.NET ワールド。</param>
    public CombatPoolService(World world)
    {
        PlayerBullets = new EntityPool(
            world,
            128,
            e => e.Set(new PooledEntity(PooledEntityType.PlayerBullet))
               .Set(new Transform(0, 0))
               .Set(new Velocity(0))
               .Set(new CollisionBox(0, 0))
               .Set(new Hitbox(default, 0))
               .Add<PlayerBulletTag>(),
            ResetBulletEntity);

        BossBullets = new EntityPool(
            world,
            256,
            e => e.Set(new PooledEntity(PooledEntityType.BossBullet))
               .Set(new Transform(0, 0))
               .Set(new Velocity(0))
               .Set(new Bullet(0, Color.Empty, Color.Empty))
               .Set(new Hitbox(default, 0, Faction.Hostile))
               .Add<BossBulletTag>(),
            ResetBulletEntity);

        SparkEffects = new EntityPool(
            world,
            256,
            e => e.Set(new PooledEntity(PooledEntityType.SparkEffect))
               .Set(new Transform(0, 0))
               .Set(new Velocity(0))
               .Set(new Bullet(0, Color.Empty, Color.Empty))
               .Set(new FadeEffect(1.0f))
               .Add<VfxTag>(),
            ResetEffectEntity);

        RingEffects = new EntityPool(
            world,
            64,
            e => e.Set(new PooledEntity(PooledEntityType.RingEffect))
               .Set(new Transform(0, 0))
               .Set(new RingEffect(0, 0, Color.Empty))
               .Set(new FadeEffect(1.0f))
               .Add<VfxTag>(),
            ResetEffectEntity);

        AreaEffects = new EntityPool(
            world,
            32,
            e => e.Set(new PooledEntity(PooledEntityType.AreaEffect))
               .Set(new Transform(0, 0))
               .Set(new Velocity(0))
               .Set(new Hitbox(default, 0, Faction.Hostile, false))
               .Set(new Sprite(-1))
               .Set(new FadeEffect(1.0f))
               .Add<VfxTag>(),
            ResetEffectEntity);
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

    /// <summary>Boss 弾幕エンティティをプールから借出します。</summary>
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
    ///     エンティティを安全に回収します。プール所属エンティティはプールへ返却（Disable）し、それ以外は Destruct します。
    ///     ワールド引数を不要化し、直接ドメインサービスへ O(1) 委譲します。
    /// </summary>
    /// <param name="entity">回収するエンティティ。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Recycle(Entity entity)
    {
        if (!entity.IsAlive()) return;

        if (entity.Has<PooledEntity>())
        {
            var type = entity.Get<PooledEntity>().PoolType;
            var service = Current;
            switch (type)
            {
                case PooledEntityType.PlayerBullet:
                    service.PlayerBullets.Return(entity);
                    return;
                case PooledEntityType.BossBullet:
                    service.BossBullets.Return(entity);
                    return;
                case PooledEntityType.SparkEffect:
                    service.SparkEffects.Return(entity);
                    return;
                case PooledEntityType.RingEffect:
                    service.RingEffects.Return(entity);
                    return;
                case PooledEntityType.AreaEffect:
                    service.AreaEffects.Return(entity);
                    return;
                default:
                    Debug.Fail($"[CombatPoolService 警告] 未知の PooledEntityType: {type}");
                    break;
            }
        }

        entity.Destruct();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        PlayerBullets.Dispose();
        BossBullets.Dispose();
        SparkEffects.Dispose();
        RingEffects.Dispose();
        AreaEffects.Dispose();
        if (_current == this) _current = null;
    }

    private static void ResetBulletEntity(Entity e)
    {
        e.Remove<HitFlashEffect>();
        e.Remove<BulletModifier>();
        e.Remove<OrbitSatellite>();
    }

    private static void ResetEffectEntity(Entity e)
    {
        e.Remove<HitFlashEffect>();
    }
}
