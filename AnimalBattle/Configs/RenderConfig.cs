using System.Drawing;
using AnimalBattle.Core;

namespace AnimalBattle.Configs;

/// <summary>
/// Boss の攻撃予兆・無敵シールド等の描画エフェクト設定クラス。
/// </summary>
public sealed class BossEffectsRenderConfig
{
    /// <summary>危険判定矩形 (Danger Box) の半透明アルファ値 (0～255)</summary>
    public int DangerBoxAlpha { get; set; }

    /// <summary>照準レーザー (Aiming Laser) の半透明アルファ値 (0～255)</summary>
    public int LaserAlpha { get; set; }

    /// <summary>突進軌道レーン (Charge Track) の半透明アルファ値 (0～255)</summary>
    public int ChargeTrackAlpha { get; set; }

    /// <summary>警告テキスト・マーカー表示色 (<see cref="Color"/>)</summary>
    public Color WarningTextColor { get; set; }

    /// <summary>照準レーザー線の太さ (px)</summary>
    public int LaserThickness { get; set; }

    /// <summary>俯衝突進（DiveRam）予兆テキストの X オフセット (px)</summary>
    public float DiveRamTextOffsetX { get; set; }

    /// <summary>俯衝突進（DiveRam）予兆テキストの Y オフセット (px)</summary>
    public float DiveRamTextOffsetY { get; set; }

    /// <summary>泰山圧頂（LeapSmash）予兆テキストの X オフセット (px)</summary>
    public float LeapSmashTextOffsetX { get; set; }

    /// <summary>泰山圧頂（LeapSmash）予兆テキストの Y オフセット (px)</summary>
    public float LeapSmashTextOffsetY { get; set; }

    /// <summary>撼地踏撃（Stomp）予兆テキストの X オフセット (px)</summary>
    public float StompTextOffsetX { get; set; }

    /// <summary>撼地踏撃（Stomp）予兆テキストの Y オフセット (px)</summary>
    public float StompTextOffsetY { get; set; }

    /// <summary>落雨落石（Skyfall）予兆テキストの X オフセット (px)</summary>
    public float SkyfallTextOffsetX { get; set; }

    /// <summary>落雨落石（Skyfall）予兆テキストの Y オフセット (px)</summary>
    public float SkyfallTextOffsetY { get; set; }

    /// <summary>易傷・隙あり（Vulnerable）時の醒目攻撃チャンス表示テキスト</summary>
    public string ChanceMarkerText { get; set; } = string.Empty;

    /// <summary>易傷時の醒目攻撃チャンス表示 Y 軸オフセット (px)</summary>
    public float ChanceMarkerOffsetY { get; set; }

    /// <summary>易傷時の醒目攻撃チャンス表示色 (<see cref="Color"/>)</summary>
    public Color ChanceMarkerColor { get; set; }

    /// <summary>Boss 浮遊時の揺れ角度振幅</summary>
    public double BossHoverAngleAmp { get; set; }


    /// <summary>Boss 浮遊時の揺れ角速度</summary>
    public float BossHoverAngleSpeed { get; set; }
}

/// <summary>
/// 銃弾および弾幕の描画設定クラス。
/// </summary>
public sealed class BulletRenderConfig
{
    /// <summary>プレイヤー弾外層グロー半径</summary>
    public float PlayerBulletOuterRadius { get; set; }

    /// <summary>プレイヤー弾外層グロー色 (<see cref="Color"/>)</summary>
    public Color PlayerBulletOuterColor { get; set; }

    /// <summary>プレイヤー弾内層コア半径</summary>
    public float PlayerBulletInnerRadius { get; set; }

    /// <summary>プレイヤー弾内層コア色 (<see cref="Color"/>)</summary>
    public Color PlayerBulletInnerColor { get; set; }

    /// <summary>プレイヤー弾ビームトレイル半幅</summary>
    public int PlayerBulletTrailHalfWidth { get; set; }

    /// <summary>プレイヤー弾ビームトレイル半高</summary>
    public int PlayerBulletTrailHalfHeight { get; set; }

    /// <summary>プレイヤー弾ビームトレイル色 (<see cref="Color"/>)</summary>
    public Color PlayerBulletTrailColor { get; set; }

    /// <summary>Boss 弾内層ホワイトコア半径比率</summary>
    public float BossBulletCoreRadiusRatio { get; set; }

    /// <summary>Boss 弾内層ホワイトコア最小ピクセル半径</summary>
    public int BossBulletCoreMinRadius { get; set; }

    /// <summary>Boss 弾内層ホワイトコア色 (<see cref="Color"/>)</summary>
    public Color BossBulletCoreColor { get; set; }

    /// <summary>Boss 弾外枠付加ピクセル半径</summary>
    public int BossBulletOutlinePadding { get; set; }

    /// <summary>画面外カリング（描画除外）マージン X (ピクセル)</summary>
    public int ViewportCullingMarginX { get; set; }

    /// <summary>画面外カリング（描画除外）マージン Y (ピクセル)</summary>
    public int ViewportCullingMarginY { get; set; }
}

/// <summary>
/// 左上 HUD 操作案内テキストの 1 行（テキストと色）を表す設定クラス。
/// </summary>
public sealed class HudTextItem
{
    /// <summary>案内テキスト文字列</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>文字色 (<see cref="Color"/>)</summary>
    public Color Color { get; set; }
}

/// <summary>
/// 左上 HUD 操作案内表示の設定クラス。
/// </summary>
public sealed class HudRenderConfig
{
    /// <summary>タイトル X 座標</summary>
    public int TitleX { get; set; }

    /// <summary>タイトル Y 座標</summary>
    public int TitleY { get; set; }

    /// <summary>タイトル文字色 (<see cref="Color"/>)</summary>
    public Color TitleColor { get; set; }

    /// <summary>タイトル文字列</summary>
    public string TitleText { get; set; } = string.Empty;

    /// <summary>行間隔ピクセル</summary>
    public int LineSpacing { get; set; }

    /// <summary>
    /// 操作案内テキスト項目のリスト。
    /// </summary>
    public List<HudTextItem> Text { get; set; } =
    [
        new() { Text = "[A/D/Left/Right]: Move (Instant Stop)", Color = ColorPalette.GrayLight },
        new() { Text = "[Space/K]: Jump & Air Double Jump", Color = ColorPalette.GrayLight },
        new() { Text = "[J/Z] + Direction: 8-Way Aim & Shoot", Color = ColorPalette.GrayLight },
        new() { Text = "[R]: Instant Restart (1-Frame Reset)", Color = ColorPalette.RedCoral },
        new() { Text = "[F1/H]: Toggle Hitbox/Hurtbox Debug", Color = ColorPalette.GrayLight },
    ];

    /// <summary>二段ジャンプインジケータ Y 座標</summary>
    public int DoubleJumpY { get; set; }

    /// <summary>二段ジャンプ有効時シンボル色 (<see cref="Color"/>)</summary>
    public Color DoubleJumpActiveColor { get; set; }

    /// <summary>二段ジャンプ消費時シンボル色 (<see cref="Color"/>)</summary>
    public Color DoubleJumpInactiveColor { get; set; }
}

/// <summary>
/// 画面下部 Boss HP バーおよび状態表示の設定クラス。
/// </summary>
public sealed class BossHpBarRenderConfig
{
    /// <summary>HP バー左上 X 座標</summary>
    public int BarX1 { get; set; }

    /// <summary>HP バー左上 Y 座標</summary>
    public int BarY1 { get; set; }

    /// <summary>HP バー右下 X 座標</summary>
    public int BarX2 { get; set; }

    /// <summary>HP バー右下 Y 座標</summary>
    public int BarY2 { get; set; }

    /// <summary>Phase 1 HP ゲージ色 (<see cref="Color"/>)</summary>
    public Color Phase1Color { get; set; }

    /// <summary>Phase 2 HP ゲージ色 (<see cref="Color"/>)</summary>
    public Color Phase2Color { get; set; }

    /// <summary>Phase 3 HP ゲージ色 (<see cref="Color"/>)</summary>
    public Color Phase3Color { get; set; }

    /// <summary>HP バー背景外枠パディング</summary>
    public int BgPadding { get; set; }

    /// <summary>HP バー暗色背景色 (<see cref="Color"/>)</summary>
    public Color BgColor { get; set; }

    /// <summary>HP バー外枠線色 (<see cref="Color"/>)</summary>
    public Color BorderColor { get; set; }

    /// <summary>HP バー上面ハイライト厚み</summary>
    public int TopHighlightThickness { get; set; }

    /// <summary>HP バー上面ハイライト色 (<see cref="Color"/>)</summary>
    public Color TopHighlightColor { get; set; }

    /// <summary>ヘッダー文字の Y 座標オフセット</summary>
    public int HeaderOffsetY { get; set; }

    /// <summary>Boss タイトル文字色 (<see cref="Color"/>)</summary>
    public Color TitleColor { get; set; }

    /// <summary>HP 数値文字色 (<see cref="Color"/>)</summary>
    public Color HpTextColor { get; set; }

    /// <summary>状態バッジテキストの X 軸オフセット</summary>
    public int StateBadgeOffsetX { get; set; }

    /// <summary>前隙予兆状態バッジ文字色 (<see cref="Color"/>)</summary>
    public Color StateTelegraphingColor { get; set; }

    /// <summary>発動中状態バッジ文字色 (<see cref="Color"/>)</summary>
    public Color StateCastingColor { get; set; }

    /// <summary>隙あり虚弱状態バッジ文字色 (<see cref="Color"/>)</summary>
    public Color StateRecoveringColor { get; set; }

    /// <summary>フェーズ遷移演出状態バッジ文字色 (<see cref="Color"/>)</summary>
    public Color StatePhaseTransitionColor { get; set; }

    /// <summary>通常巡航状態バッジ文字色 (<see cref="Color"/>)</summary>
    public Color StateDefaultColor { get; set; }
}

/// <summary>
/// 敗北 (Game Over) および勝利 (Victory) モーダルダイアログの描画設定クラス。
/// </summary>
public sealed class ModalRenderConfig
{
    /// <summary>Game Over 矩形左上 X 座標</summary>
    public int GameOverX1 { get; set; }

    /// <summary>Game Over 矩形左上 Y 座標</summary>
    public int GameOverY1 { get; set; }

    /// <summary>Game Over 矩形右下 X 座標</summary>
    public int GameOverX2 { get; set; }

    /// <summary>Game Over 矩形右下 Y 座標</summary>
    public int GameOverY2 { get; set; }

    /// <summary>Game Over 背景色 (<see cref="Color"/>)</summary>
    public Color GameOverBgColor { get; set; }

    /// <summary>Game Over 外枠線色 (<see cref="Color"/>)</summary>
    public Color GameOverOuterBorderColor { get; set; }

    /// <summary>Game Over 内枠線色 (<see cref="Color"/>)</summary>
    public Color GameOverInnerBorderColor { get; set; }

    /// <summary>Game Over タイトル X 座標</summary>
    public int GameOverTitleX { get; set; }

    /// <summary>Game Over タイトル Y 座標</summary>
    public int GameOverTitleY { get; set; }

    /// <summary>Game Over タイトル文字色 (<see cref="Color"/>)</summary>
    public Color GameOverTitleColor { get; set; }

    /// <summary>Game Over タイトル文字列</summary>
    public string GameOverTitleText { get; set; } = string.Empty;

    /// <summary>Game Over リトライテキスト X 座標</summary>
    public int GameOverRetryX { get; set; }

    /// <summary>Game Over リトライテキスト Y 座標</summary>
    public int GameOverRetryY { get; set; }

    /// <summary>Game Over リトライ文字色 (<see cref="Color"/>)</summary>
    public Color GameOverRetryColor { get; set; }

    /// <summary>Game Over リトライ案内文字列</summary>
    public string GameOverRetryText { get; set; } = string.Empty;

    /// <summary>Victory 矩形左上 X 座標</summary>
    public int VictoryX1 { get; set; }

    /// <summary>Victory 矩形左上 Y 座標</summary>
    public int VictoryY1 { get; set; }

    /// <summary>Victory 矩形右下 X 座標</summary>
    public int VictoryX2 { get; set; }

    /// <summary>Victory 矩形右下 Y 座標</summary>
    public int VictoryY2 { get; set; }

    /// <summary>Victory 背景色 (<see cref="Color"/>)</summary>
    public Color VictoryBgColor { get; set; }

    /// <summary>Victory 外枠線色 (<see cref="Color"/>)</summary>
    public Color VictoryOuterBorderColor { get; set; }

    /// <summary>Victory 内枠線色 (<see cref="Color"/>)</summary>
    public Color VictoryInnerBorderColor { get; set; }

    /// <summary>Victory タイトル X 座標</summary>
    public int VictoryTitleX { get; set; }

    /// <summary>Victory タイトル Y 座標</summary>
    public int VictoryTitleY { get; set; }

    /// <summary>Victory タイトル文字色 (<see cref="Color"/>)</summary>
    public Color VictoryTitleColor { get; set; }

    /// <summary>Victory タイトル文字列</summary>
    public string VictoryTitleText { get; set; } = string.Empty;

    /// <summary>Victory リトライテキスト X 座標</summary>
    public int VictoryRetryX { get; set; }

    /// <summary>Victory リトライテキスト Y 座標</summary>
    public int VictoryRetryY { get; set; }

    /// <summary>Victory リトライ文字色 (<see cref="Color"/>)</summary>
    public Color VictoryRetryColor { get; set; }

    /// <summary>Victory リトライ案内文字列</summary>
    public string VictoryRetryText { get; set; } = string.Empty;
}

/// <summary>
/// Hitbox / Hurtbox / 衝突境界線デバッグ可視化描画の設定クラス。
/// </summary>
public sealed class DebugRenderConfig
{
    /// <summary>起動時にデバッグ描画を有効にするか</summary>
    public bool Enabled { get; set; }

    /// <summary>プレイヤー地形衝突ボックス枠色 (<see cref="Color"/>)</summary>
    public Color PlayerTerrainBoxColor { get; set; }

    /// <summary>プレイヤー被弾判定円色 (<see cref="Color"/>)</summary>
    public Color PlayerHurtboxColor { get; set; }

    /// <summary>Boss 攻撃・被弾衝突ボックス枠色 (<see cref="Color"/>)</summary>
    public Color BossHitboxColor { get; set; }

    /// <summary>Boss 弾幕判定円枠色 (<see cref="Color"/>)</summary>
    public Color BossBulletHitboxColor { get; set; }

    /// <summary>プレイヤー銃弾判定ボックス枠色 (<see cref="Color"/>)</summary>
    public Color PlayerBulletHitboxColor { get; set; }

    /// <summary>足場衝突境界枠色 (<see cref="Color"/>)</summary>
    public Color PlatformHitboxColor { get; set; }

    /// <summary>デバッグ状態インジケータ文字色 (<see cref="Color"/>)</summary>
    public Color IndicatorColor { get; set; }
}

/// <summary>
/// Config/render.toml から読み込まれる全体の描画および UI 設定クラス。
/// </summary>
public sealed class RenderConfig
{
    /// <summary>Boss 描画エフェクト設定</summary>
    public BossEffectsRenderConfig BossEffects { get; set; } = new();

    /// <summary>銃弾および弾幕描画設定</summary>
    public BulletRenderConfig Bullets { get; set; } = new();

    /// <summary>左上 HUD 描画設定</summary>
    public HudRenderConfig Hud { get; set; } = new();

    /// <summary>画面下部 Boss HP バー描画設定</summary>
    public BossHpBarRenderConfig BossHpBar { get; set; } = new();

    /// <summary>モーダルダイアログ描画設定</summary>
    public ModalRenderConfig Modals { get; set; } = new();

    /// <summary>Hitbox / Hurtbox デバッグ描画設定</summary>
    public DebugRenderConfig Debug { get; set; } = new();

    /// <summary>
    /// 指定された TOML ファイルから設定を読み込んだ <see cref="ConfigBase{RenderConfig}"/> インスタンスを生成します。
    /// </summary>
    /// <param name="path">TOML ファイル名</param>
    /// <returns>読み込み結果</returns>
    public static ErrorOr.ErrorOr<ConfigBase<RenderConfig>> LoadFromFile(string path = ResourcePath.Config.Render)
    {
        return ConfigBase<RenderConfig>.LoadFromFile(path);
    }
}
