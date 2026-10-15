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
///     Config/boss2.toml から読み込まれる Boss 2 (Ground Crusher) の基本パラメータおよび各フェーズの FSM 設定クラス。
/// </summary>
public sealed class Boss2Config : BaseBossConfig {
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

    /// <summary>カイト対策近接技能識別名</summary>
    public string AntiKitingMeleeSkill { get; set; } = string.Empty;

    /// <summary>カイト対策遠隔技能識別名</summary>
    public string AntiKitingRangedSkill { get; set; } = string.Empty;

    /// <summary>目標ロック・飛翔技能識別名</summary>
    public string TargetLockSkill { get; set; } = string.Empty;

    /// <summary>壁激突スタン技能識別名</summary>
    public string WallStunSkill { get; set; } = string.Empty;

    /// <summary>Phase 1 状態機設定</summary>
    public Boss2PhaseConfig Phase1 { get; set; } = new();

    /// <summary>Phase 2 状態機設定</summary>
    public Boss2PhaseConfig Phase2 { get; set; } = new();

    /// <summary>Phase 3 状態機設定</summary>
    public Boss2PhaseConfig Phase3 { get; set; } = new();

    /// <summary>Phase 遷移（フェーズ移行）能力設定</summary>
    public PhaseTransitionAbilityConfig PhaseTransition { get; set; } = new();

    /// <summary>壁激突気絶（スタン）設定</summary>
    public WallCrashStunConfig WallCrashStun { get; set; } = new();



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