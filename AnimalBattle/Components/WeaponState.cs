using AnimalBattle.Configs;

namespace AnimalBattle.Components;

/// <summary>
///     キャラクターの射撃武器状態コンポーネント。
///     射撃クールダウンおよび生成する弾丸パラメータを保持します。
/// </summary>
public struct WeaponState
{
    /// <summary>射撃クールダウンの残り秒数。</summary>
    public float CooldownRemaining;

    /// <summary>射撃クールダウンの基本持続秒数。</summary>
    public float CooldownDuration;

    /// <summary>弾丸の飛翔速度（px/s）。</summary>
    public float BulletSpeed;

    /// <summary>弾丸の攻撃力。</summary>
    public float BulletDamage;

    /// <summary>弾丸の横幅（px）。</summary>
    public float BulletWidth;

    /// <summary>弾丸の縦幅（px）。</summary>
    public float BulletHeight;

    /// <summary>発射口の水平オフセット距離（px）。</summary>
    public float BulletSpawnOffset;

    /// <summary>発射口の垂直オフセット距離（px）。</summary>
    public float BulletSpawnOffsetY;

    /// <summary>
    ///     プレイヤー設定（PlayerConfig）から武器状態コンポーネントを構築します。
    /// </summary>
    /// <param name="cfg">プレイヤー設定。</param>
    /// <returns>構築された WeaponState インスタンス。</returns>
    public static WeaponState FromPlayerConfig(PlayerConfig cfg)
    {
        System.Diagnostics.Debug.Assert(cfg != null, "PlayerConfig は null であってはなりません。");
        return new WeaponState
        {
            CooldownRemaining = 0.0f,
            CooldownDuration = cfg.ShootCooldown,
            BulletSpeed = cfg.BulletSpeed,
            BulletDamage = cfg.BulletDamage,
            BulletWidth = cfg.BulletWidth,
            BulletHeight = cfg.BulletHeight,
            BulletSpawnOffset = cfg.BulletSpawnOffset,
            BulletSpawnOffsetY = cfg.BulletSpawnOffsetY
        };
    }
}
