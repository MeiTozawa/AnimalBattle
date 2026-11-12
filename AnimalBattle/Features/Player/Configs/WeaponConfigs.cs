using System.Drawing;
using Microsoft.Extensions.Configuration;

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

    private Color _specialOrbFillColor;
    private Color _specialOrbOutlineColor;

    /// <summary>巨型エネルギー弾の塗りつぶし色 Hex 文字列</summary>
    [ConfigurationKeyName("SpecialOrbFillColor")]
    public string SpecialOrbFillColorHex
    {
        get => string.Empty;
        set => _specialOrbFillColor = value.ToColor();
    }

    /// <summary>巨型エネルギー弾の塗りつぶし色</summary>
    public Color SpecialOrbFillColor => _specialOrbFillColor;

    /// <summary>巨型エネルギー弾の外枠色 Hex 文字列</summary>
    [ConfigurationKeyName("SpecialOrbOutlineColor")]
    public string SpecialOrbOutlineColorHex
    {
        get => string.Empty;
        set => _specialOrbOutlineColor = value.ToColor();
    }

    /// <summary>巨型エネルギー弾の外枠色</summary>
    public Color SpecialOrbOutlineColor => _specialOrbOutlineColor;
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

    /// <summary>全画面斬撃による全敵対ユニットへの期待総確定ダメージ</summary>
    public float SpecialCleaveDamage { get; set; }

    /// <summary>全画面斬撃の継続ダメージ発生間隔（秒）</summary>
    public float SpecialCleaveTickInterval { get; set; }

    /// <summary>全画面斬撃発動時の自機無敵時間（秒）</summary>
    public float SpecialCleaveInvincibleDuration { get; set; }

    /// <summary>全画面斬撃時のカメラ衝撃トラウマ値</summary>
    public float SpecialCleaveTrauma { get; set; }

    /// <summary>全画面斬撃エフェクトの発生総数</summary>
    public int SpecialCleaveSlashCount { get; set; }

    /// <summary>全画面斬撃シーケンスの総演出時間（秒）</summary>
    public float SpecialCleaveDuration { get; set; }

    /// <summary>単一斬撃エフェクトの生存時間（秒）</summary>
    public float SpecialCleaveSlashLifetime { get; set; }

    /// <summary>斬撃エフェクト最小スケール</summary>
    public float SpecialCleaveSlashScaleMin { get; set; }

    /// <summary>斬撃エフェクト最大スケール</summary>
    public float SpecialCleaveSlashScaleMax { get; set; }

    /// <summary>斬撃エフェクトの推進速度（px/s）</summary>
    public float SpecialCleaveSlashSpeed { get; set; }

    private Color _specialCleaveSlashColor;

    /// <summary>斬撃エフェクトの表示色 Hex 文字列</summary>
    [ConfigurationKeyName("SpecialCleaveSlashColor")]
    public string SpecialCleaveSlashColorHex
    {
        get => string.Empty;
        set => _specialCleaveSlashColor = value.ToColor();
    }

    /// <summary>斬撃エフェクトの表示色</summary>
    public Color SpecialCleaveSlashColor => _specialCleaveSlashColor;
}
