namespace AnimalBattle.Player;

using System.IO;

/// <summary>
///     武器 Prefabs プレハブの登録・管理およびプレイヤーエンティティへの排他バインド機構。
/// </summary>
public static class WeaponPrefabs
{
    public const string CatWeaponName = "CatWeaponPrefab";
    public const string BunnyWeaponName = "BunnyWeaponPrefab";

    public static Entity CatWeapon { get; private set; }
    public static Entity BunnyWeapon { get; private set; }

    /// <summary>
    ///     武器 Prefab をワールドへ登録します。
    /// </summary>
    public static void Register(World world, RangedWeaponConfig catWeaponCfg, MeleeWeaponConfig bunnyWeaponCfg)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");
        Debug.Assert(catWeaponCfg != null, "Cat の武器設定は null にできません。");
        Debug.Assert(bunnyWeaponCfg != null, "Bunny の武器設定は null にできません。");

        CatWeapon = world.Prefab(CatWeaponName)
            .Set(RangedWeaponComponent.FromConfig(catWeaponCfg));

        BunnyWeapon = world.Prefab(BunnyWeaponName)
            .Set(MeleeWeaponComponent.FromConfig(bunnyWeaponCfg));
    }

    /// <summary>
    ///     指定された武器名に対応する Prefab を取得します。
    /// </summary>
    public static Entity Get(string weaponName)
    {
        var normalized = weaponName.Trim();
        if (normalized.EndsWith(".toml", StringComparison.OrdinalIgnoreCase))
        {
            normalized = Path.GetFileNameWithoutExtension(normalized);
        }

        return normalized switch
        {
            "CatWeapon" => CatWeapon,
            "BunnyWeapon" => BunnyWeapon,
            _ => AssertHelper.FailWithDefault($"未知の武器です: {weaponName}", CatWeapon)
        };
    }

    /// <summary>
    ///     対象 Prefab へ武器 Prefab を排他的にアタッチ（IsA 継承）します。
    /// </summary>
    public static void Attach(Entity entity, string weaponName)
    {
        if (string.IsNullOrEmpty(weaponName)) return;

        var weaponPrefab = Get(weaponName);
        if (CatWeapon.IsAlive()) entity.Remove(Ecs.IsA, CatWeapon);
        if (BunnyWeapon.IsAlive()) entity.Remove(Ecs.IsA, BunnyWeapon);

        entity.IsA(weaponPrefab);
    }

    /// <summary>
    ///     生成されたプレイヤーインスタンスに必要な武器付随エンティティ（近接攻撃判定等）をセットアップします。
    /// </summary>
    public static void SetupInstance(World world, Entity playerEntity, string weaponName)
    {
        var weaponPrefab = Get(weaponName);
        if (weaponPrefab == BunnyWeapon)
        {
            var melee = world.Entity("PlayerSlashHitbox")
                .ChildOf(playerEntity)
                .IsA(CombatPrefabs.PlayerSlashHitbox);
            melee.Disable();
            playerEntity.Add<EquippedWeapon>(melee);
        }
    }
}
