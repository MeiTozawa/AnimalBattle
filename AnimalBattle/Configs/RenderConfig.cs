using System.Drawing;
using AnimalBattle.Core;

namespace AnimalBattle.Configs;

/// <summary>
/// 地図足場プラットフォームの描画設定クラス。
/// </summary>
public sealed class PlatformRenderConfig
{
    /// <summary>プラットフォーム内部塗りつぶし色 (<see cref="Color"/>)</summary>
    public Color FillColor { get; set; } = Color.FromArgb(0xFF, 0x14, 0x19, 0x26);

    /// <summary>プラットフォーム外枠線色 (<see cref="Color"/>)</summary>
    public Color BorderColor { get; set; } = Color.FromArgb(0xFF, 0x00, 0xE5, 0xFF);

    /// <summary>プラットフォーム上面ハイライト色 (<see cref="Color"/>)</summary>
    public Color TopHighlightColor { get; set; } = Color.FromArgb(0xFF, 0x80, 0xFF, 0xFF);

    /// <summary>上面ハイライトの内側オフセット量</summary>
    public int TopHighlightOffset { get; set; } = 2;

    /// <summary>上面ハイライトの厚み</summary>
    public int TopHighlightThickness { get; set; } = 2;
}

/// <summary>
/// Boss の光環・予兆リング・無敵シールド・受撃フラッシュ等の描画エフェクト設定クラス。
/// </summary>
public sealed class BossEffectsRenderConfig
{
    /// <summary>Phase 1 オーラカラー (<see cref="Color"/>)</summary>
    public Color Phase1AuraColor { get; set; } = Color.FromArgb(0xFF, 0x00, 0xE5, 0xFF);

    /// <summary>Phase 2 オーラカラー (<see cref="Color"/>)</summary>
    public Color Phase2AuraColor { get; set; } = Color.FromArgb(0xFF, 0xBA, 0x55, 0xD3);

    /// <summary>Phase 3 オーラカラー (<see cref="Color"/>)</summary>
    public Color Phase3AuraColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0x17, 0x44);

    /// <summary>オーラ基本半径</summary>
    public float AuraBaseRadius { get; set; } = 75.0f;

    /// <summary>オーラ脈動振幅</summary>
    public float AuraPulseAmp { get; set; } = 8.0f;

    /// <summary>オーラ脈動周波数</summary>
    public float AuraPulseSpeed { get; set; } = 4.0f;

    /// <summary>前隙予兆リング開始外径</summary>
    public float TelegraphStartRadius { get; set; } = 140.0f;

    /// <summary>前隙予兆リング収縮距離</summary>
    public float TelegraphEndRadiusDelta { get; set; } = 60.0f;

    /// <summary>前隙予兆リング外枠色 (<see cref="Color"/>)</summary>
    public Color TelegraphRingColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0x33, 0x33);

    /// <summary>前隙予兆リング内枠色 (<see cref="Color"/>)</summary>
    public Color TelegraphInnerRingColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

    /// <summary>前隙予兆中心コア半径</summary>
    public float TelegraphCoreRadius { get; set; } = 35.0f;

    /// <summary>前隙予兆中心コア色 (<see cref="Color"/>)</summary>
    public Color TelegraphCoreColor { get; set; } = Color.FromArgb(0x55, 0xFF, 0x22, 0x22);

    /// <summary>無敵シールド基本半径</summary>
    public float ShieldBaseRadius { get; set; } = 85.0f;

    /// <summary>无敌シールド脈動振幅</summary>
    public float ShieldPulseAmp { get; set; } = 6.0f;

    /// <summary>無敵シールド脈動周波数</summary>
    public float ShieldPulseSpeed { get; set; } = 12.0f;

    /// <summary>無敵シールド外枠色 (<see cref="Color"/>)</summary>
    public Color ShieldOuterColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0xD7, 0x00);

    /// <summary>無敵シールド内枠色 (<see cref="Color"/>)</summary>
    public Color ShieldInnerColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0xF2, 0x75);

    /// <summary>無敵シールド半透明塗りつぶし色 (<see cref="Color"/>)</summary>
    public Color ShieldFillColor { get; set; } = Color.FromArgb(0x44, 0xFF, 0xFF, 0x88);

    /// <summary>後隙（隙あり）時の光輪半径</summary>
    public float RecoveringAuraRadius { get; set; } = 65.0f;

    /// <summary>後隙時の光輪色 (<see cref="Color"/>)</summary>
    public Color RecoveringAuraColor { get; set; } = Color.FromArgb(0x33, 0x00, 0xE5, 0xFF);

    /// <summary>被弾ホワイトフラッシュ半径</summary>
    public float HitFlashRadius { get; set; } = 45.0f;

    /// <summary>被弾ホワイトフラッシュ色 (<see cref="Color"/>)</summary>
    public Color HitFlashColor { get; set; } = Color.FromArgb(0x88, 0xFF, 0xFF, 0xFF);

    /// <summary>Boss スプライト描画スケール</summary>
    public double BossSpriteScale { get; set; } = 2.8;

    /// <summary>Boss 浮遊時の揺れ角度振幅</summary>
    public double BossHoverAngleAmp { get; set; } = 0.08;

    /// <summary>Boss 浮遊時の揺れ角速度</summary>
    public float BossHoverAngleSpeed { get; set; } = 2.0f;
}

/// <summary>
/// プレイヤー自機描画設定クラス。
/// </summary>
public sealed class PlayerRenderConfig
{
    /// <summary>当たり判定コア点（内円）半径</summary>
    public float HitboxCoreRadius { get; set; } = 3.0f;

    /// <summary>当たり判定コア点塗りつぶし色 (<see cref="Color"/>)</summary>
    public Color HitboxCoreColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0x00, 0x33);

    /// <summary>当たり判定枠（外円）半径</summary>
    public float HitboxBorderRadius { get; set; } = 4.0f;

    /// <summary>当たり判定枠色 (<see cref="Color"/>)</summary>
    public Color HitboxBorderColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
}

/// <summary>
/// 銃弾および弾幕の描画設定クラス。
/// </summary>
public sealed class BulletRenderConfig
{
    /// <summary>プレイヤー弾外層グロー半径</summary>
    public float PlayerBulletOuterRadius { get; set; } = 6.0f;

    /// <summary>プレイヤー弾外層グロー色 (<see cref="Color"/>)</summary>
    public Color PlayerBulletOuterColor { get; set; } = Color.FromArgb(0xFF, 0x00, 0xFF, 0xFF);

    /// <summary>プレイヤー弾内層コア半径</summary>
    public float PlayerBulletInnerRadius { get; set; } = 3.0f;

    /// <summary>プレイヤー弾内層コア色 (<see cref="Color"/>)</summary>
    public Color PlayerBulletInnerColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

    /// <summary>プレイヤー弾ビームトレイル半幅</summary>
    public int PlayerBulletTrailHalfWidth { get; set; } = 7;

    /// <summary>プレイヤー弾ビームトレイル半高</summary>
    public int PlayerBulletTrailHalfHeight { get; set; } = 2;

    /// <summary>プレイヤー弾ビームトレイル色 (<see cref="Color"/>)</summary>
    public Color PlayerBulletTrailColor { get; set; } = Color.FromArgb(0xFF, 0x80, 0xFF, 0xFF);

    /// <summary>Boss 弾内層ホワイトコア半径比率</summary>
    public float BossBulletCoreRadiusRatio { get; set; } = 0.45f;

    /// <summary>Boss 弾内層ホワイトコア最小ピクセル半径</summary>
    public int BossBulletCoreMinRadius { get; set; } = 2;

    /// <summary>Boss 弾内層ホワイトコア色 (<see cref="Color"/>)</summary>
    public Color BossBulletCoreColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

    /// <summary>Boss 弾外枠付加ピクセル半径</summary>
    public int BossBulletOutlinePadding { get; set; } = 2;
}

/// <summary>
/// 左上 HUD 操作案内表示の設定クラス。
/// </summary>
public sealed class HudRenderConfig
{
    /// <summary>タイトル X 座標</summary>
    public int TitleX { get; set; } = 20;

    /// <summary>タイトル Y 座標</summary>
    public int TitleY { get; set; } = 16;

    /// <summary>タイトル文字色 (<see cref="Color"/>)</summary>
    public Color TitleColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0xD7, 0x00);

    /// <summary>タイトル文字列</summary>
    public string TitleText { get; set; } = "=== BULLET BATTLE (TOML FSM) ===";

    /// <summary>行間隔ピクセル</summary>
    public int LineSpacing { get; set; } = 18;

    /// <summary>移動操作案内テキスト</summary>
    public string MoveHelpText { get; set; } = "[A/D/Left/Right]: Move (Instant Stop)";

    /// <summary>ジャンプ操作案内テキスト</summary>
    public string JumpHelpText { get; set; } = "[W/Space/Up]: Jump & Air Double Jump";

    /// <summary>射撃操作案内テキスト</summary>
    public string ShootHelpText { get; set; } = "[J/Z]: Shoot Pistol";

    /// <summary>通常案内文字色 (<see cref="Color"/>)</summary>
    public Color HelpTextColor { get; set; } = Color.FromArgb(0xFF, 0xCC, 0xCC, 0xCC);

    /// <summary>リスタート案内テキスト</summary>
    public string RestartHelpText { get; set; } = "[R]: Instant Restart (1-Frame Reset)";

    /// <summary>リスタート案内文字色 (<see cref="Color"/>)</summary>
    public Color RestartHelpColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0x80, 0x80);

    /// <summary>二段ジャンプインジケータ Y 座標</summary>
    public int DoubleJumpY { get; set; } = 116;

    /// <summary>二段ジャンプ有効時シンボル色 (<see cref="Color"/>)</summary>
    public Color DoubleJumpActiveColor { get; set; } = Color.FromArgb(0xFF, 0x00, 0xE5, 0xFF);

    /// <summary>二段ジャンプ消費時シンボル色 (<see cref="Color"/>)</summary>
    public Color DoubleJumpInactiveColor { get; set; } = Color.FromArgb(0xFF, 0x88, 0x88, 0x88);
}

/// <summary>
/// 画面下部 Boss HP バーおよび状態表示の設定クラス。
/// </summary>
public sealed class BossHpBarRenderConfig
{
    /// <summary>HP バー左上 X 座標</summary>
    public int BarX1 { get; set; } = 240;

    /// <summary>HP バー左上 Y 座標</summary>
    public int BarY1 { get; set; } = 662;

    /// <summary>HP バー右下 X 座標</summary>
    public int BarX2 { get; set; } = 1040;

    /// <summary>HP バー右下 Y 座標</summary>
    public int BarY2 { get; set; } = 688;

    /// <summary>HP バー背景外枠パディング</summary>
    public int BgPadding { get; set; } = 4;

    /// <summary>HP バー暗色背景色 (<see cref="Color"/>)</summary>
    public Color BgColor { get; set; } = Color.FromArgb(0xDD, 0x0A, 0x0D, 0x14);

    /// <summary>HP バー外枠線色 (<see cref="Color"/>)</summary>
    public Color BorderColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

    /// <summary>HP バー上面ハイライト厚み</summary>
    public int TopHighlightThickness { get; set; } = 3;

    /// <summary>HP バー上面ハイライト色 (<see cref="Color"/>)</summary>
    public Color TopHighlightColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

    /// <summary>ヘッダー文字の Y 座標オフセット</summary>
    public int HeaderOffsetY { get; set; } = 22;

    /// <summary>Boss タイトル文字色 (<see cref="Color"/>)</summary>
    public Color TitleColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0xD7, 0x00);

    /// <summary>HP 数値文字色 (<see cref="Color"/>)</summary>
    public Color HpTextColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

    /// <summary>状態バッジテキストの X 軸オフセット</summary>
    public int StateBadgeOffsetX { get; set; } = 270;

    /// <summary>前隙予兆状態バッジ文字色 (<see cref="Color"/>)</summary>
    public Color StateTelegraphingColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0x55, 0x55);

    /// <summary>発動中状態バッジ文字色 (<see cref="Color"/>)</summary>
    public Color StateCastingColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0xD7, 0x00);

    /// <summary>隙あり虚弱状態バッジ文字色 (<see cref="Color"/>)</summary>
    public Color StateRecoveringColor { get; set; } = Color.FromArgb(0xFF, 0x00, 0xFF, 0x88);

    /// <summary>フェーズ遷移演出状態バッジ文字色 (<see cref="Color"/>)</summary>
    public Color StatePhaseTransitionColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0x88, 0xFF);

    /// <summary>通常巡航状態バッジ文字色 (<see cref="Color"/>)</summary>
    public Color StateDefaultColor { get; set; } = Color.FromArgb(0xFF, 0x88, 0xCC, 0xFF);
}

/// <summary>
/// 敗北 (Game Over) および勝利 (Victory) モーダルダイアログの描画設定クラス。
/// </summary>
public sealed class ModalRenderConfig
{
    /// <summary>Game Over 矩形左上 X 座標</summary>
    public int GameOverX1 { get; set; } = 360;

    /// <summary>Game Over 矩形左上 Y 座標</summary>
    public int GameOverY1 { get; set; } = 260;

    /// <summary>Game Over 矩形右下 X 座標</summary>
    public int GameOverX2 { get; set; } = 920;

    /// <summary>Game Over 矩形右下 Y 座標</summary>
    public int GameOverY2 { get; set; } = 440;

    /// <summary>Game Over 背景色 (<see cref="Color"/>)</summary>
    public Color GameOverBgColor { get; set; } = Color.FromArgb(0xDD, 0x2A, 0x05, 0x05);

    /// <summary>Game Over 外枠線色 (<see cref="Color"/>)</summary>
    public Color GameOverOuterBorderColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0x33, 0x33);

    /// <summary>Game Over 内枠線色 (<see cref="Color"/>)</summary>
    public Color GameOverInnerBorderColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0x88, 0x88);

    /// <summary>Game Over タイトル X 座標</summary>
    public int GameOverTitleX { get; set; } = 540;

    /// <summary>Game Over タイトル Y 座標</summary>
    public int GameOverTitleY { get; set; } = 305;

    /// <summary>Game Over タイトル文字色 (<see cref="Color"/>)</summary>
    public Color GameOverTitleColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0x33, 0x33);

    /// <summary>Game Over タイトル文字列</summary>
    public string GameOverTitleText { get; set; } = "=== GAME OVER ===";

    /// <summary>Game Over リトライテキスト X 座標</summary>
    public int GameOverRetryX { get; set; } = 460;

    /// <summary>Game Over リトライテキスト Y 座標</summary>
    public int GameOverRetryY { get; set; } = 360;

    /// <summary>Game Over リトライ文字色 (<see cref="Color"/>)</summary>
    public Color GameOverRetryColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

    /// <summary>Game Over リトライ案内文字列</summary>
    public string GameOverRetryText { get; set; } = "Press  [ R ]  for Instant Retry!";

    /// <summary>Victory 矩形左上 X 座標</summary>
    public int VictoryX1 { get; set; } = 340;

    /// <summary>Victory 矩形左上 Y 座標</summary>
    public int VictoryY1 { get; set; } = 250;

    /// <summary>Victory 矩形右下 X 座標</summary>
    public int VictoryX2 { get; set; } = 940;

    /// <summary>Victory 矩形右下 Y 座標</summary>
    public int VictoryY2 { get; set; } = 450;

    /// <summary>Victory 背景色 (<see cref="Color"/>)</summary>
    public Color VictoryBgColor { get; set; } = Color.FromArgb(0xEE, 0x2A, 0x2A, 0x00);

    /// <summary>Victory 外枠線色 (<see cref="Color"/>)</summary>
    public Color VictoryOuterBorderColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0xD7, 0x00);

    /// <summary>Victory 内枠線色 (<see cref="Color"/>)</summary>
    public Color VictoryInnerBorderColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0xF2, 0x75);

    /// <summary>Victory タイトル X 座標</summary>
    public int VictoryTitleX { get; set; } = 440;

    /// <summary>Victory タイトル Y 座標</summary>
    public int VictoryTitleY { get; set; } = 295;

    /// <summary>Victory タイトル文字色 (<see cref="Color"/>)</summary>
    public Color VictoryTitleColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0xD7, 0x00);

    /// <summary>Victory タイトル文字列</summary>
    public string VictoryTitleText { get; set; } = "★ ★ ★ VICTORY! BOSS DEFEATED! ★ ★ ★";

    /// <summary>Victory リトライテキスト X 座標</summary>
    public int VictoryRetryX { get; set; } = 460;

    /// <summary>Victory リトライテキスト Y 座標</summary>
    public int VictoryRetryY { get; set; } = 350;

    /// <summary>Victory リトライ文字色 (<see cref="Color"/>)</summary>
    public Color VictoryRetryColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

    /// <summary>Victory リトライ案内文字列</summary>
    public string VictoryRetryText { get; set; } = "Congratulations! Press [ R ] to Play Again!";
}

/// <summary>
/// Config/render.toml から読み込まれる全体の描画および UI 設定クラス。
/// </summary>
public sealed class RenderConfig
{
    /// <summary>プラットフォーム足場描画設定</summary>
    public PlatformRenderConfig Platform { get; set; } = new();

    /// <summary>Boss 描画エフェクト設定</summary>
    public BossEffectsRenderConfig BossEffects { get; set; } = new();

    /// <summary>プレイヤー自機描画設定</summary>
    public PlayerRenderConfig Player { get; set; } = new();

    /// <summary>銃弾および弾幕描画設定</summary>
    public BulletRenderConfig Bullets { get; set; } = new();

    /// <summary>左上 HUD 描画設定</summary>
    public HudRenderConfig Hud { get; set; } = new();

    /// <summary>画面下部 Boss HP バー描画設定</summary>
    public BossHpBarRenderConfig BossHpBar { get; set; } = new();

    /// <summary>モーダルダイアログ描画設定</summary>
    public ModalRenderConfig Modals { get; set; } = new();

    /// <summary>
    /// 指定された TOML ファイルから設定を読み込んだ <see cref="ConfigBase{RenderConfig}"/> インスタンスを生成します。
    /// </summary>
    /// <param name="path">TOML ファイル名</param>
    /// <returns>読み込み結果</returns>
    public static Result<ConfigBase<RenderConfig>> LoadFromFile(string path = "render.toml")
    {
        return ConfigBase<RenderConfig>.LoadFromFile(path);
    }
}
