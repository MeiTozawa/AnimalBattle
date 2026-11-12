namespace AnimalBattle.Configs;

/// <summary>
///     全武器設定の基底インターフェース。
///     武器機構の種別識別子を提供します。
/// </summary>
public interface IWeaponConfig
{
    /// <summary>武器種別識別子</summary>
    string Type { get; }
}

/// <summary>
///     遠距離投射武器の物理および数値設定基底クラス。
/// </summary>
public class RangedWeaponConfig : IWeaponConfig
{
    /// <summary>武器種別識別子</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>射撃クールダウン（秒）</summary>
    public float Cooldown { get; set; }

    /// <summary>弾丸初速：px/s</summary>
    public float BulletSpeed { get; set; }

    /// <summary>弾丸発射位置オフセット</summary>
    public Vec2f BulletSpawnOffset { get; set; }

    /// <summary>弾丸判定サイズ</summary>
    public Vec2f BulletSize { get; set; }

    /// <summary>弾丸単発ダメージ</summary>
    public float BulletDamage { get; set; }

    /// <summary>射撃時後退力積インパルス</summary>
    public float ShootRecoilImpulse { get; set; }

    /// <summary>射撃時変形ストレッチ率</summary>
    public Vec2f ShootRecoilStretch { get; set; }

    /// <summary>銃口発光エフェクト持続時間（秒）</summary>
    public float MuzzleFlashDuration { get; set; }

    /// <summary>銃口発光エフェクト半径</summary>
    public float MuzzleFlashRadius { get; set; }

    /// <summary>弾丸命中撃砕破片の最小飛散個数</summary>
    public int ImpactSplatterMinCount { get; set; }

    /// <summary>弾丸命中撃砕破片の最大飛散個数</summary>
    public int ImpactSplatterMaxCount { get; set; }

    /// <summary>弾丸命中撃砕破片の最小初速：px/s</summary>
    public float ImpactSplatterMinSpeed { get; set; }

    /// <summary>弾丸命中撃砕破片の最大初速：px/s</summary>
    public float ImpactSplatterMaxSpeed { get; set; }

    /// <summary>弾丸命中撃砕破片粒子の半径ピクセル単位</summary>
    public float ImpactSplatterParticleRadius { get; set; }

    /// <summary>弾丸命中撃砕破片のフェード持続時間秒単位</summary>
    public float ImpactSplatterDuration { get; set; }
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
    /// <summary>武器種別識別子</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>攻撃判定単発ダメージ</summary>
    public float Damage { get; set; }

    /// <summary>近接攻撃クールダウン（秒）</summary>
    public float Cooldown { get; set; }

    /// <summary>攻撃射程距離</summary>
    public float Range { get; set; }

    /// <summary>攻撃判定矩形サイズ</summary>
    public Vec2f Size { get; set; }

    /// <summary>攻撃判定持続時間（秒）</summary>
    public float Duration { get; set; }

    /// <summary>踏み込み推進力積インパルス</summary>
    public float LungeImpulse { get; set; }

    /// <summary>攻撃時変形ストレッチ率</summary>
    public Vec2f StretchScale { get; set; }
}

/// <summary>
///     Bunny 固有近接武器設定。
///     BunnyWeapon.toml からバインドされ、ワールド単例として管理されます。
/// </summary>
public sealed class BunnyWeaponConfig : MeleeWeaponConfig
{
}
