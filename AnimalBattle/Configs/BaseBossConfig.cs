namespace AnimalBattle.Configs;

/// <summary>
///     多態 Boss の基本幾何形状・物理パラメータ・当たり判定生成を一元管理する共通基底設定クラス。
/// </summary>
public abstract class BaseBossConfig {
    /// <summary>Boss 表示名</summary>
    public string BossName { get; set; } = string.Empty;

    /// <summary>Boss の最大 HP</summary>
    public float MaxHp { get; set; }

    /// <summary>Boss の出現 X 座標</summary>
    public float SpawnX { get; set; } = 640.0f;

    /// <summary>Boss の出現 Y 座標</summary>
    public float SpawnY { get; set; } = 200.0f;

    /// <summary>Boss 描画スケール X</summary>
    public float ScaleX { get; set; }

    /// <summary>Boss 描画スケール Y</summary>
    public float ScaleY { get; set; }

    /// <summary>Boss 被弾判定 (Hurtbox) の幾何形状種別（"Box" または "Circle"）</summary>
    public string HurtboxShape { get; set; } = string.Empty;

    /// <summary>Boss 被弾当たり判定幅（HurtboxShape == "Box" 時に使用）</summary>
    public float HurtboxWidth { get; set; }

    /// <summary>Boss 被弾当たり判定高さ（HurtboxShape == "Box" 時に使用）</summary>
    public float HurtboxHeight { get; set; }

    /// <summary>Boss 被弾当たり判定円半径（HurtboxShape == "Circle" 時に使用）</summary>
    public float HurtboxRadius { get; set; }

    /// <summary>Boss 被弾判定の X 軸局所オフセット</summary>
    public float HurtboxOffsetX { get; set; }

    /// <summary>Boss 被弾判定の Y 軸局所オフセット</summary>
    public float HurtboxOffsetY { get; set; }

    /// <summary>Boss 身体接触・攻撃判定 (Hitbox) の幾何形状種別（"Box" または "Circle"）</summary>
    public string HitboxShape { get; set; } = string.Empty;

    /// <summary>Boss 接触攻撃判定幅（HitboxShape == "Box" 時に使用）</summary>
    public float BodyHitboxWidth { get; set; }

    /// <summary>Boss 接触攻撃判定高さ（HitboxShape == "Box" 時に使用）</summary>
    public float BodyHitboxHeight { get; set; }

    /// <summary>Boss 接触攻撃判定円半径（HitboxShape == "Circle" 時に使用）</summary>
    public float BodyHitboxRadius { get; set; }

    /// <summary>Boss 接触判定の X 軸局所オフセット</summary>
    public float BodyHitboxOffsetX { get; set; }

    /// <summary>Boss 接触判定の Y 軸局所オフセット</summary>
    public float BodyHitboxOffsetY { get; set; }

    /// <summary>Boss 身体接触・突進命中時の与ダメージ量</summary>
    public float BodyContactDamage { get; set; }

    /// <summary>
    ///     設定に基づいた Boss 被弾判定 (<see cref="CollisionShape" />) を取得します。
    /// </summary>
    /// <returns>構築された被弾衝突形状バリアント</returns>
    public virtual CollisionShape GetHurtboxShape()
    {
        return CollisionShapeFactory.CreateWithFallback(HurtboxShape, HurtboxRadius, HurtboxWidth, HurtboxHeight,
            HurtboxOffsetX, HurtboxOffsetY);
    }

    /// <summary>
    ///     設定に基づいた Boss 接触・突進攻撃判定 (<see cref="CollisionShape" />) を取得します。
    /// </summary>
    /// <returns>構築された接触攻撃衝突形状バリアント</returns>
    public virtual CollisionShape GetHitboxShape()
    {
        return CollisionShapeFactory.CreateWithFallback(HitboxShape, BodyHitboxRadius, BodyHitboxWidth,
            BodyHitboxHeight, BodyHitboxOffsetX, BodyHitboxOffsetY);
    }
}
