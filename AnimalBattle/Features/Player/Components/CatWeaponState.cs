using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Player.Components;

/// <summary>
///     Cat遠隔射撃武器状態コンポーネント。
///     クールダウン、弾速、弾丸攻撃力、発射口オフセットおよび射撃反動パラメータを保持します。
/// </summary>
public struct CatWeaponState
{
    public float CooldownRemaining;
    public float CooldownDuration;
    public float BulletSpeed;
    public float BulletDamage;
    public Vec2f BulletSize;
    public Vec2f BulletSpawnOffset;
    public float ShootRecoilImpulse;
    public Vec2f ShootRecoilStretch;
    public float MuzzleFlashDuration;
    public float MuzzleFlashRadius;

    public static CatWeaponState FromConfig(RangedWeaponConfig cfg)
    {
        return new CatWeaponState
        {
            CooldownRemaining = 0.0f,
            CooldownDuration = cfg.Cooldown,
            BulletSpeed = cfg.BulletSpeed,
            BulletDamage = cfg.BulletDamage,
            BulletSize = cfg.BulletSize,
            BulletSpawnOffset = cfg.BulletSpawnOffset,
            ShootRecoilImpulse = cfg.ShootRecoilImpulse,
            ShootRecoilStretch = cfg.ShootRecoilStretch,
            MuzzleFlashDuration = cfg.MuzzleFlashDuration,
            MuzzleFlashRadius = cfg.MuzzleFlashRadius
        };
    }
}
