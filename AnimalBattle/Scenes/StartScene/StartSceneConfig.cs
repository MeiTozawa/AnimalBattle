using System.Drawing;
using AnimalBattle.Core;
using Microsoft.Extensions.Configuration;

namespace AnimalBattle.Scenes.Start;

/// <summary>
///     カード矩形領域設定：位置・サイズ。
/// </summary>
public sealed class CardLayoutConfig {
    public Vec2i Pos { get; set; }
    public Vec2i Size { get; set; }

    public Rectangle ToRectangle() => new(Pos.X, Pos.Y, Size.X, Size.Y);
}

/// <summary>
///     カード個別のテキスト設定：タイトル・特徴タグ・説明文。
/// </summary>
public sealed class StartSceneCardTextConfig {
    public string Title { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
///     StartScene の UI テキスト設定クラス。
/// </summary>
public sealed class StartSceneTextsConfig {
    public string HeaderTitle { get; set; } = string.Empty;
    public string PlayerHeader { get; set; } = string.Empty;
    public string BossHeader { get; set; } = string.Empty;
    public string HintText { get; set; } = string.Empty;
    public string ConfirmButton { get; set; } = string.Empty;
    public string SelectedBadge { get; set; } = string.Empty;
    public string UnselectedBadge { get; set; } = string.Empty;

    public StartSceneCardTextConfig Cat { get; set; } = new();
    public StartSceneCardTextConfig Bunny { get; set; } = new();
    public StartSceneCardTextConfig Boss1 { get; set; } = new();
    public StartSceneCardTextConfig Boss2 { get; set; } = new();
}

/// <summary>
///     StartScene のマウスホバー演出および物理アニメーション定数設定クラス。
/// </summary>
public sealed class StartSceneMotionConfig {
    public float StepFrequency { get; set; }
    public float StepAmplitude { get; set; }

    public float Boss1FlyFrequency { get; set; }
    public float Boss1FlyAmplitude { get; set; }
    public float Boss1FlyOffsetY { get; set; }
    public float Boss1TiltFrequency { get; set; }
    public float Boss1TiltAmplitude { get; set; }
    public double Boss1Scale { get; set; }
    public float Boss1FlyDecaySpeed { get; set; }

    public float Boss2SlamCycleDuration { get; set; }
    public float Boss2RiseDuration { get; set; }
    public float Boss2RiseHeight { get; set; }
    public double Boss2RiseScale { get; set; }
    public float Boss2SlamDuration { get; set; }
    public double Boss2SlamScale { get; set; }
    public float Boss2ImpactDuration { get; set; }
    public float Boss2ImpactShakeFreq { get; set; }
    public float Boss2ImpactShakeAmplitude { get; set; }
    public double Boss2BaseScale { get; set; }
}

/// <summary>
///     StartScene の画面レイアウト設定クラス。
/// </summary>
public sealed class StartSceneLayoutConfig {
    public CardLayoutConfig CatCard { get; set; } = new();
    public CardLayoutConfig BunnyCard { get; set; } = new();
    public CardLayoutConfig Boss1Card { get; set; } = new();
    public CardLayoutConfig Boss2Card { get; set; } = new();
    public CardLayoutConfig ConfirmButton { get; set; } = new();

    public Vec2i HeaderTitlePos { get; set; }
    public Vec2i PlayerHeaderPos { get; set; }
    public Vec2i BossHeaderPos { get; set; }
    public Vec2i HintTextPos { get; set; }

    public Vec2i ConfirmBtnTextOffset { get; set; }

    public Vec2i CardTitleOffset { get; set; }
    public Vec2i CardTagOffset { get; set; }
    public int CardBadgeBottomOffset { get; set; }
    public int CardBadgeLeftOffset { get; set; }
    public int CardDescBottomOffset { get; set; }
    public int CardDescLeftOffset { get; set; }
    public int PreviewCenterOffsetY { get; set; }
}

/// <summary>
///     StartScene の配色パレット設定クラス。
/// </summary>
public sealed class StartSceneColorsConfig {
    private Color _bgColor;
    [ConfigurationKeyName("BgColor")]
    public string BgColorHex
    {
        get => string.Empty;
        set { _bgColor = value.ToColor(); }
    }
    public Color BgColor => _bgColor;

    private Color _cardBgColor;
    [ConfigurationKeyName("CardBgColor")]
    public string CardBgColorHex
    {
        get => string.Empty;
        set { _cardBgColor = value.ToColor(); }
    }
    public Color CardBgColor => _cardBgColor;

    private Color _cardHoverBgColor;
    [ConfigurationKeyName("CardHoverBgColor")]
    public string CardHoverBgColorHex
    {
        get => string.Empty;
        set { _cardHoverBgColor = value.ToColor(); }
    }
    public Color CardHoverBgColor => _cardHoverBgColor;

    private Color _borderDefaultColor;
    [ConfigurationKeyName("BorderDefaultColor")]
    public string BorderDefaultColorHex
    {
        get => string.Empty;
        set { _borderDefaultColor = value.ToColor(); }
    }
    public Color BorderDefaultColor => _borderDefaultColor;

    private Color _playerSelectedColor;
    [ConfigurationKeyName("PlayerSelectedColor")]
    public string PlayerSelectedColorHex
    {
        get => string.Empty;
        set { _playerSelectedColor = value.ToColor(); }
    }
    public Color PlayerSelectedColor => _playerSelectedColor;

    private Color _bossSelectedColor;
    [ConfigurationKeyName("BossSelectedColor")]
    public string BossSelectedColorHex
    {
        get => string.Empty;
        set { _bossSelectedColor = value.ToColor(); }
    }
    public Color BossSelectedColor => _bossSelectedColor;

    private Color _confirmBtnDefaultColor;
    [ConfigurationKeyName("ConfirmBtnDefaultColor")]
    public string ConfirmBtnDefaultColorHex
    {
        get => string.Empty;
        set { _confirmBtnDefaultColor = value.ToColor(); }
    }
    public Color ConfirmBtnDefaultColor => _confirmBtnDefaultColor;

    private Color _confirmBtnHoverColor;
    [ConfigurationKeyName("ConfirmBtnHoverColor")]
    public string ConfirmBtnHoverColorHex
    {
        get => string.Empty;
        set { _confirmBtnHoverColor = value.ToColor(); }
    }
    public Color ConfirmBtnHoverColor => _confirmBtnHoverColor;

    private Color _confirmBtnBorderDefaultColor;
    [ConfigurationKeyName("ConfirmBtnBorderDefaultColor")]
    public string ConfirmBtnBorderDefaultColorHex
    {
        get => string.Empty;
        set { _confirmBtnBorderDefaultColor = value.ToColor(); }
    }
    public Color ConfirmBtnBorderDefaultColor => _confirmBtnBorderDefaultColor;

    private Color _confirmBtnBorderHoverColor;
    [ConfigurationKeyName("ConfirmBtnBorderHoverColor")]
    public string ConfirmBtnBorderHoverColorHex
    {
        get => string.Empty;
        set { _confirmBtnBorderHoverColor = value.ToColor(); }
    }
    public Color ConfirmBtnBorderHoverColor => _confirmBtnBorderHoverColor;

    private Color _textWhite;
    [ConfigurationKeyName("TextWhite")]
    public string TextWhiteHex
    {
        get => string.Empty;
        set { _textWhite = value.ToColor(); }
    }
    public Color TextWhite => _textWhite;

    private Color _textMuted;
    [ConfigurationKeyName("TextMuted")]
    public string TextMutedHex
    {
        get => string.Empty;
        set { _textMuted = value.ToColor(); }
    }
    public Color TextMuted => _textMuted;

    private Color _textGold;
    [ConfigurationKeyName("TextGold")]
    public string TextGoldHex
    {
        get => string.Empty;
        set { _textGold = value.ToColor(); }
    }
    public Color TextGold => _textGold;
}

/// <summary>
///     ゲーム開始・キャラクター選択画面 の全設定を管理するルート設定モデル。
/// </summary>
public sealed class StartSceneConfig {
    public StartSceneLayoutConfig Layout { get; set; } = new();
    public StartSceneColorsConfig Colors { get; set; } = new();
    public StartSceneTextsConfig Texts { get; set; } = new();
    public StartSceneMotionConfig Motion { get; set; } = new();
}
