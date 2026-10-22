using AnimalBattle.Core;
using AnimalBattle.Core.Geometry;

#nullable disable

namespace AnimalBattle.Configs;

/// <summary>
///     多態 Boss の基本幾何形状・物理パラメータ・当たり判定生成を一元管理する共通基底設定クラス。
/// </summary>
public abstract class BaseBossConfig {
    /// <summary>Boss 表示名</summary>
    public string BossName { get; set; }

    /// <summary>Boss の最大 HP</summary>
    public float MaxHp { get; set; }

    /// <summary>被弾時ホワイトフラッシュ持続時間（秒）</summary>
    public float HitFlashDuration { get; set; }

    /// <summary>Boss の出現座標</summary>
    public Vec2f Spawn { get; set; }

    /// <summary>Boss 描画スケール</summary>
    public Vec2f Scale { get; set; }

    /// <summary>Boss 地形・幾何衝突判定サイズ（ピクセル）</summary>
    public Vec2f CollisionSize { get; set; }

    /// <summary>Boss 被弾判定 (Hurtbox) の幾何形状種別（"Box" または "Circle"）</summary>
    public string HurtboxShape { get; set; }

    /// <summary>Boss 被弾当たり判定サイズ（HurtboxShape == "Box" 時に使用）</summary>
    public Vec2f HurtboxSize { get; set; }

    /// <summary>Boss 被弾当たり判定円半径（HurtboxShape == "Circle" 時に使用）</summary>
    public float HurtboxRadius { get; set; }

    /// <summary>Boss 被弾判定の局所オフセット</summary>
    public Vec2f HurtboxOffset { get; set; }

    /// <summary>Boss 身体接触・攻撃判定 (Hitbox) の幾何形状種別（"Box" または "Circle"）</summary>
    public string HitboxShape { get; set; }

    /// <summary>Boss 接触攻撃判定サイズ（HitboxShape == "Box" 時に使用）</summary>
    public Vec2f BodyHitboxSize { get; set; }

    /// <summary>Boss 接触攻撃判定円半径（HitboxShape == "Circle" 時に使用）</summary>
    public float BodyHitboxRadius { get; set; }

    /// <summary>Boss 接触判定の局所オフセット</summary>
    public Vec2f BodyHitboxOffset { get; set; }

    /// <summary>Boss 身体接触・突進命中時の与ダメージ量</summary>
    public float BodyContactDamage { get; set; }

    /// <summary>
    ///     設定に基づいた Boss 被弾判定 (<see cref="CollisionShape" />) を取得します。
    /// </summary>
    /// <returns>構築された被弾衝突形状バリアント</returns>
    public virtual CollisionShape GetHurtboxShape()
    {
        return CollisionShapeFactory.CreateWithFallback(HurtboxShape, HurtboxRadius, HurtboxSize, HurtboxOffset);
    }

    /// <summary>
    ///     設定に基づいた Boss 接触・突進攻撃判定 (<see cref="CollisionShape" />) を取得します。
    /// </summary>
    /// <returns>構築された接触攻撃衝突形状バリアント</returns>
    public virtual CollisionShape GetHitboxShape()
    {
        return CollisionShapeFactory.CreateWithFallback(HitboxShape, BodyHitboxRadius, BodyHitboxSize, BodyHitboxOffset);
    }
}
