using System.Drawing;
using Microsoft.Extensions.Configuration;

namespace AnimalBattle.Presentation.Rendering;

/// <summary>
///     Config/ui.render.toml から読み込まれる UI・ビューポート・レティクル・ダイアログ描画設定ルートクラス。
/// </summary>
public sealed class UiRenderConfig
{
    /// <summary>ビューポートおよびカリング領域設定</summary>
    public ViewportRenderConfig Viewport { get; set; } = new();

    /// <summary>左上 HUD 描画設定</summary>
    public HudRenderConfig Hud { get; set; } = new();

    /// <summary>タクティカルエイムレティクル：照準描画設定</summary>
    public AimReticleRenderConfig AimReticle { get; set; } = new();

    /// <summary>モーダルダイアログ描画設定</summary>
    public ModalRenderConfig Modals { get; set; } = new();
}

/// <summary>
///     ビューポートおよびカメラカリング領域設定クラス。
/// </summary>
public sealed class ViewportRenderConfig
{
    /// <summary>ビューポート左上座標ピクセル単位</summary>
    public Vec2f Pos { get; set; }

    /// <summary>ビューポートサイズピクセル単位</summary>
    public Vec2f Size { get; set; }

    private Color _platformFallbackFillColor;

    /// <summary>タイルマップ未指定時のプラットフォーム代替塗りつぶし色 Hex 文字列</summary>
    [ConfigurationKeyName("PlatformFallbackFillColor")]
    public string PlatformFallbackFillColorHex
    {
        get => string.Empty;
        set { _platformFallbackFillColor = value.ToColor(); }
    }

    /// <summary>タイルマップ未指定時のプラットフォーム代替塗りつぶし色</summary>
    public Color PlatformFallbackFillColor => _platformFallbackFillColor;

    private Color _platformFallbackBorderColor;

    /// <summary>タイルマップ未指定時のプラットフォーム代替枠線色 Hex 文字列</summary>
    [ConfigurationKeyName("PlatformFallbackBorderColor")]
    public string PlatformFallbackBorderColorHex
    {
        get => string.Empty;
        set { _platformFallbackBorderColor = value.ToColor(); }
    }

    /// <summary>タイルマップ未指定時のプラットフォーム代替枠線色</summary>
    public Color PlatformFallbackBorderColor => _platformFallbackBorderColor;
}

/// <summary>
///     左上 HUD 操作案内表示の設定クラス。
/// </summary>
public sealed class HudRenderConfig
{
    /// <summary>タイトル表示座標</summary>
    public Vec2i TitlePos { get; set; }

    /// <summary>行間隔ピクセル</summary>
    public int LineSpacing { get; set; }

    /// <summary>Boss 切り替え操作案内テキスト</summary>
    public string BossSwitchGuideText { get; set; } = string.Empty;

    private Color _bossSwitchGuideColor;

    /// <summary>Boss 切り替え操作案内文字色 Hex 文字列</summary>
    [ConfigurationKeyName("BossSwitchGuideColor")]
    public string BossSwitchGuideColorHex
    {
        get => string.Empty;
        set { _bossSwitchGuideColor = value.ToColor(); }
    }

    /// <summary>Boss 切り替え操作案内文字色 <see cref="Color" /></summary>
    public Color BossSwitchGuideColor => _bossSwitchGuideColor;

    /// <summary>キャラクター切り替え操作案内テキスト</summary>
    public string CharacterSwitchGuideText { get; set; } = string.Empty;

    private Color _characterSwitchGuideColor;

    /// <summary>キャラクター切り替え操作案内文字色 Hex 文字列</summary>
    [ConfigurationKeyName("CharacterSwitchGuideColor")]
    public string CharacterSwitchGuideColorHex
    {
        get => string.Empty;
        set { _characterSwitchGuideColor = value.ToColor(); }
    }

    /// <summary>キャラクター切り替え操作案内文字色 <see cref="Color" /></summary>
    public Color CharacterSwitchGuideColor => _characterSwitchGuideColor;

    /// <summary>二段ジャンプインジケータ表示座標</summary>
    public Vec2i DoubleJumpPos { get; set; }

    /// <summary>二段ジャンプ表示ラベル文字列</summary>
    public string DoubleJumpText { get; set; } = string.Empty;

    private Color _doubleJumpActiveColor;

    /// <summary>二段ジャンプ有効時シンボル色 Hex 文字列</summary>
    [ConfigurationKeyName("DoubleJumpActiveColor")]
    public string DoubleJumpActiveColorHex
    {
        get => string.Empty;
        set { _doubleJumpActiveColor = value.ToColor(); }
    }

    /// <summary>二段ジャンプ有効時シンボル色 <see cref="Color" /></summary>
    public Color DoubleJumpActiveColor => _doubleJumpActiveColor;

    private Color _doubleJumpInactiveColor;

    /// <summary>二段ジャンプ消費時シンボル色 Hex 文字列</summary>
    [ConfigurationKeyName("DoubleJumpInactiveColor")]
    public string DoubleJumpInactiveColorHex
    {
        get => string.Empty;
        set { _doubleJumpInactiveColor = value.ToColor(); }
    }

    /// <summary>二段ジャンプ消費時シンボル色 <see cref="Color" /></summary>
    public Color DoubleJumpInactiveColor => _doubleJumpInactiveColor;

    /// <summary>プレイヤー HP 表示座標</summary>
    public Vec2i PlayerHpPos { get; set; }

    /// <summary>トレーニングモード案内タイトル表示座標</summary>
    public Vec2i TrainingModeTitlePos { get; set; } = new(460, 20);

    /// <summary>トレーニングモード案内タイトルテキスト</summary>
    public string TrainingModeTitleText { get; set; } = string.Empty;

    private Color _trainingModeTitleColor;

    /// <summary>トレーニングモード案内タイトル色 Hex 文字列</summary>
    [ConfigurationKeyName("TrainingModeTitleColor")]
    public string TrainingModeTitleColorHex
    {
        get => string.Empty;
        set { _trainingModeTitleColor = value.ToColor(); }
    }

    /// <summary>トレーニングモード案内タイトル色 <see cref="Color" /></summary>
    public Color TrainingModeTitleColor => _trainingModeTitleColor;

    /// <summary>トレーニングモード案内ガイド表示座標</summary>
    public Vec2i TrainingModeGuidePos { get; set; } = new(460, 38);

    /// <summary>トレーニングモード案内ガイドテキスト</summary>
    public string TrainingModeGuideText { get; set; } = string.Empty;

    private Color _trainingModeGuideColor;

    /// <summary>トレーニングモード案内テキスト色 Hex 文字列</summary>
    [ConfigurationKeyName("TrainingModeGuideColor")]
    public string TrainingModeGuideColorHex
    {
        get => string.Empty;
        set { _trainingModeGuideColor = value.ToColor(); }
    }

    /// <summary>トレーニングモード案内テキスト色 <see cref="Color" /></summary>
    public Color TrainingModeGuideColor => _trainingModeGuideColor;
}

/// <summary>
///     タクティカルエイムレティクル：鼠标准星描画設定クラス。
/// </summary>
public sealed class AimReticleRenderConfig
{
    /// <summary>レティクル外周円環の基準半径 ピクセル単位</summary>
    public float BaseRadius { get; set; }

    /// <summary>射撃開火時の最大スプリング拡張振幅 ピクセル単位</summary>
    public float RecoilExpansion { get; set; }

    /// <summary>スプリング反動復元周波数</summary>
    public float SpringFrequency { get; set; }

    /// <summary>スプリング減衰比 ゼロで無減衰、1.0で臨界減衰を示します</summary>
    public float SpringDampingRatio { get; set; }

    /// <summary>上下左右 4 方向刻度ショートラインの長さ ピクセル単位</summary>
    public int TickLength { get; set; }

    /// <summary>円環と刻度線の間のギャップ幅 ピクセル単位</summary>
    public int TickGap { get; set; }

    /// <summary>中心照準ドット半径 ピクセル単位</summary>
    public int CenterDotRadius { get; set; }

    private Color _centerDotColor;

    /// <summary>中心照準ドット表示色 Hex 文字列</summary>
    [ConfigurationKeyName("CenterDotColor")]
    public string CenterDotColorHex
    {
        get => string.Empty;
        set { _centerDotColor = value.ToColor(); }
    }

    /// <summary>中心照準ドット表示色 <see cref="Color" /></summary>
    public Color CenterDotColor => _centerDotColor;

    private Color _ringColor;

    /// <summary>外周タクティカルリング表示色 Hex 文字列</summary>
    [ConfigurationKeyName("RingColor")]
    public string RingColorHex
    {
        get => string.Empty;
        set { _ringColor = value.ToColor(); }
    }

    /// <summary>外周タクティカルリング表示色 <see cref="Color" /></summary>
    public Color RingColor => _ringColor;

    private Color _tickColor;

    /// <summary>上下左右刻度ショートライン表示色 Hex 文字列</summary>
    [ConfigurationKeyName("TickColor")]
    public string TickColorHex
    {
        get => string.Empty;
        set { _tickColor = value.ToColor(); }
    }

    /// <summary>上下左右刻度ショートライン表示色 <see cref="Color" /></summary>
    public Color TickColor => _tickColor;

    /// <summary>照準リング最小半径クランプ ピクセル単位</summary>
    public int MinRadius { get; set; }

    /// <summary>照準リング最大半径クランプ ピクセル単位</summary>
    public int MaxRadius { get; set; }

    /// <summary>反動拡張上限倍率</summary>
    public float RecoilExpansionClampRatio { get; set; }
}

/// <summary>
///     敗北 および勝利 モーダルダイアログの描画設定クラス。
/// </summary>
public sealed class ModalRenderConfig
{
    /// <summary>Game Over 矩形左上座標</summary>
    public Vec2i GameOverPos1 { get; set; }

    /// <summary>Game Over 矩形右下座標</summary>
    public Vec2i GameOverPos2 { get; set; }

    private Color _gameOverBgColor;

    /// <summary>Game Over 背景色 Hex 文字列</summary>
    [ConfigurationKeyName("GameOverBgColor")]
    public string GameOverBgColorHex
    {
        get => string.Empty;
        set { _gameOverBgColor = value.ToColor(); }
    }

    /// <summary>Game Over 背景色 <see cref="Color" /></summary>
    public Color GameOverBgColor => _gameOverBgColor;

    private Color _gameOverOuterBorderColor;

    /// <summary>Game Over 外枠線色 Hex 文字列</summary>
    [ConfigurationKeyName("GameOverOuterBorderColor")]
    public string GameOverOuterBorderColorHex
    {
        get => string.Empty;
        set { _gameOverOuterBorderColor = value.ToColor(); }
    }

    /// <summary>Game Over 外枠線色 <see cref="Color" /></summary>
    public Color GameOverOuterBorderColor => _gameOverOuterBorderColor;

    private Color _gameOverInnerBorderColor;

    /// <summary>Game Over 内枠線色 Hex 文字列</summary>
    [ConfigurationKeyName("GameOverInnerBorderColor")]
    public string GameOverInnerBorderColorHex
    {
        get => string.Empty;
        set { _gameOverInnerBorderColor = value.ToColor(); }
    }

    /// <summary>Game Over 内枠線色 <see cref="Color" /></summary>
    public Color GameOverInnerBorderColor => _gameOverInnerBorderColor;

    /// <summary>Game Over タイトル表示座標</summary>
    public Vec2i GameOverTitlePos { get; set; }

    private Color _gameOverTitleColor;

    /// <summary>Game Over タイトル文字色 Hex 文字列</summary>
    [ConfigurationKeyName("GameOverTitleColor")]
    public string GameOverTitleColorHex
    {
        get => string.Empty;
        set { _gameOverTitleColor = value.ToColor(); }
    }

    /// <summary>Game Over タイトル文字色 <see cref="Color" /></summary>
    public Color GameOverTitleColor => _gameOverTitleColor;

    /// <summary>Game Over タイトル文字列</summary>
    public string GameOverTitleText { get; set; } = string.Empty;

    /// <summary>Game Over リトライテキスト表示座標</summary>
    public Vec2i GameOverRetryPos { get; set; }

    private Color _gameOverRetryColor;

    /// <summary>Game Over リトライ文字色 Hex 文字列</summary>
    [ConfigurationKeyName("GameOverRetryColor")]
    public string GameOverRetryColorHex
    {
        get => string.Empty;
        set { _gameOverRetryColor = value.ToColor(); }
    }

    /// <summary>Game Over リトライ文字色 <see cref="Color" /></summary>
    public Color GameOverRetryColor => _gameOverRetryColor;

    /// <summary>Game Over リトライ案内文字列</summary>
    public string GameOverRetryText { get; set; } = string.Empty;

    /// <summary>Victory 矩形左上座標</summary>
    public Vec2i VictoryPos1 { get; set; }

    /// <summary>Victory 矩形右下座標</summary>
    public Vec2i VictoryPos2 { get; set; }

    private Color _victoryBgColor;

    /// <summary>Victory 背景色 Hex 文字列</summary>
    [ConfigurationKeyName("VictoryBgColor")]
    public string VictoryBgColorHex
    {
        get => string.Empty;
        set { _victoryBgColor = value.ToColor(); }
    }

    /// <summary>Victory 背景色 <see cref="Color" /></summary>
    public Color VictoryBgColor => _victoryBgColor;

    private Color _victoryOuterBorderColor;

    /// <summary>Victory 外枠線色 Hex 文字列</summary>
    [ConfigurationKeyName("VictoryOuterBorderColor")]
    public string VictoryOuterBorderColorHex
    {
        get => string.Empty;
        set { _victoryOuterBorderColor = value.ToColor(); }
    }

    /// <summary>Victory 外枠線色 <see cref="Color" /></summary>
    public Color VictoryOuterBorderColor => _victoryOuterBorderColor;

    private Color _victoryInnerBorderColor;

    /// <summary>Victory 内枠線色 Hex 文字列</summary>
    [ConfigurationKeyName("VictoryInnerBorderColor")]
    public string VictoryInnerBorderColorHex
    {
        get => string.Empty;
        set { _victoryInnerBorderColor = value.ToColor(); }
    }

    /// <summary>Victory 内枠線色 <see cref="Color" /></summary>
    public Color VictoryInnerBorderColor => _victoryInnerBorderColor;

    /// <summary>Victory タイトル表示座標</summary>
    public Vec2i VictoryTitlePos { get; set; }

    private Color _victoryTitleColor;

    /// <summary>Victory タイトル文字色 Hex 文字列</summary>
    [ConfigurationKeyName("VictoryTitleColor")]
    public string VictoryTitleColorHex
    {
        get => string.Empty;
        set { _victoryTitleColor = value.ToColor(); }
    }

    /// <summary>Victory タイトル文字色 <see cref="Color" /></summary>
    public Color VictoryTitleColor => _victoryTitleColor;

    /// <summary>Victory タイトル文字列</summary>
    public string VictoryTitleText { get; set; } = string.Empty;

    /// <summary>Victory リトライテキスト表示座標</summary>
    public Vec2i VictoryRetryPos { get; set; }

    private Color _victoryRetryColor;

    /// <summary>Victory リトライ文字色 Hex 文字列</summary>
    [ConfigurationKeyName("VictoryRetryColor")]
    public string VictoryRetryColorHex
    {
        get => string.Empty;
        set { _victoryRetryColor = value.ToColor(); }
    }

    /// <summary>Victory リトライ文字色 <see cref="Color" /></summary>
    public Color VictoryRetryColor => _victoryRetryColor;

    /// <summary>Victory リトライ案内文字列</summary>
    public string VictoryRetryText { get; set; } = string.Empty;

    /// <summary>モーダルダイアログ内枠線のパディング ピクセル単位</summary>
    public int InnerBorderPadding { get; set; }
}
