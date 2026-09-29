using System.Drawing;
using Flecs.NET.Core;

namespace AnimalBattle.Core;

/// <summary>
///     プール管理対象エンティティの所属種別を表す列挙体。
/// </summary>
public enum PooledEntityType : byte {
    /// <summary>自機通常弾。</summary>
    PlayerBullet = 0,

    /// <summary>Boss 弾幕。</summary>
    BossBullet = 1,

    /// <summary>スパーク・微小爆発などの点粒子エフェクト。</summary>
    SparkEffect = 2,

    /// <summary>ジャンプリングや衝撃波などの円環エフェクト。</summary>
    RingEffect = 3,

    /// <summary>近接斬撃や判定を伴うエリアエフェクト。</summary>
    AreaEffect = 4
}

/// <summary>
///     プール管理対象エンティティであることを示す識別コンポーネント。
///     リサイクル時に対応するプールへの返却を O(1) で判定します。
/// </summary>
public readonly record struct PooledEntity(PooledEntityType PoolType);

/// <summary>
///     弾幕・エフェクト用の共有エンティティプールを保持する ECS シングルトンコンポーネント。
///     同構（Homogeneous）な Archetype ごとに専用プールを分離し、実行時の Archetype Table 移行を根絶します。
/// </summary>
public sealed class CombatEntityPoolComponent : IDisposable {
    /// <summary>
    ///     <see cref="CombatEntityPoolComponent" /> の新しいインスタンスを初期化し、各種同構エンティティを事前確保します。
    /// </summary>
    /// <param name="world">エンティティが所属する Flecs.NET ワールド。</param>
    public CombatEntityPoolComponent(World world)
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
               .Add<EffectTag>(),
            ResetEffectEntity);

        RingEffects = new EntityPool(
            world,
            64,
            e => e.Set(new PooledEntity(PooledEntityType.RingEffect))
               .Set(new Transform(0, 0))
               .Set(new RingEffect(0, 0, Color.Empty))
               .Set(new FadeEffect(1.0f))
               .Add<EffectTag>(),
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
               .Add<EffectTag>(),
            ResetEffectEntity);
    }

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

    /// <inheritdoc />
    public void Dispose()
    {
        PlayerBullets.Dispose();
        BossBullets.Dispose();
        SparkEffects.Dispose();
        RingEffects.Dispose();
        AreaEffects.Dispose();
    }

    /// <summary>スパークエフェクト用エンティティを貸し出します。</summary>
    public Entity RentSparkEffect()
    {
        return SparkEffects.Rent();
    }

    /// <summary>リングエフェクト用エンティティを貸し出します。</summary>
    public Entity RentRingEffect()
    {
        return RingEffects.Rent();
    }

    /// <summary>エリアエフェクト用エンティティを貸し出します。</summary>
    public Entity RentAreaEffect()
    {
        return AreaEffects.Rent();
    }

    private static void ResetBulletEntity(Entity e)
    {
        if (e.Has<HitFlashEffect>()) e.Remove<HitFlashEffect>();
    }

    private static void ResetEffectEntity(Entity e)
    {
        if (e.Has<HitFlashEffect>()) e.Remove<HitFlashEffect>();
    }
}

/// <summary>
///     戦闘エンティティプールの借出・返却・シングルトン管理を仲介するヘルパークラス。
/// </summary>
public static class CombatEntityPoolHelper {
    /// <summary>
    ///     Flecs ワールドから共有戦闘エンティティプールを取得します（未登録の場合は遅延生成して登録）。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド。</param>
    /// <returns>戦闘エンティティプールコンポーネント。</returns>
    public static CombatEntityPoolComponent GetCombatEntityPool(this World world)
    {
        if (!world.Has<CombatEntityPoolComponent>())
        {
            CombatEntityPoolComponent comp = new(world);
            world.Set(comp);
            return comp;
        }

        return world.Get<CombatEntityPoolComponent>();
    }

    /// <summary>
    ///     自機通常弾エンティティをプールから借出します。
    /// </summary>
    public static Entity RentPlayerBullet(World world)
    {
        return world.GetCombatEntityPool().PlayerBullets.Rent();
    }

    /// <summary>
    ///     Boss 弾幕エンティティをプールから借出します。
    /// </summary>
    public static Entity RentBossBullet(World world)
    {
        return world.GetCombatEntityPool().BossBullets.Rent();
    }

    /// <summary>
    ///     スパーク演出用エンティティをプールから借出します。
    /// </summary>
    public static Entity RentSparkEffect(World world)
    {
        return world.GetCombatEntityPool().SparkEffects.Rent();
    }

    /// <summary>
    ///     リング演出用エンティティをプールから借出します。
    /// </summary>
    public static Entity RentRingEffect(World world)
    {
        return world.GetCombatEntityPool().RingEffects.Rent();
    }

    /// <summary>
    ///     エリア演出用エンティティをプールから借出します。
    /// </summary>
    public static Entity RentAreaEffect(World world)
    {
        return world.GetCombatEntityPool().AreaEffects.Rent();
    }

    /// <summary>
    ///     エンティティを安全に回収します。プール所属エンティティはプールへ返却（Disable）し、それ以外は Destruct します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド。</param>
    /// <param name="entity">回収するエンティティ。</param>
    public static void Recycle(World world, Entity entity)
    {
        if (!entity.IsAlive()) return;

        if (entity.Has<PooledEntity>())
        {
            var type = entity.Get<PooledEntity>().PoolType;
            var poolComp = world.GetCombatEntityPool();
            switch (type)
            {
                case PooledEntityType.PlayerBullet:
                    poolComp.PlayerBullets.Return(entity);
                    return;
                case PooledEntityType.BossBullet:
                    poolComp.BossBullets.Return(entity);
                    return;
                case PooledEntityType.SparkEffect:
                    poolComp.SparkEffects.Return(entity);
                    return;
                case PooledEntityType.RingEffect:
                    poolComp.RingEffects.Return(entity);
                    return;
                case PooledEntityType.AreaEffect:
                    poolComp.AreaEffects.Return(entity);
                    return;
            }
        }

        entity.Destruct();
    }
}