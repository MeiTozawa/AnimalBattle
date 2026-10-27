using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Player.Components;

/// <summary>
///     Bunny近接攻撃状態コンポーネント。
///     近接クールダウン、攻撃力、判定サイズ、持続時間、突進インパルスおよびスプリング変形を保持します。
/// </summary>
public struct BunnyWeaponState
{
    public float CooldownRemaining;
    public float CooldownDuration;
    public float AttackDamage;
    public Vec2f AttackSize;
    public float AttackRange;
    public float AttackDuration;
    public float AttackLungeImpulse;
    public Vec2f AttackStretchScale;
    public float AttackActiveRemaining;

    public static BunnyWeaponState FromConfig(MeleeWeaponConfig cfg)
    {
        return new BunnyWeaponState
        {
            CooldownRemaining = 0.0f,
            CooldownDuration = cfg.Cooldown,
            AttackDamage = cfg.Damage,
            AttackSize = cfg.Size,
            AttackRange = cfg.Range,
            AttackDuration = cfg.Duration,
            AttackLungeImpulse = cfg.LungeImpulse,
            AttackStretchScale = cfg.StretchScale,
            AttackActiveRemaining = 0.0f
        };
    }
}
