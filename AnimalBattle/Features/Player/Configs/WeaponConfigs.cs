namespace AnimalBattle.Configs;

/// <summary>
///     全武器設定の基底インターフェース。
///     武器機構の種別識別子を提供します。
/// </summary>
public interface IWeaponConfig
{
    string Type { get; }
}

/// <summary>
///     遠距離投射武器の物理および数値設定基底クラス。
/// </summary>
public class RangedWeaponConfig : IWeaponConfig
{
    public string Type { get; set; } = string.Empty;
    public float Cooldown { get; set; }
    public float BulletSpeed { get; set; }
    public Vec2f BulletSpawnOffset { get; set; }
    public string BulletHitboxShape { get; set; } = string.Empty;
    public Vec2f BulletSize { get; set; }
    public float BulletRadius { get; set; }
    public Vec2f BulletHitboxOffset { get; set; }
    public float BulletDamage { get; set; }
    public float ShootRecoilImpulse { get; set; }
    public Vec2f ShootRecoilStretch { get; set; }
    public float MuzzleFlashDuration { get; set; }
    public float MuzzleFlashRadius { get; set; }
}

/// <summary>
///     Cat 固有遠距離武器設定。
///     CatWeapon.toml からバインドされ、ワールド単例として管理されます。
/// </summary>
public sealed class CatWeaponConfig : RangedWeaponConfig
{
}

/// <summary>
///     近接判定展開武器の物理および数値設定基底クラス。
/// </summary>
public class MeleeWeaponConfig : IWeaponConfig
{
    public string Type { get; set; } = string.Empty;
    public float Damage { get; set; }
    public float Cooldown { get; set; }
    public float Range { get; set; }
    public Vec2f Size { get; set; }
    public float Duration { get; set; }
    public float LungeImpulse { get; set; }
    public Vec2f StretchScale { get; set; }
}

/// <summary>
///     Bunny 固有近接武器設定。
///     BunnyWeapon.toml からバインドされ、ワールド単例として管理されます。
/// </summary>
public sealed class BunnyWeaponConfig : MeleeWeaponConfig
{
}
