using System.Drawing;
using AnimalBattle.Ai;
using AnimalBattle.CombatPool;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Core.Geometry;
using AnimalBattle.Prefabs;
using Flecs.NET.Core;

namespace AnimalBattle.Combat;

/// <summary>
///     敵性弾幕（Boss 弾幕・特殊弾幕・地面衝撃波・反撃黒弾）の生成および一括消去を管理する静的サービスです。
///     Flecs Prefab と同構プールに基づき、動的な Archetype 変更を伴わずに高速生成します。
/// </summary>
public static class BulletSpawner
{
    /// <summary>
    ///     指定座標に Boss 通常弾幕エンティティを生成します。
    /// </summary>
    public static void SpawnBossBullet(World world, float x, float y, float vx, float vy, float radius, Color fillColor,
        Color outlineColor, bool isIndestructible = false)
    {
        SpawnBossSpecialBullet(world, x, y, vx, vy, radius, BulletType.Normal, fillColor, outlineColor, isIndestructible);
    }

    /// <summary>
    ///     指定座標に特殊挙動（品紅・黄・青・黒）を持つ Boss 弾幕エンティティを生成します。
    /// </summary>
    public static void SpawnBossSpecialBullet(
        World world,
        float x,
        float y,
        float vx,
        float vy,
        float radius,
        BulletType type,
        Color? fillColor = null,
        Color? outlineColor = null,
        bool isIndestructible = false)
    {
        var (fill, outline) = BulletPalette.ResolveBulletColors(type, fillColor, outlineColor);
        var bullet = new Bullet(radius, fill, outline) {
            IsIndestructible = isIndestructible
        };

        // 統合された BossBullets プールから借出
        var entity = CombatPoolService.RentBossBullet();
        entity
            .Set(new Transform(x, y))
            .Set(new Velocity(vx, vy))
            .Set(new BulletModifier(type, radius, vx, vy))
            .Set(bullet);

        if (type == BulletType.Yellow)
        {
            var cfg = ConfigManager.SpecialBullets;
            var satRadius = MathF.Max(cfg.YellowSatelliteMinRadius, radius * cfg.YellowSatelliteRadiusMultiplier);
            var orbitDist = radius * cfg.YellowOrbitDistanceMultiplier;
            var parentVersion = entity.Get<PoolLeaseState>().Version;

            var (satFill, satOutline) = BulletPalette.ResolveBulletColors(BulletType.Yellow, fillColor, outlineColor);
            var satBullet = new Bullet(satRadius, satFill, satOutline) {
                IsIndestructible = isIndestructible
            };
            var satEntity = CombatPoolService.RentBossBullet();
            satEntity
                .Set(new Transform(x + orbitDist, y))
                .Set(new Velocity(vx, vy))
                .Set(new BulletModifier(BulletType.Yellow, satRadius, vx, vy))
                .Set(satBullet)
                .Set(new OrbitSatellite(true, entity, orbitDist, 0.0f, parentVersion));
        }
    }

    /// <summary>
    ///     Boss 被弾時に、現在の Phase に応じた数量の黑色弾幕を全方位ランダムに発射します。
    /// </summary>
    public static void SpawnBossCounterAttack(World world, float bossX, float bossY, Phase phase)
    {
        var cfg = ConfigManager.SpecialBullets;
        var bulletCount = phase switch {
            Phase.Phase1 => cfg.CounterBulletCountPhase1,
            Phase.Phase2 => cfg.CounterBulletCountPhase2,
            Phase.Phase3 => cfg.CounterBulletCountPhase3,
            _ => AssertHelper.FailWithDefault($"[BulletSpawner 警告] 未知のフェーズ: {phase}", cfg.CounterBulletCountPhase1)
        };

        for (var i = 0; i < bulletCount; i++)
        {
            var angle = Random.Shared.NextSingle() * MathF.PI * 2.0f;
            var vx = MathF.Cos(angle) * cfg.CounterBulletSpeed;
            var vy = MathF.Sin(angle) * cfg.CounterBulletSpeed;

            SpawnBossSpecialBullet(
                world,
                bossX,
                bossY,
                vx,
                vy,
                cfg.CounterBulletRadius,
                BulletType.Black,
                isIndestructible: cfg.CounterBulletIsIndestructible);
        }
    }

    /// <summary>
    ///     地面を這う衝撃波弾幕エンティティを生成します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド。</param>
    /// <param name="x">生成 X 座標。</param>
    /// <param name="y">生成 Y 座標。</param>
    /// <param name="vx">初速 X 成分。</param>
    /// <param name="radius">判定半径。</param>
    /// <param name="damage">威力ダメージ。</param>
    /// <param name="fillColor">塗りつぶしカラー。</param>
    /// <param name="outlineColor">外枠発光カラー。</param>
    /// <param name="isIndestructible">近接攻撃で破壊不能かどうかのフラグ。</param>
    public static void SpawnGroundShockwave(World world, float x, float y, float vx, float radius, float damage,
        Color fillColor, Color outlineColor, bool isIndestructible)
    {
        var bullet = new Bullet(radius, fillColor, outlineColor) {
            IsIndestructible = isIndestructible
        };

        var entity = CombatPoolService.RentBossBullet();
        entity
            .Set(new Transform(x, y))
            .Set(new Velocity(vx, 0.0f))
            .Set(bullet)
            .Set(new Hitbox(new CollisionShape.Circle(radius), damage, Faction.Hostile));
    }

    /// <summary>
    ///     フィールド上に存在するすべての Boss 弾幕を一括消去します。
    ///     シミュレーション実行中の時序競合および二重返却を防止するため、
    ///     即時返却ではなく <see cref="RecycleTag" /> を付与し、
    ///     フレーム末尾の <see cref="CombatRecycleSystem" /> へ回収を一元委譲します。
    /// </summary>
    public static void ClearAllBossBullets(World world)
    {
        using var query = world.QueryBuilder()
           .With<BossBulletTag>()
           .Without<RecycleTag>()
           .Build();
        query.Each(e => e.Add<RecycleTag>());
    }
}
