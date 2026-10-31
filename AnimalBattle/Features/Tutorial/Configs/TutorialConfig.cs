using System.Drawing;
using Microsoft.Extensions.Configuration;

namespace AnimalBattle.Features.Tutorial.Configs;

/// <summary>
///     チュートリアル画面の配色パレット設定クラス。
/// </summary>
public sealed class TutorialColorsConfig
{
    private Color _gold = Color.FromArgb(255, 215, 80);
    [ConfigurationKeyName("Gold")]
    public string GoldHex { get => string.Empty; set => _gold = value.ToColor(); }
    public Color Gold => _gold;

    private Color _white = Color.FromArgb(240, 245, 255);
    [ConfigurationKeyName("White")]
    public string WhiteHex { get => string.Empty; set => _white = value.ToColor(); }
    public Color White => _white;

    private Color _cyan = Color.FromArgb(80, 210, 255);
    [ConfigurationKeyName("Cyan")]
    public string CyanHex { get => string.Empty; set => _cyan = value.ToColor(); }
    public Color Cyan => _cyan;

    private Color _red = Color.FromArgb(255, 100, 100);
    [ConfigurationKeyName("Red")]
    public string RedHex { get => string.Empty; set => _red = value.ToColor(); }
    public Color Red => _red;

    private Color _muted = Color.FromArgb(160, 170, 190);
    [ConfigurationKeyName("Muted")]
    public string MutedHex { get => string.Empty; set => _muted = value.ToColor(); }
    public Color Muted => _muted;

    private Color _panelBg = Color.FromArgb(20, 24, 36);
    [ConfigurationKeyName("PanelBg")]
    public string PanelBgHex { get => string.Empty; set => _panelBg = value.ToColor(); }
    public Color PanelBg => _panelBg;

    private Color _panelBorder = Color.FromArgb(55, 70, 95);
    [ConfigurationKeyName("PanelBorder")]
    public string PanelBorderHex { get => string.Empty; set => _panelBorder = value.ToColor(); }
    public Color PanelBorder => _panelBorder;

    private Color _doorBg = Color.FromArgb(26, 30, 44);
    [ConfigurationKeyName("DoorBg")]
    public string DoorBgHex { get => string.Empty; set => _doorBg = value.ToColor(); }
    public Color DoorBg => _doorBg;

    private Color _tooltipBg = Color.FromArgb(15, 30, 55);
    [ConfigurationKeyName("TooltipBg")]
    public string TooltipBgHex { get => string.Empty; set => _tooltipBg = value.ToColor(); }
    public Color TooltipBg => _tooltipBg;
}

/// <summary>
///     移動練習ターゲット設定。
/// </summary>
public sealed class TutorialTargetMovementConfig
{
    public Vec2f Target1Pos { get; set; } = new(250f, 520f);
    public Vec2f Target2Pos { get; set; } = new(850f, 520f);
    public float Radius { get; set; } = 24f;
}

/// <summary>
///     単一目標位置・半径設定。
/// </summary>
public sealed class TutorialSingleTargetConfig
{
    public Vec2f Pos { get; set; }
    public float Radius { get; set; }
}

/// <summary>
///     チュートリアルの全練習目標物設定。
/// </summary>
public sealed class TutorialTargetsConfig
{
    public TutorialTargetMovementConfig Movement { get; set; } = new();
    public TutorialSingleTargetConfig Jump { get; set; } = new() { Pos = new Vec2f(420f, 400f), Radius = 28f };
    public TutorialSingleTargetConfig DoubleJump { get; set; } = new() { Pos = new Vec2f(640f, 250f), Radius = 28f };
    public TutorialSingleTargetConfig Dash { get; set; } = new() { Pos = new Vec2f(830f, 200f), Radius = 34f };
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
}

/// <summary>
///     ボス挑戦扉システム全体設定。
/// </summary>
public sealed class TutorialDoorsConfig
{
    public Vec2i Size { get; set; } = new(150, 200);
    public float InteractionRadius { get; set; } = 75f;
    public Vec2f PreviewScale { get; set; } = new(0.55f, 0.55f);
    public float PreviewBobFrequency { get; set; } = 4.5f;
    public float PreviewBobAmplitude { get; set; } = 6.0f;
    public int HighlightCircleRadius { get; set; } = 50;
    public int TooltipOffsetY { get; set; } = -25;
    public Vec2i TooltipSize { get; set; } = new(140, 28);

    public TutorialDoorItemConfig Snake { get; set; } = new()
    {
        Pos = new Vec2f(420f, 480f),
        BossType = "BossSnake",
        TexturePath = ResourcePath.Characters.Character031_3Png,
        Title = "【 BOSS: 蛇 】",
        Tag = "飛行 & 全方位弾幕"
    };

    public TutorialDoorItemConfig Lion { get; set; } = new()
    {
        Pos = new Vec2f(860f, 480f),
        BossType = "BossLion",
        TexturePath = ResourcePath.Characters.Character027_3Png,
        Title = "【 BOSS: 獅子 】",
        Tag = "突撃 & 衝撃波重撃"
    };
}

/// <summary>
///     UI画面レイアウト座標設定。
/// </summary>
public sealed class TutorialLayoutConfig
{
    public Vec2i GuidanceTitlePos { get; set; } = new(640, 70);
    public Vec2i GuidanceSubPos { get; set; } = new(640, 102);
    public Vec2i RetryPos { get; set; } = new(640, 360);
    public Vec2i ControlBoardP1 { get; set; } = new(960, 25);
    public Vec2i ControlBoardP2 { get; set; } = new(1255, 155);
}

/// <summary>
///     タイミングおよび物理パラメータ設定。
/// </summary>
public sealed class TutorialTimingConfig
{
    public float TargetCollectDelay { get; set; } = 0.4f;
    public float MinionDefeatDelay { get; set; } = 0.6f;
    public float RetryDuration { get; set; } = 1.2f;
    public float TargetFloatSpeed { get; set; } = 3.5f;
    public float TargetFloatAmplitude { get; set; } = 5.0f;
    public float ReachMargin { get; set; } = 32.0f;
}

/// <summary>
///     フェーズ別タイトル・サブタイトル設定。
/// </summary>
public sealed class TutorialPhaseTextConfig
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
}

/// <summary>
///     チュートリアル全テキスト設定。
/// </summary>
public sealed class TutorialTextsConfig
{
    public TutorialPhaseTextConfig Movement { get; set; } = new()
    {
        Title = "【 A / D : 移動 】",
        Subtitle = "左右に配置されたコインに触れて回収してください"
    };

    public TutorialPhaseTextConfig Jump { get; set; } = new()
    {
        Title = "【 SPACE : ジャンプ 】",
        Subtitle = "スペースキーで跳躍し、コインを回収してください"
    };

    public TutorialPhaseTextConfig DoubleJump { get; set; } = new()
    {
        Title = "【 SPACE × 2 : 二段ジャンプ 】",
        Subtitle = "空中で再度スペースキーを押し、高所のコインを回収してください"
    };

    public TutorialPhaseTextConfig Dash { get; set; } = new()
    {
        Title = "【 L-SHIFT : ダッシュ 】",
        Subtitle = "足場からジャンプし、空中でダッシュしてリングを通過してください"
    };

    public TutorialPhaseTextConfig FlyingMinion { get; set; } = new()
    {
        Title = "★ 【 左クリック : 攻撃 】",
        Subtitle = "マウスで狙いを定め、空中を浮遊する小怪を撃破してください"
    };

    public TutorialPhaseTextConfig GroundMinion { get; set; } = new()
    {
        Title = "★ 【 左クリック : 攻撃 】 / 【 L-SHIFT : 回避 】",
        Subtitle = "突進してくる小怪を攻撃して撃破してください"
    };

    public TutorialPhaseTextConfig BossSelect { get; set; } = new()
    {
        Title = "【 ボス選択 】",
        Subtitle = "扉に近づき、[F] キーで挑戦するボスを選択してください"
    };

    public string RetryPrompt { get; set; } = "【 RETRY - リトライ 】";
    public string DoorInteractPrompt { get; set; } = "【 [F] 挑戦する 】";
    public string ControlBoardTitle { get; set; } = "【 基本操作ガイド 】";
    public string ControlBoardMove { get; set; } = "移動　　: A / D";
    public string ControlBoardJump { get; set; } = "ジャンプ: SPACE (空中で二段)";
    public string ControlBoardDash { get; set; } = "ダッシュ: L-SHIFT / 右クリック";
    public string ControlBoardAttack { get; set; } = "★ 攻撃 : 左クリック (照準射撃)";
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
public sealed class TutorialConfig
{
    public TutorialColorsConfig Colors { get; set; } = new();
    public TutorialTargetsConfig Targets { get; set; } = new();
    public TutorialDoorsConfig Doors { get; set; } = new();
    public TutorialLayoutConfig Layout { get; set; } = new();
    public TutorialTimingConfig Timing { get; set; } = new();
    public TutorialTextsConfig Texts { get; set; } = new();

    /// <summary>
    ///     データ駆動フェーズシーケンス一覧。
    /// </summary>
    public TutorialPhaseConfig[] Phases { get; set; } = [];
}
