namespace AnimalBattle.Components;

/// <summary>
/// Boss 2 (Ground Crusher) AI 状態機の微観アクション状態を表す列挙型。
/// 現代アクションゲームの階層型有限状態機械 (HFSM) 設計に基づき、細分化された状態を定義します。
/// </summary>
public enum Boss2StateKind
{
    /// <summary>待機・対峙観察状態（プラットフォーム上での短時間静止・様子見）</summary>
    Idle,

    /// <summary>同一プラットフォーム上での近接追従・歩行接近状態</summary>
    PatrolChase,

    /// <summary>別プラットフォームへの起跳・下落地点への移動状態</summary>
    MoveToTakeoff,

    /// <summary>跨プラットフォーム跳躍・空中上昇飛翔状態</summary>
    PlatformJumping,

    /// <summary>プラットフォーム端からの落下・空中自由落下状態</summary>
    PlatformFalling,

    /// <summary>着地直後の微小衝撃吸収・着地硬直状態</summary>
    LandingRecovery,

    /// <summary>重劈斬撃の前揺蓄力状態（前方赤色警戒枠・突進準備）</summary>
    TelegraphHeavySlash,

    /// <summary>二連撃の前揺蓄力状態（構え・連撃予兆）</summary>
    TelegraphDoubleCleave,

    /// <summary>撼地踏撃の前揺蓄力状態（足を高く引き上げ火花集中・足元警戒）</summary>
    TelegraphGroundStomp,

    /// <summary>泰坦躍撃の前揺蓄力状態（深いしゃがみ込み・プレイヤー足元ロックオン）</summary>
    TelegraphLeapSmash,

    /// <summary>狂暴突進の前揺蓄力状態（足元地面掻き・怒気蒸気・直線突進警戒）</summary>
    TelegraphFrenzyCharge,

    /// <summary>滅地狂吼の前揺蓄力状態（深吸気・全画面共鳴震動リング）</summary>
    TelegraphEarthquakeRoar,

    /// <summary>重劈突進斬撃の実行状態（前方スライド斬撃・Hitbox 判定）</summary>
    ExecutingHeavySlash,

    /// <summary>二連撃斬撃の実行状態（一段目薙ぎ払い＋二段目強撃）</summary>
    ExecutingDoubleCleave,

    /// <summary>撼地踏撃の実行状態（足踏み衝撃波＋左右地走り衝撃波生成）</summary>
    ExecutingGroundStomp,

    /// <summary>泰坦躍撃の上昇・目標接近飛翔状態</summary>
    ExecutingLeapAscent,

    /// <summary>泰坦躍撃の急降下猛撃状態（高速落下・激突爆発＋破片飛散）</summary>
    ExecutingLeapDescent,

    /// <summary>狂暴突進の高速疾走状態（地面を猛スピードで直進突進）</summary>
    ExecutingFrenzyCharge,

    /// <summary>滅地狂吼の継続詠唱状態（咆哮咆吼・落石隕石群の連続生成）</summary>
    ExecutingEarthquakeRoar,

    /// <summary>突進時の壁激突・眩暈気絶硬直状態（プレイヤーの最大攻撃チャンス）</summary>
    WallCrashStun,

    /// <summary>技発動後の後揺硬直・破綻隙状態（プレイヤーの反撃好機）</summary>
    SkillRecovery,

    /// <summary>フェーズ転換時の無敵咆哮・爆気衝撃波状態</summary>
    PhaseTransition,

    /// <summary>撃破・死亡演出状態</summary>
    Dead
}

/// <summary>
/// Boss 2 (Ground Crusher) AI の分層状態機械 (HFSM) 実行時データを保持するコンポーネント。
/// </summary>
public struct Boss2StateMachine
{
    /// <summary>現在の微観アクション状態</summary>
    public Boss2StateKind CurrentState;

    /// <summary>現在の状態の経過時間（秒）</summary>
    public float StateTimer;

    /// <summary>現在の状態の目標持続時間（秒）</summary>
    public float StateDuration;

    /// <summary>現在フェーズの技能リストにおける実行インデックス</summary>
    public int CurrentSkillIndex;

    /// <summary>現在実行中または準備中の技能識別名</summary>
    public string CurrentSkillName;

    /// <summary>技能発動中のサブタイマー（連射間隔・連撃インターバルなど）</summary>
    public float SubTimer;

    /// <summary>移動・突進・跳躍開始位置 X</summary>
    public float StartX;

    /// <summary>移動・突進・跳躍開始位置 Y</summary>
    public float StartY;

    /// <summary>突進・跳躍目標位置 X</summary>
    public float TargetX;

    /// <summary>突進・跳躍目標位置 Y</summary>
    public float TargetY;

    /// <summary>突進・攻撃中の移動速度</summary>
    public float ActionSpeed;

    /// <summary>フェーズ転換中などの無敵保護フラグ</summary>
    public bool IsInvulnerable;

    /// <summary>現在いずれかの攻撃アクション実行中であるかどうか</summary>
    public readonly bool IsAttacking => CurrentState is >= Boss2StateKind.ExecutingHeavySlash and <= Boss2StateKind.ExecutingEarthquakeRoar;

    /// <summary>現在いずれかの前揺蓄力・予警中であるかどうか</summary>
    public readonly bool IsTelegraphing => CurrentState is >= Boss2StateKind.TelegraphHeavySlash and <= Boss2StateKind.TelegraphEarthquakeRoar;

    /// <summary>現在空中滞空・跳躍・落下中であるかどうか</summary>
    public readonly bool IsAirborne => CurrentState is Boss2StateKind.PlatformJumping
        or Boss2StateKind.PlatformFalling
        or Boss2StateKind.ExecutingLeapAscent
        or Boss2StateKind.ExecutingLeapDescent;

    /// <summary>現在プレイヤーの攻撃に対して大きな隙（脆弱状態）を晒しているかどうか</summary>
    public readonly bool IsVulnerable => CurrentState is Boss2StateKind.SkillRecovery or Boss2StateKind.WallCrashStun;

    /// <summary>
    /// <see cref="Boss2StateMachine"/> の新しいインスタンスを初期化します。
    /// </summary>
    public Boss2StateMachine()
    {
        CurrentState = Boss2StateKind.Idle;
        StateTimer = 0.0f;
        StateDuration = 0.5f;
        CurrentSkillIndex = 0;
        CurrentSkillName = "HeavySlash";
        SubTimer = 0.0f;
        StartX = 0.0f;
        StartY = 0.0f;
        TargetX = 0.0f;
        TargetY = 0.0f;
        ActionSpeed = 0.0f;
        IsInvulnerable = false;
    }
}
