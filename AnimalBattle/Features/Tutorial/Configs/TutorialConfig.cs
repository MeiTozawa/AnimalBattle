using System.Drawing;
using ErrorOr;
using Microsoft.Extensions.Configuration;

namespace AnimalBattle.Features.Tutorial.Configs;

/// <summary>
///     個別ボス扉設定。
/// </summary>
public sealed class TutorialDoorItemConfig
{
    public Vec2f Pos { get; set; }
    public string BossType { get; set; } = string.Empty;
    public string TexturePath { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string RuneSymbols { get; set; } = string.Empty;

    private Color _runeColorIdle;
    [ConfigurationKeyName("RuneColorIdle")]
    public string RuneColorIdleHex { get => string.Empty; set => _runeColorIdle = value.ToColor(); }
    public Color RuneColorIdle => _runeColorIdle;

    private Color _runeColorActive;
    [ConfigurationKeyName("RuneColorActive")]
    public string RuneColorActiveHex { get => string.Empty; set => _runeColorActive = value.ToColor(); }
    public Color RuneColorActive => _runeColorActive;

    private Color _auraColor;
    [ConfigurationKeyName("AuraColor")]
    public string AuraColorHex { get => string.Empty; set => _auraColor = value.ToColor(); }
    public Color AuraColor => _auraColor;

    public float MotionSpeed { get; set; }
    public float MotionAmplitude { get; set; }
}

/// <summary>
///     タイミングおよび物理パラメータ設定。
/// </summary>
public sealed class TutorialTimingConfig
{
    public float TargetCollectDelay { get; set; }
    public float MinionDefeatDelay { get; set; }
    public float RetryDuration { get; set; }
    public float TargetFloatSpeed { get; set; }
    public float TargetFloatAmplitude { get; set; }
    public float ReachMargin { get; set; }
}

/// <summary>
///     チュートリアル全テキスト設定。
/// </summary>
public sealed class TutorialTextsConfig
{
    public string RetryPrompt { get; set; } = string.Empty;
    public string DoorInteractPrompt { get; set; } = string.Empty;
    public string ControlBoardTitle { get; set; } = string.Empty;
    public string ControlBoardMove { get; set; } = string.Empty;
    public string ControlBoardJump { get; set; } = string.Empty;
    public string ControlBoardDash { get; set; } = string.Empty;
    public string ControlBoardAttack { get; set; } = string.Empty;
}

/// <summary>
///     チュートリアルフェーズの種別。
/// </summary>
public enum TutorialPhaseType
{
    /// <summary>目標物（コイン・星）収集</summary>
    Collect,

    /// <summary>ミニオン戦闘練習</summary>
    Combat,

    /// <summary>ボス挑戦扉（次シーン移行）</summary>
    BossDoor
}

/// <summary>
///     収集ターゲットアイテム設定。
/// </summary>
public sealed class TutorialTargetItemConfig
{
    public Vec2f Pos { get; set; }
    public float Radius { get; set; }
}

/// <summary>
///     ミニオン配置設定。
/// </summary>
public sealed class TutorialMinionItemConfig
{
    public string MinionType { get; set; } = string.Empty;
}

/// <summary>
///     単一フェーズの汎用設定クラス。
/// </summary>
public sealed class TutorialPhaseConfig
{
    public TutorialPhaseType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;

    public TutorialTargetItemConfig[] Targets { get; set; } = [];
    public TutorialMinionItemConfig[] Minions { get; set; } = [];
    public TutorialDoorItemConfig[] Doors { get; set; } = [];
}

/// <summary>
///     チュートリアル全体設定クラス（ゲームプレイ・フェーズ進行ロジック）。
/// </summary>
public sealed class TutorialConfig : IConfigLifecycle
{
    public TutorialTimingConfig Timing { get; set; } = new();
    public TutorialTextsConfig Texts { get; set; } = new();

    /// <summary>
    ///     データ駆動フェーズシーケンス一覧。
    /// </summary>
    public TutorialPhaseConfig[] Phases { get; set; } = [];

    /// <inheritdoc />
    public ErrorOr<Success> Validate()
    {
        return Result.Success;
    }

    /// <inheritdoc />
    public void PostProcess() { }
}
