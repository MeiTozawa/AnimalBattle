using System.Drawing;
using AnimalBattle.Combat;
using AnimalBattle.Common;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core.Geometry;
using Flecs.NET.Core;

namespace AnimalBattle.Presentation.Vfx;

/// <summary>
///     視覚演出（VFX）ドメインの Prefab 登録および管理。
/// </summary>
public static class VfxPrefabs
{
    public const string SparkVfxName = "SparkVfxPrefab";
    public const string RingVfxName = "RingVfxPrefab";
    public const string AreaVfxName = "AreaVfxPrefab";

    public static Entity SparkVfx { get; private set; }
    public static Entity RingVfx { get; private set; }
    public static Entity AreaVfx { get; private set; }

    /// <summary>
    ///     全 VFX 関連の Prefab 原型を登録します。
    /// </summary>
    public static void Register(World world)
    {
        var cfg = world.Get<CombatPrefabConfig>();

        // --- 1. 点粒子スパーク演出 ---
        var sparkCfg = cfg.SparkVfx;
        SparkVfx = world.Prefab(SparkVfxName)
            .IsA(CommonPrefabs.BaseVfx)
            .Set(new Velocity(Vec2f.Zero))
            .Set(new Bullet(sparkCfg.DefaultRadius, Color.Empty, Color.Empty))
            .Set(new FadeEffect(sparkCfg.DefaultDuration));

        // --- 2. リング拡散演出 ---
        var ringCfg = cfg.RingVfx;
        RingVfx = world.Prefab(RingVfxName)
            .IsA(CommonPrefabs.BaseVfx)
            .Set(new RingEffect(ringCfg.InitialRadius, ringCfg.MaxRadius, Color.Empty))
            .Set(new FadeEffect(ringCfg.DefaultDuration));

        // --- 3. 判定付与エリア演出 ---
        var areaCfg = cfg.AreaVfx;
        AreaVfx = world.Prefab(AreaVfxName)
            .IsA(CommonPrefabs.BaseVfx)
            .Set(new Velocity(Vec2f.Zero))
            .Set(new Hitbox(new CollisionShape.Box(Vec2f.Zero), areaCfg.Damage, Faction.Hostile, false))
            .Set(new Sprite(-1))
            .Set(new FadeEffect(areaCfg.DefaultDuration));
    }
}
