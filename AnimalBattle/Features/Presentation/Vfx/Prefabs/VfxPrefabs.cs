using System.Drawing;

namespace AnimalBattle.Presentation.Vfx;

/// <summary>
///     視覚演出：VFXドメインの Prefab 登録および管理。
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
    ///     全 VFX 関連の Prefab プレハブを登録します。
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

        // --- 3. スプライト残影演出 ---
        var areaCfg = cfg.AreaVfx;
        AreaVfx = world.Prefab(AreaVfxName)
            .IsA(CommonPrefabs.BaseVfx)
            .Set(new Sprite(-1))
            .Set(new FadeEffect(areaCfg.DefaultDuration));
    }
}
