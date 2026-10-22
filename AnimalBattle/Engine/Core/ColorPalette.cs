using System.Drawing;

namespace AnimalBattle.Core;

/// <summary>
///     ゲーム全体：UI・弾幕・エフェクト・ステージ背景・状態バッジ等で使用される共通カラーパレットを定義する静的クラス。
///     すべての常用カラーを <see cref="Color" /> 定数として一元管理します。
/// </summary>
public static class ColorPalette {
    // ==========================================
    // 基本色・無彩色
    // ==========================================

    /// <summary>完全透明色 16進形式</summary>
    public static readonly Color Transparent = Color.FromArgb(0x00, 0x00, 0x00, 0x00);

    /// <summary>純白 16進形式</summary>
    public static readonly Color White = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

    /// <summary>純黒 16進形式</summary>
    public static readonly Color Black = Color.FromArgb(0xFF, 0x00, 0x00, 0x00);

    /// <summary>明るいグレー / ヘルプテキスト色 16進形式</summary>
    public static readonly Color GrayLight = Color.FromArgb(0xFF, 0xCC, 0xCC, 0xCC);

    /// <summary>中間グレー / 非アクティブ表示色 16進形式</summary>
    public static readonly Color Gray = Color.FromArgb(0xFF, 0x88, 0x88, 0x88);

    /// <summary>ダークグレー 16進形式</summary>
    public static readonly Color GrayDark = Color.FromArgb(0xFF, 0x44, 0x44, 0x44);

    // ==========================================
    // 背景・UI コンテナ色
    // ==========================================

    /// <summary>足場プラットフォーム内部塗りつぶし暗色 16進形式</summary>
    public static readonly Color DarkBg = Color.FromArgb(0xFF, 0x14, 0x19, 0x26);

    /// <summary>HUD / HP バー用半透明暗色背景 16進形式</summary>
    public static readonly Color DarkModalBg = Color.FromArgb(0xDD, 0x0A, 0x0D, 0x14);

    /// <summary>Game Over 画面用半透明暗赤色背景 16進形式</summary>
    public static readonly Color GameOverBg = Color.FromArgb(0xDD, 0x2A, 0x05, 0x05);

    /// <summary>Victory 画面用半透明暗金色背景 16進形式</summary>
    public static readonly Color VictoryBg = Color.FromArgb(0xEE, 0x2A, 0x2A, 0x00);

    // ==========================================
    // ゴールド・イエロー系
    // ==========================================

    /// <summary>ゴールド / タイトル・シールド外枠色 16進形式</summary>
    public static readonly Color Gold = Color.FromArgb(0xFF, 0xFF, 0xD7, 0x00);

    /// <summary>ライトゴールド / 内枠ハイライト色 16進形式</summary>
    public static readonly Color GoldLight = Color.FromArgb(0xFF, 0xFF, 0xF2, 0x75);

    /// <summary>シールド半透明塗りつぶし黄 16進形式</summary>
    public static readonly Color GoldShieldFill = Color.FromArgb(0x44, 0xFF, 0xFF, 0x88);

    /// <summary>ヒット火花エフェクト色 16進形式</summary>
    public static readonly Color SparkYellow = Color.FromArgb(0xFF, 0xFF, 0xFF, 0x55);

    // ==========================================
    // シアン・アクアブルー系
    // ==========================================

    /// <summary>鮮明なシアン / Phase 1 オーラ・弾幕色 16進形式</summary>
    public static readonly Color Cyan = Color.FromArgb(0xFF, 0x00, 0xE5, 0xFF);

    /// <summary>明るいシアン / プラットフォーム上面ハイライト色 16進形式</summary>
    public static readonly Color CyanLight = Color.FromArgb(0xFF, 0x80, 0xFF, 0xFF);

    /// <summary>ネオンシアン / プレイヤー銃弾外層グロー色 16進形式</summary>
    public static readonly Color CyanNeon = Color.FromArgb(0xFF, 0x00, 0xFF, 0xFF);

    /// <summary>半透明アクアブルー / 後隙光輪色 16進形式</summary>
    public static readonly Color CyanRecovering = Color.FromArgb(0x33, 0x00, 0xE5, 0xFF);

    /// <summary>スカイブルー / 通常巡航状態バッジ色 16進形式</summary>
    public static readonly Color BlueSky = Color.FromArgb(0xFF, 0x88, 0xCC, 0xFF);

    /// <summary>ディープダークブルー / 降雨弾幕塗りつぶし色 16進形式</summary>
    public static readonly Color BlueDark = Color.FromArgb(0xFF, 0x01, 0x06, 0x76);

    // ==========================================
    // レッド・クリムゾン系
    // ==========================================

    /// <summary>鮮赤 / 弾幕・危険警告色 16進形式</summary>
    public static readonly Color Red = Color.FromArgb(0xFF, 0xFF, 0x33, 0x33);

    /// <summary>クリムゾンレッド / Phase 3 オーラ・交差波色 16進形式</summary>
    public static readonly Color RedBright = Color.FromArgb(0xFF, 0xFF, 0x17, 0x44);

    /// <summary>スナイパーレーザーレッド 16進形式</summary>
    public static readonly Color RedLaser = Color.FromArgb(0xFF, 0xFF, 0x52, 0x52);

    /// <summary>ライトレッド / 予兆・内枠色 16進形式</summary>
    public static readonly Color RedLight = Color.FromArgb(0xFF, 0xFF, 0x88, 0x88);

    /// <summary>コーラルレッド / 交差波外枠色 16進形式</summary>
    public static readonly Color RedCoral = Color.FromArgb(0xFF, 0xFF, 0x8A, 0x80);

    /// <summary>自機当たり判定コア点深紅 16進形式</summary>
    public static readonly Color RedHitbox = Color.FromArgb(0xFF, 0xFF, 0x00, 0x33);

    /// <summary>予兆中心コア半透明赤 16進形式</summary>
    public static readonly Color RedTelegraphCore = Color.FromArgb(0x55, 0xFF, 0x22, 0x22);

    /// <summary>リスタート案内薄赤 16進形式</summary>
    public static readonly Color RedRestart = Color.FromArgb(0xFF, 0xFF, 0x80, 0x80);

    // ==========================================
    // グリーン・ミント系
    // ==========================================

    /// <summary>エメラルドグリーン / HP 満タン・隙あり状態色 16進形式</summary>
    public static readonly Color Green = Color.FromArgb(0xFF, 0x00, 0xFF, 0x88);

    /// <summary>ミントグリーン / 降雨弾外枠色 16進形式</summary>
    public static readonly Color GreenMint = Color.FromArgb(0xFF, 0xB9, 0xF6, 0xCA);

    // ==========================================
    // パープル・マゼンタ系
    // ==========================================

    /// <summary>オーキッドパープル / Phase 2 オーラ色 16進形式</summary>
    public static readonly Color Purple = Color.FromArgb(0xFF, 0xBA, 0x55, 0xD3);

    /// <summary>ラベンダーパープル / 螺旋弾幕色 16進形式</summary>
    public static readonly Color PurpleSpiral = Color.FromArgb(0xFF, 0xB2, 0x55, 0xD3);

    /// <summary>ライトパープル / 予兆外枠色 16進形式</summary>
    public static readonly Color PurpleLight = Color.FromArgb(0xFF, 0xE0, 0xB0, 0xFF);

    /// <summary>ピンクマゼンタ / Phase 遷移バッジ色 16進形式</summary>
    public static readonly Color PinkMagenta = Color.FromArgb(0xFF, 0xFF, 0x88, 0xFF);

    // ==========================================
    // オレンジ・ブラウン・アース系
    // ==========================================

    /// <summary>レッドオレンジ 16進形式</summary>
    public static readonly Color RedOrange = Color.FromArgb(0xFF, 0xFF, 0x57, 0x22);

    /// <summary>ブライトオレンジ / 斬撃色 16進形式</summary>
    public static readonly Color OrangeBright = Color.FromArgb(0xFF, 0xFF, 0x70, 0x43);

    /// <summary>アンバーゴールド / 震波色 16進形式</summary>
    public static readonly Color Amber = Color.FromArgb(0xFF, 0xFF, 0xC1, 0x07);

    /// <summary>ディープクリムゾン 16進形式</summary>
    public static readonly Color RedCrimson = Color.FromArgb(0xFF, 0xD5, 0x00, 0x00);

    /// <summary>ブラウン 16進形式</summary>
    public static readonly Color Brown = Color.FromArgb(0xFF, 0x79, 0x55, 0x48);

    /// <summary>アースブラウン / 落石色 16進形式</summary>
    public static readonly Color EarthBrown = Color.FromArgb(0xFF, 0x8D, 0x6E, 0x63);

    /// <summary>アースストーン外枠色 16進形式</summary>
    public static readonly Color EarthStone = Color.FromArgb(0xFF, 0xD7, 0xCC, 0xC8);

    // ==========================================
    // 半透明エフェクト色
    // ==========================================

    /// <summary>被弾ホワイトフラッシュ色 16進形式</summary>
    public static readonly Color HitFlashWhite = Color.FromArgb(0x88, 0xFF, 0xFF, 0xFF);

    /// <summary>無敵時間シールド光輪色 16進形式</summary>
    public static readonly Color InvincibleShield = Color.FromArgb(0x88, 0x00, 0xFF, 0xFF);

    /// <summary>ダッシュ残像半透明シアン発光色 16進形式</summary>
    public static readonly Color GhostTrailCyan = Color.FromArgb(0xB4, 0x50, 0xC8, 0xFF);

    /// <summary>銃口閃光マズルフラッシュ発光色 16進形式</summary>
    public static readonly Color MuzzleFlashFill = Color.FromArgb(0xFF, 0xDC, 0xEB, 0xFF);

    /// <summary>弾丸撃砕破片スパッター塗りつぶし色 16進形式</summary>
    public static readonly Color ImpactSplatterFill = Color.FromArgb(0xFF, 0xFF, 0xE6, 0x64);

    // ==========================================
    // 特殊弾幕種別専用カラー
    // ==========================================

    /// <summary>品紅弾幕塗りつぶし色 16進形式</summary>
    public static readonly Color BulletMagentaFill = Color.FromArgb(0xFF, 0xFF, 0x14, 0x93);

    /// <summary>品紅弾幕外枠発光色 16進形式</summary>
    public static readonly Color BulletMagentaOutline = Color.FromArgb(0xFF, 0xFF, 0x80, 0xAB);

    /// <summary>黄弾幕塗りつぶしゴールド 16進形式</summary>
    public static readonly Color BulletYellowFill = Color.FromArgb(0xFF, 0xFF, 0xD7, 0x00);

    /// <summary>黄弾幕外枠発光色 16進形式</summary>
    public static readonly Color BulletYellowOutline = Color.FromArgb(0xFF, 0xFF, 0xF2, 0x75);

    /// <summary>黄弾幕副螺旋塗りつぶしエレクトリックイエロー 16進形式</summary>
    public static readonly Color BulletYellowSubFill = Color.FromArgb(0xFF, 0xFF, 0xEA, 0x00);

    /// <summary>黄弾幕副螺旋外枠明黄色 16進形式</summary>
    public static readonly Color BulletYellowSubOutline = Color.FromArgb(0xFF, 0xFF, 0xFF, 0x8D);

    /// <summary>青弾幕塗りつぶし電光シアン 16進形式</summary>
    public static readonly Color BulletCyanFill = Color.FromArgb(0xFF, 0x00, 0xE5, 0xFF);

    /// <summary>青弾幕外枠発光シアン白 16進形式</summary>
    public static readonly Color BulletCyanOutline = Color.FromArgb(0xFF, 0xE0, 0xF7, 0xFA);

    /// <summary>黒弾幕塗りつぶし暗黒コア 16進形式</summary>
    public static readonly Color BulletBlackFill = Color.FromArgb(0xFF, 0x11, 0x11, 0x16);

    /// <summary>黒弾幕外枠クリムゾン発光 16進形式</summary>
    public static readonly Color BulletBlackOutline = Color.FromArgb(0xFF, 0xFF, 0x17, 0x44);
}