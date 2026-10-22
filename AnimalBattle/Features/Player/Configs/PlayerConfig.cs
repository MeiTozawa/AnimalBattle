using AnimalBattle.Core;
using AnimalBattle.Core.Geometry;
using ErrorOr;

namespace AnimalBattle.Configs;

/// <summary>
///     Config/player.toml から読み込まれるプレイヤー動作・物理・判定パラメータの設定クラス。
///     形状種別、寸法、および局所オフセットを完全管理します。
/// </summary>
public class PlayerConfig : IConfigLifecycle {
    /// <summary>プレイヤーの最大 HP</summary>
    public float MaxHp { get; set; }

    /// <summary>被弾後の無敵時間秒単位</summary>
    public float InvincibleDuration { get; set; }

    /// <summary>被弾時ホワイトフラッシュ持続時間秒単位</summary>
    public float HitFlashDuration { get; set; }

    /// <summary>プレイヤーの左右移動速度：ピクセル/秒基準</summary>
    public float MoveSpeed { get; set; }

    /// <summary>移動・歩行アニメーション切り替え判定の速度閾値ピクセル毎秒単位</summary>
    public float WalkAnimVelocityThreshold { get; set; }

    /// <summary>地上初段ジャンプの初速度：上向き負値</summary>
    public float FirstJumpVelocity { get; set; }

    /// <summary>空中二段ジャンプの初速度：上向き負値</summary>
    public float SecondJumpVelocity { get; set; }

    /// <summary>下向き重力加速度</summary>
    public float Gravity { get; set; }

    /// <summary>最大落下終端速度</summary>
    public float MaxFallSpeed { get; set; }

    /// <summary>最大跳躍可能回数：空中二段ジャンプ</summary>
    public int MaxJumps { get; set; }

    /// <summary>プレイヤー被弾判定 の幾何形状種別："Circle" または "Box"</summary>
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

    /// <summary>手銃射撃のクールダウン間隔秒単位</summary>
    public float ShootCooldown { get; set; }

    /// <summary>手銃弾の飛行速度</summary>
    public float BulletSpeed { get; set; }

    /// <summary>銃弾生成時のプレイヤー中心からのオフセット</summary>
    public Vec2f BulletSpawnOffset { get; set; }

    /// <summary>プレイヤー銃弾攻撃判定 の幾何形状種別："Box" または "Circle"</summary>
    public string BulletHitboxShape { get; set; } = string.Empty;

    /// <summary>銃弾当たり判定サイズ：BulletHitboxShape == "Box" 時に使用</summary>
    public Vec2f BulletSize { get; set; }

    /// <summary>銃弾当たり判定円半径：BulletHitboxShape == "Circle" 時に使用</summary>
    public float BulletRadius { get; set; }

    /// <summary>銃弾判定の局所オフセット</summary>
    public Vec2f BulletHitboxOffset { get; set; }

    /// <summary>銃弾 1 発あたりの与ダメージ量</summary>
    public float BulletDamage { get; set; }

    /// <summary>近接攻撃力：Bunny</summary>
    public float AttackDamage { get; set; }

    /// <summary>近接攻撃クールダウン間隔秒単位</summary>
    public float AttackCooldown { get; set; }

    /// <summary>近接攻撃前方出現オフセット：px</summary>
    public float AttackRange { get; set; }

    /// <summary>近接攻撃判定サイズ：px</summary>
    public Vec2f AttackSize { get; set; }

    /// <summary>近接攻撃持続時間秒単位</summary>
    public float AttackDuration { get; set; }

    /// <summary>近接攻撃時の前進インパルス初速度：px/s</summary>
    public float AttackLungeImpulse { get; set; }

    /// <summary>近接攻撃時のスプリング変形倍率</summary>
    public Vec2f AttackStretchScale { get; set; }

    /// <summary>射撃出膛時の自機微後退反動インパルス初速度：px/s</summary>
    public float ShootRecoilImpulse { get; set; }

    /// <summary>射撃反動時のスプリング倍率</summary>
    public Vec2f ShootRecoilStretch { get; set; }

    /// <summary>銃口閃光：マズルフラッシュ持続時間秒単位</summary>
    public float MuzzleFlashDuration { get; set; }

    /// <summary>銃口閃光：マズルフラッシュ初期半径ピクセル単位</summary>
    public float MuzzleFlashRadius { get; set; }

    /// <summary>弾丸命中撃砕破片の最小飛散個数</summary>
    public int ImpactSplatterMinCount { get; set; }

    /// <summary>弾丸命中撃砕破片の最大飛散個数</summary>
    public int ImpactSplatterMaxCount { get; set; }

    /// <summary>弾丸命中撃砕破片の最小初速：px/s</summary>
    public float ImpactSplatterMinSpeed { get; set; }

    /// <summary>弾丸命中撃砕破片の最大初速：px/s</summary>
    public float ImpactSplatterMaxSpeed { get; set; }

    /// <summary>弾丸命中撃砕破片粒子の半径ピクセル単位</summary>
    public float ImpactSplatterParticleRadius { get; set; }

    /// <summary>弾丸命中撃砕破片のフェード持続時間秒単位</summary>
    public float ImpactSplatterDuration { get; set; }

    /// <summary>隙あり：Vulnerable / 易傷状態の Boss に対する与ダメージ倍率</summary>
    public float VulnerableDamageMultiplier { get; set; }

    /// <summary>先行入力猶予時間秒単位</summary>
    public float JumpBufferDuration { get; set; }

    /// <summary>コヨーテタイム猶予時間秒単位</summary>
    public float CoyoteTimeDuration { get; set; }

    /// <summary>ジャンプ中断時の上昇速度減衰率</summary>
    public float JumpCutDamping { get; set; }

    /// <summary>頂点滞空判定の速度閾値：px/s</summary>
    public float ApexFloatVelocityThreshold { get; set; }

    /// <summary>頂点滞空時の重力倍率</summary>
    public float ApexFloatGravityMultiplier { get; set; }

    /// <summary>ダッシュ突進速度：px/s</summary>
    public float DashSpeed { get; set; }

    /// <summary>ダッシュ持続時間秒単位</summary>
    public float DashDuration { get; set; }

    /// <summary>ダッシュ再使用クールダウン時間秒単位</summary>
    public float DashCooldown { get; set; }

    /// <summary>ダッシュによる無敵時間秒単位</summary>
    public float DashInvincibleDuration { get; set; }

    /// <summary>ダッシュ開始時のストレッチ変形倍率</summary>
    public Vec2f DashStretchScale { get; set; }

    /// <summary>ダッシュ中の残像生成間隔秒単位</summary>
    public float DashGhostTrailInterval { get; set; }

    /// <summary>ダッシュ残像のフェード持続時間秒単位</summary>
    public float DashGhostTrailDuration { get; set; }

    /// <summary>ダッシュ残像の初期アルファ値 0から255の範囲</summary>
    public float DashGhostTrailAlpha { get; set; }

    /// <summary>二段ジャンプ空気リングエフェクトの初期半径 ピクセル単位</summary>
    public float JumpRingInitialRadius { get; set; }

    /// <summary>二段ジャンプ空気リングエフェクトの最大拡散半径 ピクセル単位</summary>
    public float JumpRingMaxRadius { get; set; }

    /// <summary>二段ジャンプ空気リングエフェクトのフェード持続時間秒単位</summary>
    public float JumpRingDuration { get; set; }

    /// <summary>地面初速加速度：px/s²</summary>
    public float GroundAcceleration { get; set; }

    /// <summary>地面自然減速加速度：px/s²</summary>
    public float GroundDeceleration { get; set; }

    /// <summary>地面反転制動加速度：px/s²</summary>
    public float TurnaroundDeceleration { get; set; }

    /// <summary>空中移動加速度：px/s²</summary>
    public float AirAcceleration { get; set; }

    /// <summary>空中自然減速加速度：px/s²</summary>
    public float AirDeceleration { get; set; }

    /// <summary>通常ヒットストップ時間秒単位</summary>
    public float HitstopLightDuration { get; set; }

    /// <summary>強攻撃・弱点ヒットストップ時間秒単位</summary>
    public float HitstopHeavyDuration { get; set; }

    /// <summary>通常ヒット時のスプライト振動振幅ピクセル単位</summary>
    public float HitJitterLightAmplitude { get; set; }

    /// <summary>强攻撃・弱点ヒット時のスプライト振動振幅ピクセル単位</summary>
    public float HitJitterHeavyAmplitude { get; set; }

    /// <summary>ヒット時スプライト局所振動周波数：Hz</summary>
    public float HitJitterFrequency { get; set; }

    /// <summary>スプリング固有振動数：Hz</summary>
    public float SpringFrequency { get; set; }

    /// <summary>スプリング減衰比</summary>
    public float SpringDampingRatio { get; set; }

    /// <summary>跳躍開始時ストレッチ拡大率</summary>
    public Vec2f JumpStretchScale { get; set; }

    /// <summary>着地時スカッシュ拡大率</summary>
    public Vec2f LandSquashScale { get; set; }

    /// <summary>着地時スカッシュ演出を発火させる最小下落速度閾値：px/s</summary>
    public float LandSquashVelocityThreshold { get; set; }

    /// <summary>被弾衝撃時スカッシュ拡大率</summary>
    public Vec2f HitSquashScale { get; set; }

    /// <inheritdoc />
    public virtual ErrorOr<Success> Validate()
    {
        if (MaxHp <= 0)
            return Error.Validation("PlayerConfig.InvalidMaxHp", "プレイヤー最大 HP は 1 以上である必要があります。");

        if (MoveSpeed < 0.0f)
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

    /// <summary>
    ///     設定に基づいたプレイヤー銃弾攻撃判定 <see cref="CollisionShape" /> を取得します。
    /// </summary>
    /// <returns>構築された銃弾攻撃衝突形状バリアント</returns>
    public CollisionShape GetBulletHitboxShape()
    {
        return CollisionShapeFactory.CreateWithFallback(BulletHitboxShape, BulletRadius, BulletSize, BulletHitboxOffset);
    }
}