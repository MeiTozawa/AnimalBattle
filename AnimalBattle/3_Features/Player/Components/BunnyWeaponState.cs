using AnimalBattle.Configs;

namespace AnimalBattle.Components;

/// <summary>
///     Bunny (ウサギ) 近接攻撃武器状態コンポーネント。
///     クールダウン、近接打撃攻撃力、判定寸法、射程、持続時間、前進突進距離およびスプリング伸縮演出パラメータを保持します。
/// </summary>
public struct BunnyWeaponState
{
    /// <summary>近接攻撃クールダウンの残り秒数。</summary>
    public float CooldownRemaining;

    /// <summary>近接攻撃クールダウンの基本持続秒数。</summary>
    public float CooldownDuration;

    /// <summary>近接打撃の攻撃力。</summary>
    public float AttackDamage;

    /// <summary>近接打撃攻撃判定の横幅（px）。</summary>
    public float AttackWidth;

    /// <summary>近接打撃攻撃判定の縦幅（px）。</summary>
    public float AttackHeight;

    /// <summary>近接打撃出現の水平オフセット射程距離（px）。</summary>
    public float AttackRange;

    /// <summary>近接打撃判定持続時間（秒）。</summary>
    public float AttackDuration;

    /// <summary>近接打撃時の前進突進距離（px）。</summary>
    public float AttackLungeDistance;

    /// <summary>近接打撃時のスプリング変形倍率 X。</summary>
    public float AttackStretchScaleX;

    /// <summary>近接打撃時のスプリング変形倍率 Y。</summary>
    public float AttackStretchScaleY;

    /// <summary>近接攻撃判定の残り有効秒数。0 より大きい場合は判定がアクティブ。</summary>
    public float AttackActiveRemaining;

    /// <summary>
    ///     bunny.toml から BunnyWeaponState を構築します。
    /// </summary>
    /// <param name="cfg">Bunny のプレイヤー設定。</param>
    /// <returns>初期化された BunnyWeaponState。</returns>
    public static BunnyWeaponState FromConfig(PlayerConfig cfg)
    {
        System.Diagnostics.Debug.Assert(cfg != null, "PlayerConfig は null であってはなりません。");
        return new BunnyWeaponState
        {
            CooldownRemaining = 0.0f,
            CooldownDuration = cfg.AttackCooldown,
            AttackDamage = cfg.AttackDamage,
            AttackWidth = cfg.AttackWidth,
            AttackHeight = cfg.AttackHeight,
            AttackRange = cfg.AttackRange,
            AttackDuration = cfg.AttackDuration,
            AttackLungeDistance = cfg.AttackLungeDistance,
            AttackStretchScaleX = cfg.AttackStretchScaleX,
            AttackStretchScaleY = cfg.AttackStretchScaleY
        };
    }
}
