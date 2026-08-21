namespace AnimalBattle.Components;

/// <summary>
/// Boss 2 (Ground Crusher) AI 状態機の微観アクション状態を表す列挙型。
/// </summary>
public enum Boss2StateKind
{
    /// <summary>地面巡回・プレイヤー追従待機状態</summary>
    PatrolChase,

    /// <summary>前揺蓄力預警状態（聚気発光・赤枠警告・プレイヤーに反応時間を与える）</summary>
    Telegraphing,

    /// <summary>技能発動・近接斬撃・踏撃・突進状態</summary>
    Casting,

    /// <summary>滞空跳躍・飛撲状態</summary>
    LeapJumping,

    /// <summary>後揺硬直状態（攻撃後の隙・プレイヤーの反撃好機）</summary>
    Recovering,

    /// <summary>フェーズ転換状態（無敵演出・咆哮・爆気）</summary>
    PhaseTransition
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

    /// <summary>現在実行中の技能識別名</summary>
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

    /// <summary>
    /// <see cref="Boss2StateMachine"/> の新しいインスタンスを初期化します。
    /// </summary>
    public Boss2StateMachine()
    {
        CurrentState = Boss2StateKind.PatrolChase;
        StateTimer = 0.0f;
        StateDuration = 1.0f;
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
