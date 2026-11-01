using System.Drawing;
using AnimalBattle.Configs;
using ErrorOr;
using Microsoft.Extensions.Configuration;

namespace AnimalBattle.Features.Tutorial.Configs;

/// <summary>
///     チュートリアル画面の配色パレット設定クラス。
/// </summary>
public sealed class TutorialColorsConfig
{
    private Color _gold;
    [ConfigurationKeyName("Gold")]
    public string GoldHex { get => string.Empty; set => _gold = value.ToColor(); }
    public Color Gold => _gold;

    private Color _white;
    [ConfigurationKeyName("White")]
    public string WhiteHex { get => string.Empty; set => _white = value.ToColor(); }
    public Color White => _white;

    private Color _cyan;
    [ConfigurationKeyName("Cyan")]
    public string CyanHex { get => string.Empty; set => _cyan = value.ToColor(); }
    public Color Cyan => _cyan;

    private Color _red;
    [ConfigurationKeyName("Red")]
    public string RedHex { get => string.Empty; set => _red = value.ToColor(); }
    public Color Red => _red;

    private Color _muted;
    [ConfigurationKeyName("Muted")]
    public string MutedHex { get => string.Empty; set => _muted = value.ToColor(); }
    public Color Muted => _muted;

    private Color _panelBg;
    [ConfigurationKeyName("PanelBg")]
    public string PanelBgHex { get => string.Empty; set => _panelBg = value.ToColor(); }
    public Color PanelBg => _panelBg;

    private Color _panelBorder;
    [ConfigurationKeyName("PanelBorder")]
    public string PanelBorderHex { get => string.Empty; set => _panelBorder = value.ToColor(); }
    public Color PanelBorder => _panelBorder;

    private Color _doorBg;
    [ConfigurationKeyName("DoorBg")]
    public string DoorBgHex { get => string.Empty; set => _doorBg = value.ToColor(); }
    public Color DoorBg => _doorBg;

    private Color _tooltipBg;
    [ConfigurationKeyName("TooltipBg")]
    public string TooltipBgHex { get => string.Empty; set => _tooltipBg = value.ToColor(); }
    public Color TooltipBg => _tooltipBg;
}

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
///     ボス挑戦扉システム全体設定。
/// </summary>
public sealed class TutorialDoorsConfig
{
    public Vec2i Size { get; set; }
    public int PillarWidth { get; set; }
    public int LintelHeight { get; set; }
    public int PedestalHeight { get; set; }
    public int PedestalExtraWidth { get; set; }
    public float InteractionRadius { get; set; }
    public int TooltipOffsetY { get; set; }
    public Vec2i TooltipSize { get; set; }

    public Vec2f PreviewScale { get; set; }

    private Color _stoneBase;
    [ConfigurationKeyName("StoneBase")]
    public string StoneBaseHex { get => string.Empty; set => _stoneBase = value.ToColor(); }
    public Color StoneBase => _stoneBase;

    private Color _stoneHighlight;
    [ConfigurationKeyName("StoneHighlight")]
    public string StoneHighlightHex { get => string.Empty; set => _stoneHighlight = value.ToColor(); }
    public Color StoneHighlight => _stoneHighlight;

    private Color _stoneShadow;
    [ConfigurationKeyName("StoneShadow")]
    public string StoneShadowHex { get => string.Empty; set => _stoneShadow = value.ToColor(); }
    public Color StoneShadow => _stoneShadow;

    private Color _voidBg;
    [ConfigurationKeyName("VoidBg")]
    public string VoidBgHex { get => string.Empty; set => _voidBg = value.ToColor(); }
    public Color VoidBg => _voidBg;

    public TutorialDoorItemConfig Snake { get; set; } = new();
    public TutorialDoorItemConfig Lion { get; set; } = new();
}

/// <summary>
///     UI画面レイアウト座標設定。
/// </summary>
public sealed class TutorialLayoutConfig
{
    public Vec2i GuidanceTitlePos { get; set; }
    public Vec2i GuidanceSubPos { get; set; }
    public Vec2i RetryPos { get; set; }
    public Vec2i ControlBoardP1 { get; set; }
    public Vec2i ControlBoardP2 { get; set; }
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

    /// <summary>小怪戦闘練習</summary>
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
///     小怪配置設定。
/// </summary>
public sealed class TutorialMinionItemConfig
{
    public string MinionType { get; set; } = string.Empty;
    public Vec2f Pos { get; set; }
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
///     チュートリアル全体設定クラス。
/// </summary>
public sealed class TutorialConfig : IConfigLifecycle
{
    public TutorialColorsConfig Colors { get; set; } = new();
    public TutorialDoorsConfig Doors { get; set; } = new();
    public TutorialLayoutConfig Layout { get; set; } = new();
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
