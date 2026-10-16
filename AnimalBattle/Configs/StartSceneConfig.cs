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
///     StartScene の画面レイアウト設定クラス。
/// </summary>
public sealed class StartSceneLayoutConfig {
    public CardLayoutConfig CatCard { get; set; } = new() { X = 100, Y = 150, Width = 240, Height = 390 };
    public CardLayoutConfig BunnyCard { get; set; } = new() { X = 360, Y = 150, Width = 240, Height = 390 };
    public CardLayoutConfig Boss1Card { get; set; } = new() { X = 680, Y = 150, Width = 240, Height = 390 };
    public CardLayoutConfig Boss2Card { get; set; } = new() { X = 940, Y = 150, Width = 240, Height = 390 };
    public CardLayoutConfig ConfirmButton { get; set; } = new() { X = 490, Y = 580, Width = 300, Height = 60 };
}

/// <summary>
///     StartScene の配色パレット設定クラス。
/// </summary>
public sealed class StartSceneColorsConfig {
    public Color BgColor { get; set; } = Color.FromArgb(18, 20, 28);
    public Color CardBgColor { get; set; } = Color.FromArgb(28, 32, 45);
    public Color CardHoverBgColor { get; set; } = Color.FromArgb(38, 44, 62);
    public Color BorderDefaultColor { get; set; } = Color.FromArgb(70, 78, 100);
    public Color PlayerSelectedColor { get; set; } = Color.FromArgb(50, 200, 240);
    public Color BossSelectedColor { get; set; } = Color.FromArgb(240, 80, 80);
    public Color ConfirmBtnDefaultColor { get; set; } = Color.FromArgb(35, 75, 120);
    public Color ConfirmBtnHoverColor { get; set; } = Color.FromArgb(50, 130, 210);
    public Color TextWhite { get; set; } = Color.FromArgb(240, 245, 255);
    public Color TextMuted { get; set; } = Color.FromArgb(160, 170, 190);
    public Color TextGold { get; set; } = Color.FromArgb(255, 215, 80);
}

/// <summary>
///     ゲーム開始・選人シーン (StartScene) の全設定を管理するルート設定モデル。
/// </summary>
public sealed class StartSceneConfig {
    public StartSceneLayoutConfig Layout { get; set; } = new();
    public StartSceneColorsConfig Colors { get; set; } = new();
}
