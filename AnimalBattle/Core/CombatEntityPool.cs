using System;
using Flecs.NET.Core;

namespace AnimalBattle.Core;

/// <summary>
/// プール管理対象エンティティの所属種別を表す列挙体。
/// </summary>
public enum PooledEntityType : byte
{
    /// <summary>自機通常弾。</summary>
    PlayerBullet = 0,

    /// <summary>Boss 弾幕。</summary>
    BossBullet = 1,

    /// <summary>パーティクル・火花・衝撃波などの演出エフェクト。</summary>
    Effect = 2
}

/// <summary>
/// プール管理対象エンティティであることを示す識別コンポーネント。
/// リサイクル時に対応するプールへの返却を O(1) で判定します。
/// </summary>
public readonly record struct PooledEntity(PooledEntityType PoolType);

/// <summary>
/// 弾幕・エフェクト用の共有エンティティプールを保持する ECS シングルトンコンポーネント。
/// </summary>
public sealed class CombatEntityPoolComponent : IDisposable
{
    /// <summary>自機通常弾用エンティティプールを取得します。</summary>
    public EntityPool PlayerBullets { get; }

    /// <summary>Boss 弾幕用エンティティプールを取得します。</summary>
    public EntityPool BossBullets { get; }

    /// <summary>パーティクル・演出エフェクト用エンティティプールを取得します。</summary>
    public EntityPool Effects { get; }

    /// <summary>
    /// <see cref="CombatEntityPoolComponent"/> の新しいインスタンスを初期化し、各種エンティティを事前確保します。
    /// </summary>
    /// <param name="world">エンティティが所属する Flecs.NET ワールド。</param>
    public CombatEntityPoolComponent(World world)
    {
        PlayerBullets = new EntityPool(world, initialCapacity: 128, e => e.Set(new PooledEntity(PooledEntityType.PlayerBullet)));
        BossBullets = new EntityPool(world, initialCapacity: 256, e => e.Set(new PooledEntity(PooledEntityType.BossBullet)));
        Effects = new EntityPool(world, initialCapacity: 256, e => e.Set(new PooledEntity(PooledEntityType.Effect)));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        PlayerBullets.Dispose();
        BossBullets.Dispose();
        Effects.Dispose();
    }
}

/// <summary>
/// 戦闘エンティティプールの借出・返却・シングルトン管理を仲介するヘルパークラス。
/// </summary>
public static class CombatEntityPoolHelper
{
    /// <summary>
    /// Flecs ワールドから共有戦闘エンティティプールを取得します（未登録の場合は遅延生成して登録）。
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
    /// 自機通常弾エンティティをプールから借出します。
    /// </summary>
    public static Entity RentPlayerBullet(World world)
    {
        return world.GetCombatEntityPool().PlayerBullets.Rent();
    }

    /// <summary>
    /// Boss 弾幕エンティティをプールから借出します。
    /// </summary>
    public static Entity RentBossBullet(World world)
    {
        return world.GetCombatEntityPool().BossBullets.Rent();
    }

    /// <summary>
    /// 演出エフェクトエンティティをプールから借出します。
    /// </summary>
    public static Entity RentEffect(World world)
    {
        return world.GetCombatEntityPool().Effects.Rent();
    }

    /// <summary>
    /// エンティティを安全に回収します。プール所属エンティティはプールへ返却（Disable）し、それ以外は Destruct します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド。</param>
    /// <param name="entity">回収するエンティティ。</param>
    public static void Recycle(World world, Entity entity)
    {
        if (!entity.IsAlive())
        {
            return;
        }

        if (entity.Has<PooledEntity>())
        {
            PooledEntityType type = entity.Get<PooledEntity>().PoolType;
            CombatEntityPoolComponent poolComp = world.GetCombatEntityPool();
            switch (type)
            {
                case PooledEntityType.PlayerBullet:
                    poolComp.PlayerBullets.Return(entity);
                    return;
                case PooledEntityType.BossBullet:
                    poolComp.BossBullets.Return(entity);
                    return;
                case PooledEntityType.Effect:
                    poolComp.Effects.Return(entity);
                    return;
            }
        }

        entity.Destruct();
    }
}
