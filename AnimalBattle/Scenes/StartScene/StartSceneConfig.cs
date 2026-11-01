using System.Drawing;
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
///     チェックボックス矩形領域設定：位置・全体サイズ・チェック枠サイズ。
/// </summary>
public sealed class StartSceneCheckboxLayoutConfig {
    public Vec2i Pos { get; set; }
    public Vec2i Size { get; set; }
    public int BoxSize { get; set; }

    public Rectangle ToRectangle() => new(Pos.X, Pos.Y, Size.X, Size.Y);
}

/// <summary>
///     StartScene の動作オプション設定クラス。
/// </summary>
public sealed class StartSceneOptionsConfig {
    public bool DefaultEnableTutorial { get; set; }
}

/// <summary>
///     カード個別のテキスト設定：タイトル・特徴タグ・説明文。
/// </summary>
public sealed class StartSceneCardTextConfig {
    public string Title { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
}

/// <summary>
///     StartScene の UI テキスト設定クラス。
/// </summary>
public sealed class StartSceneTextsConfig {
    public string HeaderTitle { get; set; } = string.Empty;
    public string PlayerHeader { get; set; } = string.Empty;
    public string HintText { get; set; } = string.Empty;
    public string StartGameButton { get; set; } = string.Empty;
    public string TutorialCheckbox { get; set; } = string.Empty;
    public string SelectedBadge { get; set; } = string.Empty;
    public string UnselectedBadge { get; set; } = string.Empty;

    public StartSceneCardTextConfig Cat { get; set; } = new();
    public StartSceneCardTextConfig Bunny { get; set; } = new();
}

/// <summary>
///     StartScene のマウスホバー演出および物理アニメーション定数設定クラス。
/// </summary>
public sealed class StartSceneMotionConfig {
    public float StepFrequency { get; set; }
    public float StepAmplitude { get; set; }
}

/// <summary>
///     StartScene の画面レイアウト設定クラス。
/// </summary>
public sealed class StartSceneLayoutConfig {
    public CardLayoutConfig CatCard { get; set; } = new();
    public CardLayoutConfig BunnyCard { get; set; } = new();
    public CardLayoutConfig StartGameButton { get; set; } = new();
    public StartSceneCheckboxLayoutConfig TutorialCheckbox { get; set; } = new();

    public Vec2i HeaderTitlePos { get; set; }
    public Vec2i PlayerHeaderPos { get; set; }
    public Vec2i HintTextPos { get; set; }

    public Vec2i CardTitleOffset { get; set; }
    public Vec2i CardTagOffset { get; set; }
    public int CardBadgeBottomOffset { get; set; }
    public int CardBadgeLeftOffset { get; set; }
    public int PreviewCenterOffsetY { get; set; }
}

/// <summary>
///     StartScene の配色パレット設定クラス。
/// </summary>
public sealed class StartSceneColorsConfig {
    private Color _cardBgColor;
    [ConfigurationKeyName("CardBgColor")]
    public string CardBgColorHex
    {
        get => string.Empty;
        set { _cardBgColor = value.ToColor(); }
    }
    public Color CardBgColor => _cardBgColor;

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
    public StartSceneOptionsConfig Options { get; set; } = new();
}
