using ErrorOr;

namespace AnimalBattle.Configs;

/// <summary>
///     Config/player.toml から読み込まれるプレイヤー動作・物理・判定パラメータの設定クラス。
///     運動、跳躍、突進はサブ設定として集約・管理されます。
/// </summary>
public class PlayerConfig : IConfigLifecycle {
    /// <summary>プレイヤーの最大 HP</summary>
    public float MaxHp { get; set; }

    /// <summary>被弾後の無敵時間秒単位</summary>
    public float InvincibleDuration { get; set; }

    /// <summary>被弾時ホワイトフラッシュ持続時間秒単位</summary>
    public float HitFlashDuration { get; set; }

    /// <summary>移動・運動キネマティクス設定</summary>
    public PlayerLocomotionConfig Movement { get; set; } = new();

    /// <summary>跳躍性能および跳躍演出設定</summary>
    public PlayerJumpConfig Jump { get; set; } = new();

    /// <summary>突進回避（ダッシュ）および残像設定（ダッシュ機能を持たない場合は null）</summary>
    public PlayerDashConfig? Dash { get; set; }

    /// <summary>プレイヤー被弾判定の幾何形状種別："Circle" または "Box"</summary>
    public string HurtboxShape { get; set; } = string.Empty;

    /// <summary>プレイヤー被弾判定円半径：HurtboxShape == "Circle" 時に使用</summary>
    public float HurtboxRadius { get; set; }

    /// <summary>プレイヤー被弾判定矩形サイズ：HurtboxShape == "Box" 時に使用</summary>
    public Vec2f HurtboxSize { get; set; }

    /// <summary>プレイヤー被弾判定の局所オフセットピクセル単位</summary>
    public Vec2f HurtboxOffset { get; set; }

    /// <summary>地形足場との衝突サイズピクセル単位</summary>
    public Vec2f CollisionSize { get; set; }

    /// <summary>プレイヤー描画スケール</summary>
    public Vec2f Scale { get; set; }

    /// <summary>初期出現およびリスポーン座標</summary>
    public Vec2f Spawn { get; set; }

    /// <summary>装備武器設定名（例: "CatWeapon", "BunnyWeapon"）</summary>
    public string Weapon { get; set; } = string.Empty;

    /// <summary>スプリング固有振動数：Hz</summary>
    public float SpringFrequency { get; set; }

    /// <summary>スプリング減衰比</summary>
    public float SpringDampingRatio { get; set; }

    /// <summary>被弾衝撃時スカッシュ拡大率</summary>
    public Vec2f HitSquashScale { get; set; }

    /// <summary>エネルギーゲージ最大スロット数</summary>
    public float EnergyMaxSegments { get; set; }

    /// <summary>エネルギーゲージ1スロットあたりの必要容量</summary>
    public float EnergySegmentCapacity { get; set; }

    /// <summary>与ダメージ1点あたりに獲得するエネルギー量</summary>
    public float EnergyPerDamageDealt { get; set; }

    /// <summary>被ダメージ1点あたりに獲得するエネルギー量</summary>
    public float EnergyPerDamageTaken { get; set; }

    /// <inheritdoc />
    public virtual ErrorOr<Success> Validate()
    {
        if (MaxHp <= 0)
            return Error.Validation("PlayerConfig.InvalidMaxHp", "プレイヤー最大 HP は 1 以上である必要があります。");

        if (Movement.MoveSpeed < 0.0f)
            return Error.Validation("PlayerConfig.InvalidMoveSpeed", "プレイヤー移動速度は 0 以上である必要があります。");

        if (HurtboxShape == "Circle" && HurtboxRadius <= 0.0f)
            return Error.Validation("PlayerConfig.InvalidHurtboxRadius", "円形被弾判定の半径は 0 より大きい必要があります。");

        if (HurtboxShape == "Box" && (HurtboxSize.X <= 0.0f || HurtboxSize.Y <= 0.0f))
            return Error.Validation("PlayerConfig.InvalidHurtboxBox", "矩形被弾判定の幅および高さは 0 より大きい必要があります。");

        return Result.Success;
    }

    /// <inheritdoc />
    public virtual void PostProcess()
    {
    }

    /// <summary>
    ///     設定に基づいたプレイヤー被弾判定 <see cref="CollisionShape" /> を取得します。
    /// </summary>
    /// <returns>構築された被弾衝突形状バリアント</returns>
    public CollisionShape GetHurtboxShape()
    {
        return CollisionShapeFactory.CreateWithFallback(HurtboxShape, HurtboxRadius, HurtboxSize, HurtboxOffset);
    }
}
