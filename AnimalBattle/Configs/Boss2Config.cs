using ErrorOr;

namespace AnimalBattle.Configs;

/// <summary>
///     Boss 2 (Ground Crusher) 個別フェーズの FSM 状態機械設定クラス。
/// </summary>
public sealed class Boss2PhaseConfig {
    /// <summary>このフェーズが有効となる HP 割合上限</summary>
    public float HpThreshold { get; set; }

    /// <summary>攻撃前の待機巡航時間（秒）</summary>
    public float IdleDuration { get; set; }

    /// <summary>このフェーズでの巡回移動時間（秒）</summary>
    public float PatrolDuration { get; set; }

    /// <summary>このフェーズでの地面移動速度</summary>
    public float MoveSpeed { get; set; }

    /// <summary>このフェーズでの跳躍初速</summary>
    public float JumpForce { get; set; }

    /// <summary>蓄力予兆時の機体振動振幅（ピクセル）</summary>
    public float TelegraphShakeIntensity { get; set; }

    /// <summary>フェーズ突入時の無敵・吼叫演出時間（秒）</summary>
    public float TransitionDuration { get; set; }

    /// <summary>プレイヤー追跡時に遠隔技へ移行するまでのタイムアウト時間（秒）</summary>
    public float ChaseTimeout { get; set; }

    /// <summary>このフェーズで実行される技能名リスト（旧仕様との互換用）</summary>
    public List<string> Skills { get; set; } = [];

    /// <summary>このフェーズで評価される技能意思決定ルールリスト</summary>
    public List<AttackRuleConfig> Rules { get; set; } = [];
}

/// <summary>
///     Config/boss2.toml または boss1.toml から読み込まれる技能意思決定ルールの設定クラスです。
/// </summary>
public sealed class AttackRuleConfig {
    /// <summary>技能識別名（例: "HeavySlash", "DoubleCleave"）</summary>
    public string Skill { get; set; } = string.Empty;

    /// <summary>技能の基本冷却時間（秒）</summary>
    public float Cooldown { get; set; }

    /// <summary>最小有効距離（ピクセル）</summary>
    public float MinDistance { get; set; }

    /// <summary>最大有効距離（ピクセル）</summary>
    public float MaxDistance { get; set; }

    /// <summary>基礎加重（欲望値）</summary>
    public float Weight { get; set; }

    /// <summary>同一足場を要求するかどうか</summary>
    public bool RequireSamePlatform { get; set; }

    /// <summary>プレイヤーの接地を要求するかどうか（null の場合は問わない）</summary>
    public bool? RequirePlayerGrounded { get; set; }

    /// <summary>プレイヤー空中時の加重倍率</summary>
    public float PreferAirborneBonus { get; set; }
}

/// <summary>
///     Config/boss2.toml から読み込まれる Boss 2 (Ground Crusher) の基本パラメータおよび各フェーズの FSM 設定クラス。
/// </summary>
public sealed class Boss2Config {
    /// <summary>Boss 表示名</summary>
    public string BossName { get; set; } = string.Empty;

    /// <summary>Boss の最大 HP</summary>
    public float MaxHp { get; set; }

    /// <summary>Boss の基準出現 X 座標</summary>
    public float SpawnX { get; set; }

    /// <summary>Boss の基準出現 Y 座標</summary>
    public float SpawnY { get; set; }

    /// <summary>Boss 描画スケール X</summary>
    public float ScaleX { get; set; }

    /// <summary>Boss 描画スケール Y</summary>
    public float ScaleY { get; set; }

    /// <summary>Boss 被弾判定 (Hurtbox) の幾何形状種別（"Box" または "Circle"）</summary>
    public string HurtboxShape { get; set; } = string.Empty;

    /// <summary>Boss 被弾当たり判定幅</summary>
    public float HurtboxWidth { get; set; }

    /// <summary>Boss 被弾当たり判定高さ</summary>
    public float HurtboxHeight { get; set; }

    /// <summary>Boss 被弾当たり判定円半径</summary>
    public float HurtboxRadius { get; set; }

    /// <summary>Boss 被弾判定の X 軸局所オフセット</summary>
    public float HurtboxOffsetX { get; set; }

    /// <summary>Boss 被弾判定の Y 軸局所オフセット</summary>
    public float HurtboxOffsetY { get; set; }

    /// <summary>Boss 身体接触攻撃判定 (Hitbox) の幾何形状種別（"Box" または "Circle"）</summary>
    public string HitboxShape { get; set; } = string.Empty;

    /// <summary>Boss 接触攻撃判定幅</summary>
    public float BodyHitboxWidth { get; set; }

    /// <summary>Boss 接触攻撃判定高さ</summary>
    public float BodyHitboxHeight { get; set; }

    /// <summary>Boss 接触攻撃判定円半径</summary>
    public float BodyHitboxRadius { get; set; }

    /// <summary>Boss 接触判定の X 軸局所オフセット</summary>
    public float BodyHitboxOffsetX { get; set; }

    /// <summary>Boss 接触判定の Y 軸局所オフセット</summary>
    public float BodyHitboxOffsetY { get; set; }

    /// <summary>Boss 身体接触命中時の与ダメージ量</summary>
    public float BodyContactDamage { get; set; }

    /// <summary>地形衝突判定幅</summary>
    public float CollisionWidth { get; set; }

    /// <summary>地形衝突判定高さ</summary>
    public float CollisionHeight { get; set; }

    /// <summary>近接斬撃エフェクトの基準前方生成オフセット X</summary>
    public float SlashSpawnOffsetX { get; set; }

    /// <summary>近接斬撃エフェクトの基準生成オフセット Y</summary>
    public float SlashSpawnOffsetY { get; set; }

    /// <summary>基本地面移動速度</summary>
    public float MoveSpeed { get; set; }

    /// <summary>基本跳躍初速</summary>
    public float JumpForce { get; set; }

    /// <summary>重力加速度</summary>
    public float Gravity { get; set; }

    /// <summary>巡回パトロール半径</summary>
    public float PatrolRadius { get; set; }

    /// <summary>プレイヤー追跡時に遠隔技へ移行するまでの基本タイムアウト時間（秒）</summary>
    public float ChaseTimeout { get; set; }

    /// <summary>被弾時ホワイトフラッシュ持続時間（秒）</summary>
    public float HitFlashDuration { get; set; }

    /// <summary>最大落下速度</summary>
    public float MaxFallSpeed { get; set; }

    /// <summary>跳躍上昇最大時間（秒）</summary>
    public float MaxAscentDuration { get; set; }

    /// <summary>着地硬直時間（秒）</summary>
    public float LandingRecoveryDuration { get; set; }

    /// <summary>跳躍離陸点判定距離（ピクセル）</summary>
    public float TakeoffDistance { get; set; }

    /// <summary>離陸地点への移動タイムアウト時間（秒）</summary>
    public float MoveToTakeoffTimeout { get; set; }

    /// <summary>離陸横速度閾値（ピクセル/秒）</summary>
    public float TakeoffVelocityThreshold { get; set; }

    /// <summary>移動判定の最小位置差分閾値（ピクセル）</summary>
    public float MoveThreshold { get; set; }

    /// <summary>跳躍クールダウン時間（秒）</summary>
    public float JumpCooldownDuration { get; set; }

    /// <summary>壁激突時のスタン気絶時間（秒）</summary>
    public float WallCrashStunDuration { get; set; }

    /// <summary>同一足場と判定する垂直許容誤差（ピクセル）</summary>
    public float SamePlatformToleranceY { get; set; }

    /// <summary>プレイヤー空中浮遊判定の垂直速度閾値（ピクセル/秒）</summary>
    public float AirborneVelocityThreshold { get; set; }

    /// <summary>プレイヤー空中判定の相対高度差閾値（ピクセル）</summary>
    public float AirborneHeightThreshold { get; set; }

    /// <summary>近接攻撃発動の横方向判定距離（ピクセル）</summary>
    public float MeleeTriggerRangeX { get; set; }

    /// <summary>近接攻撃発動の縦方向判定距离（ピクセル）</summary>
    public float MeleeTriggerRangeY { get; set; }

    /// <summary>カイト対策で遠距離技（吼叫）を優先する直線距離閾値（ピクセル）</summary>
    public float AntiKitingRangedDistance { get; set; }

    /// <summary>カイト対策で中距離技（泰山圧頂）を優先する直線距離閾値（ピクセル）</summary>
    public float AntiKitingMidDistance { get; set; }

    /// <summary>カイト対策で強制技発動を行う追跡タイムアウト倍率</summary>
    public float AntiKitingEnforceMultiplier { get; set; }

    /// <summary>Phase 1 状態機設定</summary>
    public Boss2PhaseConfig Phase1 { get; set; } = new();

    /// <summary>Phase 2 状態機設定</summary>
    public Boss2PhaseConfig Phase2 { get; set; } = new();

    /// <summary>Phase 3 状態機設定</summary>
    public Boss2PhaseConfig Phase3 { get; set; } = new();

    /// <summary>
    ///     設定に基づいた Boss 2 被弾判定 (<see cref="CollisionShape" />) を取得します。
    /// </summary>
    /// <returns>構築された被弾衝突形状バリアント</returns>
    public CollisionShape GetHurtboxShape()
    {
        return CollisionShapeFactory.CreateWithFallback(HurtboxShape, HurtboxRadius, HurtboxWidth, HurtboxHeight,
            HurtboxOffsetX, HurtboxOffsetY);
    }

    /// <summary>
    ///     設定に基づいた Boss 2 接触攻撃判定 (<see cref="CollisionShape" />) を取得します。
    /// </summary>
    /// <returns>構築された接触攻撃衝突形状バリアント</returns>
    public CollisionShape GetHitboxShape()
    {
        return CollisionShapeFactory.CreateWithFallback(HitboxShape, BodyHitboxRadius, BodyHitboxWidth,
            BodyHitboxHeight, BodyHitboxOffsetX, BodyHitboxOffsetY);
    }

    /// <summary>
    ///     指定された TOML ファイルから設定を読み込んだ <see cref="ConfigBase{Boss2Config}" /> インスタンスを生成します。
    /// </summary>
    /// <param name="path">TOML ファイル名</param>
    /// <returns>読み込み結果</returns>
    public static ErrorOr<ConfigBase<Boss2Config>> LoadFromFile(string path = ResourcePath.Config.Boss2)
    {
        return ConfigBase<Boss2Config>.LoadFromFile(path);
    }
}