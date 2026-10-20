using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Player.Components;

/// <summary>
///     Cat (猫) 遠隔射撃武器状態コンポーネント。
///     クールダウン、弾速、弾丸攻撃力、発射口オフセットおよび射撃反動パラメータを保持します。
/// </summary>
public struct CatWeaponState
{
    /// <summary>射撃クールダウンの残り秒数。</summary>
    public float CooldownRemaining;

    /// <summary>射撃クールダウンの基本持続秒数。</summary>
    public float CooldownDuration;

    /// <summary>弾丸の飛翔速度（px/s）。</summary>
    public float BulletSpeed;

    /// <summary>弾丸攻撃力。</summary>
    public float BulletDamage;

    /// <summary>弾丸衝突判定のサイズ (Vec2f)。</summary>
    public Vec2f BulletSize;

    /// <summary>発射口の局所オフセット (Vec2f)。</summary>
    public Vec2f BulletSpawnOffset;

    /// <summary>自機射撃微後退反動距離（px）。</summary>
    public float ShootRecoilDistance;

    /// <summary>自機射撃時スプリング変形倍率 (Vec2f)。</summary>
    public Vec2f ShootRecoilStretch;

    /// <summary>
    ///     cat.toml から CatWeaponState を構築します。
    /// </summary>
    /// <param name="cfg">Cat のプレイヤー設定。</param>
    /// <returns>初期化された CatWeaponState。</returns>
    public static CatWeaponState FromConfig(PlayerConfig cfg)
    {
        System.Diagnostics.Debug.Assert(cfg != null, "PlayerConfig は null であってはなりません。");
        return new CatWeaponState
        {
            CooldownRemaining = 0.0f,
            CooldownDuration = cfg.ShootCooldown,
            BulletSpeed = cfg.BulletSpeed,
            BulletDamage = cfg.BulletDamage,
            BulletSize = cfg.BulletSize,
            BulletSpawnOffset = cfg.BulletSpawnOffset,
            ShootRecoilDistance = cfg.ShootRecoilDistance,
            ShootRecoilStretch = cfg.ShootRecoilStretch
        };
    }
}
