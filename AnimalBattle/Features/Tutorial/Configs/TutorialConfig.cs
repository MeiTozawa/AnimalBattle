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
///     蛇神（BossSnake）扉プレビュー動態および毒霊オーブ描画設定。
/// </summary>
public sealed class TutorialSnakeMotionConfig
{
    public int PreviewOffsetY { get; set; }
    public float BobSpeedMultiplierX { get; set; }
    public float BobAmplitudeMultiplierX { get; set; }
    public int OrbCount { get; set; }
    public float OrbSpeed { get; set; }
    public Vec2f OrbRadius { get; set; }
    public int OrbIdleRadius { get; set; }
    public int OrbActiveRadius { get; set; }
    public int OrbGlowDelta { get; set; }
}

/// <summary>
///     獅子神（BossLion）扉プレビュー動態、重砸着地および地裂衝撃波描画設定。
/// </summary>
public sealed class TutorialLionMotionConfig
{
    public int PreviewOffsetY { get; set; }
    public float TextureHeight { get; set; }

    // モーションタイムラインフェーズ境界
    public float IdlePhaseEnd { get; set; }
    public float JumpPhaseEnd { get; set; }
    public float PeakPhaseEnd { get; set; }
    public float FallPhaseEnd { get; set; }

    // 変形・呼吸・跳躍パラメータ
    public float BreathFrequency { get; set; }
    public float BreathAmplitude { get; set; }
    public float JumpStretchX { get; set; }
    public float JumpStretchY { get; set; }
    public float PeakScaleX { get; set; }
    public float PeakScaleY { get; set; }
    public float FallStretchX { get; set; }
    public float FallStretchY { get; set; }
    public float SquashOscillation { get; set; }
    public float SquashAmplitude { get; set; }

    // 着地震動
    public float TremorDuration { get; set; }
    public float TremorFrequency { get; set; }
    public float TremorAmplitude { get; set; }

    // 地裂衝撃波エフェクト
    public int GroundOffsetY { get; set; }
    public float MaxSpread { get; set; }
    public int WaveOriginOffset { get; set; }
    public int WaveThickness { get; set; }
    public float Spike1Height { get; set; }
    public float Spike2Height { get; set; }
    public int Spike1Offset { get; set; }
    public int Spike2Offset { get; set; }
    public int SpikeThickness { get; set; }
    public float ArcBaseRadius { get; set; }
    public float ArcSpread { get; set; }
    public int ArcSegments { get; set; }
    public Vec2f ArcScale { get; set; }
    public int ArcThickness { get; set; }
    public float FlashThreshold { get; set; }
    public float FlashMaxAlpha { get; set; }
    public Vec2i FlashHalfSize { get; set; }
    public float MaxShockwaveAlpha { get; set; }
}

/// <summary>
///     目標物（コイン／リング）描画設定。
/// </summary>
public sealed class TutorialTargetRenderConfig
{
    public float InnerRadiusRatio { get; set; }
    public int MinInnerRadius { get; set; }
    public string Symbol { get; set; } = string.Empty;
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
    public int LintelExtraWidth { get; set; }
    public float InteractionRadius { get; set; }
    public int TooltipOffsetY { get; set; }
    public Vec2i TooltipSize { get; set; }

    public int RuneMarginY { get; set; }
    public int RuneMinSpanY { get; set; }
    public int RuneCount { get; set; }
    public int RuneGlyphStep { get; set; }
    public int PlaqueTitleOffsetY { get; set; }
    public int PlaqueTagOffsetY { get; set; }
    public float TooltipPulseSpeed { get; set; }
    public float TooltipPulseAmplitude { get; set; }

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
    public TutorialSnakeMotionConfig SnakeMotion { get; set; } = new();
    public TutorialLionMotionConfig LionMotion { get; set; } = new();
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
    public Vec2i ControlBoardPadding { get; set; }
    public int ControlBoardTitleSpacing { get; set; }
    public int ControlBoardLineSpacing { get; set; }
    public int ControlBoardAttackSpacing { get; set; }
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
    public TutorialTargetRenderConfig TargetRender { get; set; } = new();

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
