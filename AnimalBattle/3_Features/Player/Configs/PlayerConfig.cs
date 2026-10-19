using ErrorOr;

namespace AnimalBattle.Configs;

/// <summary>
///     Config/player.toml から読み込まれるプレイヤー動作・物理・判定パラメータの設定クラス。
///     形状種別、寸法、および局所オフセット (HurtboxOffsetX/Y, BulletHitboxOffsetX/Y) を完全管理します。
/// </summary>
public class PlayerConfig : IConfigLifecycle {
    /// <summary>プレイヤーの最大 HP</summary>
    public int MaxHp { get; set; }

    /// <summary>被弾後の無敵時間（秒）</summary>
    public float InvincibleDuration { get; set; }

    /// <summary>被弾時ホワイトフラッシュ持続時間（秒）</summary>
    public float HitFlashDuration { get; set; }

    /// <summary>プレイヤーの左右移動速度（ピクセル/秒基準）</summary>
    public float MoveSpeed { get; set; }

    /// <summary>移動・歩行アニメーション切り替え判定の速度閾値（ピクセル/秒）</summary>
    public float WalkAnimVelocityThreshold { get; set; }

    /// <summary>地上初段ジャンプの初速度（上向き負値）</summary>
    public float FirstJumpVelocity { get; set; }

    /// <summary>空中二段ジャンプの初速度（上向き負値）</summary>
    public float SecondJumpVelocity { get; set; }

    /// <summary>下向き重力加速度</summary>
    public float Gravity { get; set; }

    /// <summary>最大落下終端速度</summary>
    public float MaxFallSpeed { get; set; }

    /// <summary>最大跳躍可能回数（空中二段ジャンプ）</summary>
    public int MaxJumps { get; set; }

    /// <summary>プレイヤー被弾判定 (Hurtbox) の幾何形状種別（"Circle" または "Box"）</summary>
    public string HurtboxShape { get; set; } = string.Empty;

    /// <summary>プレイヤー被弾判定円半径（HurtboxShape == "Circle" 時に使用）</summary>
    public float HurtboxRadius { get; set; }

    /// <summary>プレイヤー被弾判定矩形幅（HurtboxShape == "Box" 時に使用）</summary>
    public float HurtboxWidth { get; set; }

    /// <summary>プレイヤー被弾判定矩形高さ（HurtboxShape == "Box" 時に使用）</summary>
    public float HurtboxHeight { get; set; }

    /// <summary>プレイヤー被弾判定の X 軸局所オフセット（ピクセル）</summary>
    public float HurtboxOffsetX { get; set; }

    /// <summary>プレイヤー被弾判定の Y 軸局所オフセット（ピクセル）</summary>
    public float HurtboxOffsetY { get; set; }

    /// <summary>地形足場との衝突幅（ピクセル）</summary>
    public float CollisionWidth { get; set; }

    /// <summary>地形足場との衝突高さ（ピクセル）</summary>
    public float CollisionHeight { get; set; }

    /// <summary>プレイヤー描画スケール X</summary>
    public float ScaleX { get; set; }

    /// <summary>プレイヤー描画スケール Y</summary>
    public float ScaleY { get; set; }

    /// <summary>初期出現およびリスポーン X 座標</summary>
    public float SpawnX { get; set; }

    /// <summary>初期出現およびリスポーン Y 座標</summary>
    public float SpawnY { get; set; }

    /// <summary>手銃射撃のクールダウン間隔（秒）</summary>
    public float ShootCooldown { get; set; }

    /// <summary>手銃弾の飛行速度</summary>
    public float BulletSpeed { get; set; }

    /// <summary>銃弾生成時のプレイヤー中心からの X オフセット</summary>
    public float BulletSpawnOffset { get; set; }

    /// <summary>銃弾生成時のプレイヤー中心からの Y オフセット</summary>
    public float BulletSpawnOffsetY { get; set; }

    /// <summary>プレイヤー銃弾攻撃判定 (Hitbox) の幾何形状種別（"Box" または "Circle"）</summary>
    public string BulletHitboxShape { get; set; } = string.Empty;

    /// <summary>銃弾当たり判定幅（BulletHitboxShape == "Box" 時に使用）</summary>
    public float BulletWidth { get; set; }

    /// <summary>銃弾当たり判定高さ（BulletHitboxShape == "Box" 時に使用）</summary>
    public float BulletHeight { get; set; }

    /// <summary>銃弾当たり判定円半径（BulletHitboxShape == "Circle" 時に使用）</summary>
    public float BulletRadius { get; set; }

    /// <summary>銃弾判定の X 軸局所オフセット</summary>
    public float BulletHitboxOffsetX { get; set; }

    /// <summary>銃弾判定の Y 軸局所オフセット</summary>
    public float BulletHitboxOffsetY { get; set; }

    /// <summary>銃弾 1 発あたりの与ダメージ量</summary>
    public float BulletDamage { get; set; }

    /// <summary>近接攻撃力（Bunny）</summary>
    public float AttackDamage { get; set; }

    /// <summary>近接攻撃クールダウン間隔（秒）</summary>
    public float AttackCooldown { get; set; }

    /// <summary>近接攻撃前方出現オフセット（px）</summary>
    public float AttackRange { get; set; }

    /// <summary>近接攻撃判定幅（px）</summary>
    public float AttackWidth { get; set; }

    /// <summary>近接攻撃判定高さ（px）</summary>
    public float AttackHeight { get; set; }

    /// <summary>近接攻撃持続時間（秒）</summary>
    public float AttackDuration { get; set; }

    /// <summary>近接攻撃時の前進インパルス距離（px）</summary>
    public float AttackLungeDistance { get; set; }

    /// <summary>近接攻撃時のスプリング変形倍率 X</summary>
    public float AttackStretchScaleX { get; set; }

    /// <summary>近接攻撃時のスプリング変形倍率 Y</summary>
    public float AttackStretchScaleY { get; set; }

    /// <summary>射撃出膛時の自機微後退距離（ピクセル）</summary>
    public float ShootRecoilDistance { get; set; }

    /// <summary>射撃反動時のスプリング圧縮倍率 X</summary>
    public float ShootRecoilStretchX { get; set; }

    /// <summary>射撃反動時のスプリング伸長倍率 Y</summary>
    public float ShootRecoilStretchY { get; set; }

    /// <summary>銃口閃光（マズルフラッシュ）持続時間（秒）</summary>
    public float MuzzleFlashDuration { get; set; }

    /// <summary>銃口閃光（マズルフラッシュ）初期半径（ピクセル）</summary>
    public float MuzzleFlashRadius { get; set; }

    /// <summary>弾丸命中撃砕破片の最小飛散個数</summary>
    public int ImpactSplatterMinCount { get; set; }

    /// <summary>弾丸命中撃砕破片の最大飛散個数</summary>
    public int ImpactSplatterMaxCount { get; set; }

    /// <summary>弾丸命中撃砕破片の最小初速（px/s）</summary>
    public float ImpactSplatterMinSpeed { get; set; }

    /// <summary>弾丸命中撃砕破片の最大初速（px/s）</summary>
    public float ImpactSplatterMaxSpeed { get; set; }

    /// <summary>弾丸命中撃砕破片粒子の半径（ピクセル）</summary>
    public float ImpactSplatterParticleRadius { get; set; }

    /// <summary>弾丸命中撃砕破片のフェード持続時間（秒）</summary>
    public float ImpactSplatterDuration { get; set; }

    /// <summary>隙あり（Vulnerable / 易傷）状態の Boss に対する与ダメージ倍率</summary>
    public float VulnerableDamageMultiplier { get; set; }

    /// <summary>先行入力猶予時間（秒）</summary>
    public float JumpBufferDuration { get; set; }

    /// <summary>コヨーテタイム猶予時間（秒）</summary>
    public float CoyoteTimeDuration { get; set; }

    /// <summary>ジャンプ中断時の上昇速度減衰率</summary>
    public float JumpCutDamping { get; set; }

    /// <summary>頂点滞空判定の速度閾値（px/s）</summary>
    public float ApexFloatVelocityThreshold { get; set; }

    /// <summary>頂点滞空時の重力倍率</summary>
    public float ApexFloatGravityMultiplier { get; set; }

    /// <summary>ダッシュ突進速度（px/s）</summary>
    public float DashSpeed { get; set; }

    /// <summary>ダッシュ持続時間（秒）</summary>
    public float DashDuration { get; set; }

    /// <summary>ダッシュ再使用クールダウン時間（秒）</summary>
    public float DashCooldown { get; set; }

    /// <summary>ダッシュによる無敵時間（秒）</summary>
    public float DashInvincibleDuration { get; set; }

    /// <summary>ダッシュ開始時の横長ストレッチ変形倍率 X</summary>
    public float DashStretchScaleX { get; set; }

    /// <summary>ダッシュ開始時の扁平ストレッチ変形倍率 Y</summary>
    public float DashStretchScaleY { get; set; }

    /// <summary>ダッシュ中の残像生成間隔（秒）</summary>
    public float DashGhostTrailInterval { get; set; }

    /// <summary>ダッシュ残像のフェード持続時間（秒）</summary>
    public float DashGhostTrailDuration { get; set; }

    /// <summary>ダッシュ残像の初期アルファ値 (0～255)</summary>
    public float DashGhostTrailAlpha { get; set; }

    /// <summary>二段ジャンプ空気リングエフェクトの初期半径 (px)</summary>
    public float JumpRingInitialRadius { get; set; }

    /// <summary>二段ジャンプ空気リングエフェクトの最大拡散半径 (px)</summary>
    public float JumpRingMaxRadius { get; set; }

    /// <summary>二段ジャンプ空気リングエフェクトのフェード持続時間（秒）</summary>
    public float JumpRingDuration { get; set; }

    /// <summary>地面初速加速度（px/s²）</summary>
    public float GroundAcceleration { get; set; }

    /// <summary>地面自然減速加速度（px/s²）</summary>
    public float GroundDeceleration { get; set; }

    /// <summary>地面反転制動加速度（px/s²）</summary>
    public float TurnaroundDeceleration { get; set; }

    /// <summary>空中移動加速度（px/s²）</summary>
    public float AirAcceleration { get; set; }

    /// <summary>空中自然減速加速度（px/s²）</summary>
    public float AirDeceleration { get; set; }

    /// <summary>通常ヒットストップ時間（秒）</summary>
    public float HitstopLightDuration { get; set; }

    /// <summary>強攻撃・弱点ヒットストップ時間（秒）</summary>
    public float HitstopHeavyDuration { get; set; }

    /// <summary>通常ヒット時のスプライト振動振幅（ピクセル）</summary>
    public float HitJitterLightAmplitude { get; set; }

    /// <summary>強攻撃・弱点ヒット時のスプライト振動振幅（ピクセル）</summary>
    public float HitJitterHeavyAmplitude { get; set; }

    /// <summary>ヒット時スプライト局所振動周波数（Hz）</summary>
    public float HitJitterFrequency { get; set; }

    /// <summary>スプリング固有振動数（Hz）</summary>
    public float SpringFrequency { get; set; }

    /// <summary>スプリング減衰比</summary>
    public float SpringDampingRatio { get; set; }

    /// <summary>跳躍開始時ストレッチ拡大率 X</summary>
    public float JumpStretchScaleX { get; set; }

    /// <summary>跳躍開始時ストレッチ拡大率 Y</summary>
    public float JumpStretchScaleY { get; set; }

    /// <summary>着地時スカッシュ拡大率 X</summary>
    public float LandSquashScaleX { get; set; }

    /// <summary>着地時スカッシュ拡大率 Y</summary>
    public float LandSquashScaleY { get; set; }

    /// <summary>着地時スカッシュ演出を発火させる最小下落速度閾値（px/s）</summary>
    public float LandSquashVelocityThreshold { get; set; }

    /// <summary>被弾衝撃時スカッシュ拡大率 X</summary>
    public float HitSquashScaleX { get; set; }

    /// <summary>被弾衝撃時スカッシュ拡大率 Y</summary>
    public float HitSquashScaleY { get; set; }

    /// <inheritdoc />
    public virtual ErrorOr<Success> Validate()
    {
        if (MaxHp <= 0)
            return Error.Validation("PlayerConfig.InvalidMaxHp", "プレイヤー最大 HP は 1 以上である必要があります。");

        if (MoveSpeed < 0.0f)
            return Error.Validation("PlayerConfig.InvalidMoveSpeed", "プレイヤー移動速度は 0 以上である必要があります。");

        if (HurtboxShape == "Circle" && HurtboxRadius <= 0.0f)
            return Error.Validation("PlayerConfig.InvalidHurtboxRadius", "円形被弾判定の半径は 0 より大きい必要があります。");

        if (HurtboxShape == "Box" && (HurtboxWidth <= 0.0f || HurtboxHeight <= 0.0f))
            return Error.Validation("PlayerConfig.InvalidHurtboxBox", "矩形被弾判定の幅および高さは 0 より大きい必要があります。");

        return Result.Success;
    }

    /// <inheritdoc />
    public virtual void PostProcess()
    {
    }

    /// <summary>
    ///     設定に基づいたプレイヤー被弾判定 (<see cref="CollisionShape" />) を取得します。
    /// </summary>
    /// <returns>構築された被弾衝突形状バリアント</returns>
    public CollisionShape GetHurtboxShape()
    {
        return CollisionShapeFactory.CreateWithFallback(HurtboxShape, HurtboxRadius, HurtboxWidth, HurtboxHeight,
            HurtboxOffsetX, HurtboxOffsetY);
    }

    /// <summary>
    ///     設定に基づいたプレイヤー銃弾攻撃判定 (<see cref="CollisionShape" />) を取得します。
    /// </summary>
    /// <returns>構築された銃弾攻撃衝突形状バリアント</returns>
    public CollisionShape GetBulletHitboxShape()
    {
        return CollisionShapeFactory.CreateWithFallback(BulletHitboxShape, BulletRadius, BulletWidth, BulletHeight,
            BulletHitboxOffsetX, BulletHitboxOffsetY);
    }
}