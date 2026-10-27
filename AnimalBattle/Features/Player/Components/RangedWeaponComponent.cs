using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Player.Components;

/// <summary>
///     遠距離投射武器機構コンポーネント。
///     キャラクター固有の命名を廃止し、射撃・弾幕生成・反動物理の機構を一般化します。
/// </summary>
public struct RangedWeaponComponent
{
    /// <summary>射撃クールダウンの基本持続秒数。</summary>
    public float Cooldown;

    /// <summary>射撃クールダウンの残り秒数。</summary>
    public float Timer;

    /// <summary>弾丸の飛翔速度：px/s。</summary>
    public float BulletSpeed;

    /// <summary>弾丸攻撃力。</summary>
    public float BulletDamage;

    /// <summary>自機射撃微後退反動インパルス初速度：px/s。</summary>
    public float RecoilImpulse;

    /// <summary>発射口の局所オフセット Vec2f型。</summary>
    public Vec2f SpawnOffset;

    /// <summary>弾丸衝突判定のサイズ Vec2f型。</summary>
    public Vec2f BulletSize;

    /// <summary>自機射撃時スプリング変形倍率 Vec2f型。</summary>
    public Vec2f RecoilStretch;

    /// <summary>銃口閃光：マズルフラッシュ持続時間秒単位。</summary>
    public float MuzzleFlashDuration;

    /// <summary>銃口閃光：マズルフラッシュ初期半径ピクセル単位。</summary>
    public float MuzzleFlashRadius;

    public static RangedWeaponComponent FromConfig(RangedWeaponConfig cfg)
        => FromConfig(cfg, 0.0f);

    public static RangedWeaponComponent FromConfig(RangedWeaponConfig cfg, float currentTimer)
    {
        return new RangedWeaponComponent
        {
            Cooldown = cfg.Cooldown,
            Timer = currentTimer,
            BulletSpeed = cfg.BulletSpeed,
            BulletDamage = cfg.BulletDamage,
            RecoilImpulse = cfg.ShootRecoilImpulse,
            SpawnOffset = cfg.BulletSpawnOffset,
            BulletSize = cfg.BulletSize,
            RecoilStretch = cfg.ShootRecoilStretch,
            MuzzleFlashDuration = cfg.MuzzleFlashDuration,
            MuzzleFlashRadius = cfg.MuzzleFlashRadius
        };
    }
}
