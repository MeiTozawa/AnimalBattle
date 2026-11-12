namespace AnimalBattle.Player;

using System.IO;

/// <summary>
///     特殊能力（必殺技）Prefabs プレハブの登録・管理およびエンティティへの排他バインド機構。
/// </summary>
public static class SpecialAbilityPrefabs
{
    public const string GiantOrbName = "GiantOrbAbilityPrefab";
    public const string ScreenCleaveName = "ScreenCleaveAbilityPrefab";

    public static Entity GiantOrb { get; private set; }
    public static Entity ScreenCleave { get; private set; }

    /// <summary>
    ///     特殊能力の基底 Prefab をワールドへ登録します。
    /// </summary>
    public static void Register(World world, GiantOrbAbilityConfig orbCfg, ScreenCleaveAbilityConfig cleaveCfg)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");
        Debug.Assert(orbCfg != null, "GiantOrbAbilityConfig は null にできません。");
        Debug.Assert(cleaveCfg != null, "ScreenCleaveAbilityConfig は null にできません。");

        GiantOrb = world.Prefab(GiantOrbName)
            .Set(GiantOrbCapability.FromConfig(orbCfg));

        ScreenCleave = world.Prefab(ScreenCleaveName)
            .Set(ScreenCleaveCapability.FromConfig(cleaveCfg));
    }

    /// <summary>
    ///     指定された特殊能力名に対応する Prefab を取得します。
    /// </summary>
    public static Entity Get(string abilityName)
    {
        var normalized = abilityName.Trim();
        if (normalized.EndsWith(".toml", StringComparison.OrdinalIgnoreCase))
        {
            normalized = Path.GetFileNameWithoutExtension(normalized);
        }

        return normalized switch
        {
            "GiantOrb" => GiantOrb,
            "ScreenCleave" => ScreenCleave,
            _ => AssertHelper.FailWithDefault($"未知の特殊能力です: {abilityName}", GiantOrb)
        };
    }

    /// <summary>
    ///     対象エンティティへ特殊能力 Prefab を排他的にアタッチ（IsA 継承）します。
    /// </summary>
    public static void Attach(Entity entity, string abilityName)
    {
        if (string.IsNullOrEmpty(abilityName)) return;

        var skillPrefab = Get(abilityName);
        if (GiantOrb.IsAlive()) entity.Remove(Ecs.IsA, GiantOrb);
        if (ScreenCleave.IsAlive()) entity.Remove(Ecs.IsA, ScreenCleave);

        entity.IsA(skillPrefab);
    }
}
