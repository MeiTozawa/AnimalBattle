using System.Drawing;

namespace AnimalBattle.Configs;

/// <summary>
///     カード矩形領域設定（X, Y, Width, Height）。
/// </summary>
public sealed class CardLayoutConfig {
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public Rectangle ToRectangle() => new(X, Y, Width, Height);
}

/// <summary>
///     カード個別のテキスト設定（タイトル・特徴タグ・説明文）。
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
///     StartScene のマウスホバー動效・物理アニメーション定数設定クラス。
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

    public int HeaderTitleX { get; set; }
    public int HeaderTitleY { get; set; }
    public int PlayerHeaderX { get; set; }
    public int PlayerHeaderY { get; set; }
    public int BossHeaderX { get; set; }
    public int BossHeaderY { get; set; }
    public int HintTextX { get; set; }
    public int HintTextY { get; set; }

    public int ConfirmBtnTextOffsetX { get; set; }
    public int ConfirmBtnTextOffsetY { get; set; }

    public int CardTitleOffsetX { get; set; }
    public int CardTitleOffsetY { get; set; }
    public int CardTagOffsetX { get; set; }
    public int CardTagOffsetY { get; set; }
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
    public Color BgColor { get; set; }
    public Color CardBgColor { get; set; }
    public Color CardHoverBgColor { get; set; }
    public Color BorderDefaultColor { get; set; }
    public Color PlayerSelectedColor { get; set; }
    public Color BossSelectedColor { get; set; }
    public Color ConfirmBtnDefaultColor { get; set; }
    public Color ConfirmBtnHoverColor { get; set; }
    public Color ConfirmBtnBorderDefaultColor { get; set; }
    public Color ConfirmBtnBorderHoverColor { get; set; }
    public Color TextWhite { get; set; }
    public Color TextMuted { get; set; }
    public Color TextGold { get; set; }
}

/// <summary>
///     ゲーム開始・選人シーン (StartScene) の全設定を管理するルート設定モデル。
/// </summary>
public sealed class StartSceneConfig {
    public StartSceneLayoutConfig Layout { get; set; } = new();
    public StartSceneColorsConfig Colors { get; set; } = new();
    public StartSceneTextsConfig Texts { get; set; } = new();
    public StartSceneMotionConfig Motion { get; set; } = new();
}
