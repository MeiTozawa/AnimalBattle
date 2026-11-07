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
    public Vec2f BulletSize { get; set; }
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
    /// <summary>特殊能力発動後の連続誤爆防止クールダウン（秒）</summary>
    public float SpecialAbilityCooldown { get; set; }

    /// <summary>巨型エネルギー弾の1秒あたりの期待接触ダメージ</summary>
    public float SpecialOrbDamagePerSecond { get; set; }

    /// <summary>巨型エネルギー弾の接触ダメージティック間隔（秒）</summary>
    public float SpecialOrbTickInterval { get; set; }

    /// <summary>巨型エネルギー弾の初速（px/s）</summary>
    public float SpecialOrbSpeed { get; set; }

    /// <summary>巨型エネルギー弾の接触判定半径（px）</summary>
    public float SpecialOrbRadius { get; set; }

    /// <summary>巨型エネルギー弾の最大生存時間（秒）</summary>
    public float SpecialOrbLifetime { get; set; }
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
    /// <summary>特殊能力発動後の連続誤爆防止クールダウン（秒）</summary>
    public float SpecialAbilityCooldown { get; set; }

    /// <summary>全画面斬撃による全敵対ユニットへの確定ダメージ</summary>
    public float SpecialCleaveDamage { get; set; }

    /// <summary>全画面斬撃発動時の自機無敵時間（秒）</summary>
    public float SpecialCleaveInvincibleDuration { get; set; }

    /// <summary>全画面斬撃時のカメラ衝撃トラウマ値</summary>
    public float SpecialCleaveTrauma { get; set; }
}
