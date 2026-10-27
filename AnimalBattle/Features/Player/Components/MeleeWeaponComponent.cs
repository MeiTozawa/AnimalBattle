namespace AnimalBattle.Player.Components;

/// <summary>
///     近接判定展開武器機構コンポーネント。
///     キャラクター固有の命名を廃止し、近接斬撃・踏み込み・判定持続の機構を一般化します。
/// </summary>
public struct MeleeWeaponComponent
{
    /// <summary>近接攻撃クールダウンの基本持続秒数。</summary>
    public float Cooldown;

    /// <summary>近接攻撃クールダウンの残り秒数。</summary>
    public float Timer;

    /// <summary>近接打撃の攻撃力。</summary>
    public float Damage;

    /// <summary>近接打撃出現の水平オフセット射程距離：px。</summary>
    public float Range;

    /// <summary>近接打撃攻撃判定のサイズ Vec2f型。</summary>
    public Vec2f HitboxSize;

    /// <summary>近接打撃判定持続時間秒単位。</summary>
    public float Duration;

    /// <summary>近接打撃時の前進突進インパルス初速度：px/s。</summary>
    public float LungeImpulse;

    /// <summary>近接打撃時のスプリング変形倍率 Vec2f型。</summary>
    public Vec2f StretchScale;

    /// <summary>近接攻撃判定の残り有効秒数。0 より大きい場合は判定がアクティブ。</summary>
    public float ActiveRemaining;

    public static MeleeWeaponComponent FromConfig(MeleeWeaponConfig cfg)
        => FromConfig(cfg, 0.0f);

    public static MeleeWeaponComponent FromConfig(MeleeWeaponConfig cfg, float currentTimer)
    {
        return new MeleeWeaponComponent
        {
            Cooldown = cfg.Cooldown,
            Timer = currentTimer,
            Damage = cfg.Damage,
            Range = cfg.Range,
            HitboxSize = cfg.Size,
            Duration = cfg.Duration,
            LungeImpulse = cfg.LungeImpulse,
            StretchScale = cfg.StretchScale,
            ActiveRemaining = 0.0f
        };
    }
}
