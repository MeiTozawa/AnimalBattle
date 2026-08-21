namespace AnimalBattle.Components;

/// <summary>
/// Boss 1 (Ancient Cat Overlord) AI 状態機の微観アクション状態を表す列挙型。
/// </summary>
public enum Boss1StateKind
{
    /// <summary>自由巡航・遊弋待機状態</summary>
    Hovering,

    /// <summary>前揺蓄力預警状態（聚気発光・赤枠警告・プレイヤーに反応時間を与える）</summary>
    Telegraphing,

    /// <summary>技能発動・弾幕発射および突進攻撃状態</summary>
    Casting,

    /// <summary>後揺硬直状態（攻撃後の隙・プレイヤーの反撃好機）</summary>
    Recovering,

    /// <summary>フェーズ転換状態（無敵演出・全画面弾幕消去・爆気）</summary>
    PhaseTransition
}

/// <summary>
/// Boss 1 (Ancient Cat Overlord) AI の分層状態機械 (HFSM) 実行時データを保持するコンポーネント。
/// </summary>
public struct Boss1StateMachine
{
    /// <summary>現在の微観アクション状態</summary>
    public Boss1StateKind CurrentState;

    /// <summary>現在の状態の経過時間（秒）</summary>
    public float StateTimer;

    /// <summary>現在の状態の目標持続時間（秒）</summary>
    public float StateDuration;

    /// <summary>現在フェーズの技能リストにおける実行インデックス</summary>
    public int CurrentSkillIndex;

    /// <summary>現在実行中の技能識別名</summary>
    public string CurrentSkillName;

    /// <summary>技能発動中のサブタイマー（連射間隔など）</summary>
    public float SubTimer;

    /// <summary>弾幕の回転累積角度（ラジアン）</summary>
    public float AngleOffset;

    /// <summary>突進開始位置 X</summary>
    public float StartX;

    /// <summary>突進開始位置 Y</summary>
    public float StartY;

    /// <summary>突進目標位置 X</summary>
    public float TargetX;

    /// <summary>突進目標位置 Y</summary>
    public float TargetY;

    /// <summary>フェーズ転換中などの無敵保護フラグ</summary>
    public bool IsInvulnerable;

    /// <summary>
    /// <see cref="Boss1StateMachine"/> の新しいインスタンスを初期化します。
    /// </summary>
    public Boss1StateMachine()
    {
        CurrentState = Boss1StateKind.Hovering;
        StateTimer = 0.0f;
        StateDuration = 1.0f;
        CurrentSkillIndex = 0;
        CurrentSkillName = string.Empty;
        SubTimer = 0.0f;
        AngleOffset = 0.0f;
        StartX = 0.0f;
        StartY = 0.0f;
        TargetX = 0.0f;
        TargetY = 0.0f;
        IsInvulnerable = false;
    }
}
