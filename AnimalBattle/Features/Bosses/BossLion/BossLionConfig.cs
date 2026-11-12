namespace AnimalBattle.Configs;

/// <summary>
///     BossLion 個別フェーズの FSM 状態機械設定クラス。
/// </summary>
public sealed class BossLionPhaseConfig {
    /// <summary>このフェーズが有効となる HP 割合上限</summary>
    public float HpThreshold { get; set; }

    /// <summary>攻撃前の待機巡航時間秒単位</summary>
    public float IdleDuration { get; set; }

    /// <summary>このフェーズでの地面移動速度</summary>
    public float MoveSpeed { get; set; }

    /// <summary>このフェーズでの跳躍初速</summary>
    public float JumpForce { get; set; }

    /// <summary>フェーズ突入時の無敵・吼叫演出時間秒単位</summary>
    public float TransitionDuration { get; set; }

    /// <summary>プレイヤー追跡時に遠隔技へ移行するまでのタイムアウト時間秒単位</summary>
    public float ChaseTimeout { get; set; }

    /// <summary>このフェーズで実行される技能名リスト：旧仕様との互換用</summary>
    public List<string> Skills { get; set; } = [];

    /// <summary>このフェーズで評価される技能意思決定ルールリスト</summary>
    public List<AttackRuleConfig> Rules { get; set; } = [];
}

/// <summary>
///     Config/boss_lion.toml から読み込まれる BossLion の基本パラメータおよび各フェーズの FSM 設定クラス。
/// </summary>
public sealed class BossLionConfig : BaseBossConfig {
    /// <summary>基本地面移動速度</summary>
    public float MoveSpeed { get; set; }

    /// <summary>基本跳躍初速</summary>
    public float JumpForce { get; set; }

    /// <summary>重力加速度</summary>
    public float Gravity { get; set; }

    /// <summary>プレイヤー追跡時に遠隔技へ移行するまでの基本タイムアウト時間秒単位</summary>
    public float ChaseTimeout { get; set; }

    /// <summary>最大落下速度</summary>
    public float MaxFallSpeed { get; set; }

    /// <summary>跳躍上昇最大時間秒単位</summary>
    public float MaxAscentDuration { get; set; }

    /// <summary>着地硬直時間秒単位</summary>
    public float LandingRecoveryDuration { get; set; }

    /// <summary>移動判定の最小位置差分閾値ピクセル単位</summary>
    public float MoveThreshold { get; set; }

    /// <summary>跳躍クールダウン時間秒単位</summary>
    public float JumpCooldownDuration { get; set; }

    /// <summary>同一足場と判定する垂直許容誤差ピクセル単位</summary>
    public float SamePlatformToleranceY { get; set; }

    /// <summary>プレイヤー空中浮遊判定の垂直速度閾値ピクセル毎秒単位</summary>
    public float AirborneVelocityThreshold { get; set; }

    /// <summary>プレイヤー空中判定の相対高度差閾値ピクセル単位</summary>
    public float AirborneHeightThreshold { get; set; }

    /// <summary>カイト対策近接技能識別名</summary>
    public string AntiKitingMeleeSkill { get; set; } = string.Empty;

    /// <summary>カイト対策遠隔技能識別名</summary>
    public string AntiKitingRangedSkill { get; set; } = string.Empty;

    /// <summary>Phase 1 状態機設定</summary>
    public BossLionPhaseConfig Phase1 { get; set; } = new();

    /// <summary>Phase 2 状態機設定</summary>
    public BossLionPhaseConfig Phase2 { get; set; } = new();

    /// <summary>Phase 3 状態機設定</summary>
    public BossLionPhaseConfig Phase3 { get; set; } = new();

    /// <inheritdoc />
    public override float Phase2HpThreshold => Phase2.HpThreshold;

    /// <inheritdoc />
    public override float Phase3HpThreshold => Phase3.HpThreshold;

    /// <summary>Phase 遷移：フェーズ移行能力設定</summary>
    public PhaseTransitionAbilityConfig PhaseTransition { get; set; } = new();
}