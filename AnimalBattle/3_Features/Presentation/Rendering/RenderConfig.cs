using System.Drawing;
using ErrorOr;

namespace AnimalBattle.Configs;

/// <summary>
///     Boss の攻撃予兆・無敵シールド等の描画エフェクト設定クラス。
/// </summary>
public sealed class BossEffectsRenderConfig {
    /// <summary>危険判定矩形 (Danger Box) の半透明アルファ値 (0～255)</summary>
    public int DangerBoxAlpha { get; set; }

    /// <summary>照準レーザー (Aiming Laser) の半透明アルファ値 (0～255)</summary>
    public int LaserAlpha { get; set; }

    /// <summary>突進軌道レーン (Charge Track) の半透明アルファ値 (0～255)</summary>
    public int ChargeTrackAlpha { get; set; }

    /// <summary>警告テキスト・マーカー表示色 (<see cref="Color" />)</summary>
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

    /// <summary>易傷時の醒目攻撃チャンス表示色 (<see cref="Color" />)</summary>
    public Color ChanceMarkerColor { get; set; }

    /// <summary>易傷時のオーラ脈動角速度</summary>
    public float VulnerablePulseSpeed { get; set; } = 10.0f;

    /// <summary>易傷時のオーラ基礎アルファ値 (0～255)</summary>
    public int VulnerableAuraBaseAlpha { get; set; } = 130;

    /// <summary>易傷時のオーラ脈動アルファ振幅</summary>
    public int VulnerableAuraPulseAlpha { get; set; } = 110;

    /// <summary>易傷時のオーラ拡大量比率</summary>
    public float VulnerableAuraScale { get; set; } = 1.04f;

    /// <summary>易傷マーカー浮動揺れ角速度</summary>
    public float ChanceMarkerBobbingSpeed { get; set; } = 8.0f;

    /// <summary>易傷マーカー浮動揺れ振幅 (px)</summary>
    public float ChanceMarkerBobbingAmplitude { get; set; } = 4.0f;

    /// <summary>易傷マーカー文字 1 文字あたりの推定幅 (px)</summary>
    public int ChanceMarkerCharWidth { get; set; } = 7;

    /// <summary>無敵・フェーズ遷移シールドの基準半径 (px)</summary>
    public float InvincibleShieldRadius { get; set; }

    /// <summary>金身オーラ拡大倍率</summary>
    public float InvincibleAuraScale { get; set; }

    /// <summary>金身脈動角速度</summary>
    public float InvinciblePulseSpeed { get; set; }

    /// <summary>金身オーラ基礎アルファ値 (0～255)</summary>
    public int InvincibleAuraBaseAlpha { get; set; }

    /// <summary>金身オーラ脈動振幅アルファ値</summary>
    public int InvincibleAuraPulseAlpha { get; set; }

    /// <summary>金身オーラ発光色 (<see cref="Color" />)</summary>
    public Color InvincibleAuraColor { get; set; }

    /// <summary>シールド結界呼吸周波数</summary>
    public float InvincibleBreathSpeed { get; set; }

    /// <summary>シールド結界呼吸振幅 (px)</summary>
    public float InvincibleBreathAmplitude { get; set; }

    /// <summary>シールド内層塗りつぶし基礎アルファ値 (0～255)</summary>
    public int InvincibleFillBaseAlpha { get; set; }

    /// <summary>シールド内層塗りつぶし脈動アルファ値</summary>
    public int InvincibleFillPulseAlpha { get; set; }

    /// <summary>シールド外周枠線基礎アルファ値 (0～255)</summary>
    public int InvincibleRingBaseAlpha { get; set; }

    /// <summary>シールド外周枠線脈動アルファ値</summary>
    public int InvincibleRingPulseAlpha { get; set; }

    /// <summary>排斥衝撃波拡散速度</summary>
    public float InvincibleRippleSpeed { get; set; }

    /// <summary>排斥衝撃波開始半径比率</summary>
    public float InvincibleRippleStartRatio { get; set; }

    /// <summary>排斥衝撃波最大拡大量比率</summary>
    public float InvincibleRippleExpandRatio { get; set; }

    /// <summary>排斥衝撃波最大アルファ値 (0～255)</summary>
    public int InvincibleRippleMaxAlpha { get; set; }

    /// <summary>四元防護ノード公転角速度</summary>
    public float InvincibleOrbitSpeed { get; set; }

    /// <summary>防護ノード外周光輪半径 (px)</summary>
    public int InvincibleNodeOuterRadius { get; set; }

    /// <summary>防護ノード中心白熱コア半径 (px)</summary>
    public int InvincibleNodeInnerRadius { get; set; }

    /// <summary>防護ノードアルファ値 (0～255)</summary>
    public int InvincibleNodeAlpha { get; set; }

    /// <summary>防護結界光線基礎アルファ値 (0～255)</summary>
    public int InvinciblePrismAlphaBase { get; set; }

    /// <summary>防護結界光線脈動アルファ値</summary>
    public int InvinciblePrismAlphaPulse { get; set; }

    /// <summary>防護結界光線線の太さ (px)</summary>
    public int InvinciblePrismThickness { get; set; }

    /// <summary>核心十字星芒自転角速度</summary>
    public float InvincibleCrossAngleSpeed { get; set; }

    /// <summary>核心十字星芒長さ比率 (シールド半径に対する比率)</summary>
    public float InvincibleCrossLengthRatio { get; set; }

    /// <summary>核心十字星芒基礎アルファ値 (0～255)</summary>
    public int InvincibleCrossAlphaBase { get; set; }

    /// <summary>核心十字星芒脈動アルファ値</summary>
    public int InvincibleCrossAlphaPulse { get; set; }

    /// <summary>核心十字星芒線の太さ (px)</summary>
    public int InvincibleCrossThickness { get; set; }

    /// <summary>Boss 浮遊時の揺れ角度振幅</summary>
    public double BossHoverAngleAmp { get; set; }


    /// <summary>Boss 浮遊時の揺れ角速度</summary>
    public float BossHoverAngleSpeed { get; set; }
}

/// <summary>
///     銃弾および弾幕の描画設定クラス。
/// </summary>
public sealed class BulletRenderConfig {
    /// <summary>プレイヤー弾外層グロー半径</summary>
    public float PlayerBulletOuterRadius { get; set; }

    /// <summary>プレイヤー弾外層グロー色 (<see cref="Color" />)</summary>
    public Color PlayerBulletOuterColor { get; set; }

    /// <summary>プレイヤー弾内層コア半径</summary>
    public float PlayerBulletInnerRadius { get; set; }

    /// <summary>プレイヤー弾内層コア色 (<see cref="Color" />)</summary>
    public Color PlayerBulletInnerColor { get; set; }

    /// <summary>プレイヤー弾ビームトレイル半幅</summary>
    public int PlayerBulletTrailHalfWidth { get; set; }

    /// <summary>プレイヤー弾ビームトレイル半高</summary>
    public int PlayerBulletTrailHalfHeight { get; set; }

    /// <summary>プレイヤー弾ビームトレイル色 (<see cref="Color" />)</summary>
    public Color PlayerBulletTrailColor { get; set; }

    /// <summary>プレイヤー弾トレイル第1階層のアルファ値 (0～255)</summary>
    public int PlayerBulletTrailAlpha1 { get; set; }

    /// <summary>プレイヤー弾トレイル第2階層のアルファ値 (0～255)</summary>
    public int PlayerBulletTrailAlpha2 { get; set; }

    /// <summary>プレイヤー弾トレイル第3階層のアルファ値 (0～255)</summary>
    public int PlayerBulletTrailAlpha3 { get; set; }

    /// <summary>プレイヤー弾トレイル第1階層の半径比率</summary>
    public float PlayerBulletTrailRadiusRatio1 { get; set; }

    /// <summary>プレイヤー弾トレイル第2階層の半径比率</summary>
    public float PlayerBulletTrailRadiusRatio2 { get; set; }

    /// <summary>プレイヤー弾トレイル第3階層の半径比率</summary>
    public float PlayerBulletTrailRadiusRatio3 { get; set; }

    /// <summary>プレイヤー弾トレイル第1階層のオフセット距離比率</summary>
    public float PlayerBulletTrailOffsetRatio1 { get; set; }

    /// <summary>プレイヤー弾トレイル第2階層のオフセット距離比率</summary>
    public float PlayerBulletTrailOffsetRatio2 { get; set; }

    /// <summary>プレイヤー弾トレイル第3階層のオフセット距離比率</summary>
    public float PlayerBulletTrailOffsetRatio3 { get; set; }

    /// <summary>Boss 弾内層ホワイトコア半径比率</summary>
    public float BossBulletCoreRadiusRatio { get; set; }

    /// <summary>Boss 弾内層ホワイトコア最小ピクセル半径</summary>
    public int BossBulletCoreMinRadius { get; set; }

    /// <summary>Boss 弾内層ホワイトコア色 (<see cref="Color" />)</summary>
    public Color BossBulletCoreColor { get; set; }

    /// <summary>Boss 弾外枠付加ピクセル半径</summary>
    public int BossBulletOutlinePadding { get; set; }

    /// <summary>画面外カリング（描画除外）マージン X (ピクセル)</summary>
    public int ViewportCullingMarginX { get; set; }

    /// <summary>画面外カリング（描画除外）マージン Y (ピクセル)</summary>
    public int ViewportCullingMarginY { get; set; }

    /// <summary>マゼンタ弾オーラ基礎半径比率</summary>
    public float SpecialMagentaAuraBaseRatio { get; set; } = 1.35f;

    /// <summary>マゼンタ弾オーラ脈動比率</summary>
    public float SpecialMagentaAuraPulseRatio { get; set; } = 0.15f;

    /// <summary>マゼンタ弾脈動角速度</summary>
    public float SpecialMagentaPulseSpeed { get; set; } = 6.0f;

    /// <summary>マゼンタ弾オーラアルファ値 (0～255)</summary>
    public int SpecialMagentaAuraAlpha { get; set; } = 65;

    /// <summary>シアン弾外周リング拡張幅 (px)</summary>
    public int SpecialCyanRingPadding { get; set; } = 2;

    /// <summary>シアン弾外周リング発光色 (<see cref="Color" />)</summary>
    public Color SpecialCyanRingColor { get; set; } = Color.FromArgb(50, 0, 240, 255);

    /// <summary>ブラック弾オーラ基礎半径比率</summary>
    public float SpecialBlackAuraBaseRatio { get; set; } = 1.4f;

    /// <summary>ブラック弾オーラ脈動比率</summary>
    public float SpecialBlackAuraPulseRatio { get; set; } = 0.2f;

    /// <summary>ブラック弾脈動角速度</summary>
    public float SpecialBlackPulseSpeed { get; set; } = 6.0f;

    /// <summary>ブラック弾オーラ色 (<see cref="Color" />)</summary>
    public Color SpecialBlackAuraColor { get; set; } = Color.FromArgb(90, 220, 20, 80);

    /// <summary>ブラック弾衛星公転半径比率</summary>
    public float SpecialBlackSatelliteOrbitRatio { get; set; } = 1.55f;

    /// <summary>ブラック弾衛星公転角速度</summary>
    public float SpecialBlackSatelliteOrbitSpeed { get; set; } = 6.0f;

    /// <summary>ブラック弾衛星半径比率</summary>
    public float SpecialBlackSatelliteRadiusRatio { get; set; } = 0.55f;

    /// <summary>ブラック弾衛星最小ピクセル半径</summary>
    public int SpecialBlackSatelliteMinRadius { get; set; } = 3;

    /// <summary>ブラック弾内層微小核最小ピクセル半径</summary>
    public int SpecialBlackCoreInnerMinRadius { get; set; } = 2;

    /// <summary>ブラック弾内層微小核半径除数</summary>
    public int SpecialBlackCoreInnerDivisor { get; set; } = 2;

    /// <summary>ブラック弾内層微小核色 (<see cref="Color" />)</summary>
    public Color SpecialBlackCoreInnerColor { get; set; } = Color.FromArgb(255, 255, 180, 200);

    /// <summary>パーティクルエフェクト外枠ピクセル拡張幅</summary>
    public int ParticleOutlinePadding { get; set; } = 1;
}

/// <summary>
///     左上 HUD 操作案内テキストの 1 行（テキストと色）を表す設定クラス。
/// </summary>
public sealed class HudTextItem {
    /// <summary>案内テキスト文字列</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>文字色 (<see cref="Color" />)</summary>
    public Color Color { get; set; }
}

/// <summary>
///     左上 HUD 操作案内表示の設定クラス。
/// </summary>
public sealed class HudRenderConfig {
    /// <summary>タイトル X 座標</summary>
    public int TitleX { get; set; }

    /// <summary>タイトル Y 座標</summary>
    public int TitleY { get; set; }

    /// <summary>タイトル文字色 (<see cref="Color" />)</summary>
    public Color TitleColor { get; set; }

    /// <summary>タイトル文字列</summary>
    public string TitleText { get; set; } = string.Empty;

    /// <summary>行間隔ピクセル</summary>
    public int LineSpacing { get; set; }

    /// <summary>
    ///     操作案内テキスト項目のリスト。
    /// </summary>
    public List<HudTextItem> Text { get; set; } = [];

    /// <summary>Boss 切り替え操作案内テキスト</summary>
    public string BossSwitchGuideText { get; set; } = string.Empty;

    /// <summary>Boss 切り替え操作案内文字色 (<see cref="Color" />)</summary>
    public Color BossSwitchGuideColor { get; set; }

    /// <summary>キャラクター切り替え操作案内テキスト</summary>
    public string CharacterSwitchGuideText { get; set; } = string.Empty;

    /// <summary>キャラクター切り替え操作案内文字色 (<see cref="Color" />)</summary>
    public Color CharacterSwitchGuideColor { get; set; }

    /// <summary>二段ジャンプインジケータ X 座標</summary>
    public int DoubleJumpX { get; set; }

    /// <summary>二段ジャンプインジケータ Y 座標</summary>
    public int DoubleJumpY { get; set; }

    /// <summary>二段ジャンプ表示ラベル文字列</summary>
    public string DoubleJumpText { get; set; } = string.Empty;

    /// <summary>二段ジャンプ有効時シンボル色 (<see cref="Color" />)</summary>
    public Color DoubleJumpActiveColor { get; set; }

    /// <summary>二段ジャンプ消費時シンボル色 (<see cref="Color" />)</summary>
    public Color DoubleJumpInactiveColor { get; set; } = ColorPalette.Gray;

    /// <summary>プレイヤー HP 表示 X 座標</summary>
    public int PlayerHpX { get; set; }

    /// <summary>プレイヤー HP 表示 Y 座標</summary>
    public int PlayerHpY { get; set; }

    /// <summary>プレイヤー HP 表示ラベル文字列</summary>
    public string PlayerHpText { get; set; } = string.Empty;

    /// <summary>プレイヤー HP 高（3以上）文字色 (<see cref="Color" />)</summary>
    public Color PlayerHpHighColor { get; set; }

    /// <summary>プレイヤー HP 中（2）文字色 (<see cref="Color" />)</summary>
    public Color PlayerHpMidColor { get; set; } = ColorPalette.Gold;

    /// <summary>プレイヤー HP 低（1）文字色 (<see cref="Color" />)</summary>
    public Color PlayerHpLowColor { get; set; } = ColorPalette.Red;

    /// <summary>プレイヤー HP 死亡（0）文字色 (<see cref="Color" />)</summary>
    public Color PlayerHpDeadColor { get; set; } = ColorPalette.Gray;

    /// <summary>プレイヤー HP 高判定閾値比率</summary>
    public float PlayerHpHighThreshold { get; set; } = 0.66f;

    /// <summary>プレイヤー HP 中判定閾値比率</summary>
    public float PlayerHpMidThreshold { get; set; } = 0.33f;
}

/// <summary>
///     画面下部 Boss HP バーおよび状態表示の設定クラス。
/// </summary>
public sealed class BossHpBarRenderConfig {
    /// <summary>HP バー左上 X 座標</summary>
    public int BarX1 { get; set; }

    /// <summary>HP バー左上 Y 座標</summary>
    public int BarY1 { get; set; }

    /// <summary>HP バー右下 X 座標</summary>
    public int BarX2 { get; set; }

    /// <summary>HP バー右下 Y 座標</summary>
    public int BarY2 { get; set; }

    /// <summary>Phase 1 HP ゲージ色 (<see cref="Color" />)</summary>
    public Color Phase1Color { get; set; }

    /// <summary>Phase 2 HP ゲージ色 (<see cref="Color" />)</summary>
    public Color Phase2Color { get; set; }

    /// <summary>Phase 3 HP ゲージ色 (<see cref="Color" />)</summary>
    public Color Phase3Color { get; set; }

    /// <summary>HP バー背景外枠パディング</summary>
    public int BgPadding { get; set; }

    /// <summary>HP バー暗色背景色 (<see cref="Color" />)</summary>
    public Color BgColor { get; set; }

    /// <summary>HP バー外枠線色 (<see cref="Color" />)</summary>
    public Color BorderColor { get; set; }

    /// <summary>HP バー上面ハイライト厚み</summary>
    public int TopHighlightThickness { get; set; }

    /// <summary>HP バー上面ハイライト色 (<see cref="Color" />)</summary>
    public Color TopHighlightColor { get; set; }

    /// <summary>ヘッダー文字の Y 座標オフセット</summary>
    public int HeaderOffsetY { get; set; }

    /// <summary>Boss タイトル表示 X 軸オフセット (BarX1 からの相対距離)</summary>
    public int TitleOffsetX { get; set; }

    /// <summary>Boss タイトル表示 Y 軸オフセット (BarY1 から上方向へのオフセット、未指定時は HeaderOffsetY を使用)</summary>
    public int TitleOffsetY { get; set; }

    /// <summary>Boss タイトル文字色 (<see cref="Color" />)</summary>
    public Color TitleColor { get; set; }

    /// <summary>HP 数値テキストの右端 X 軸オフセット (BarX2 から左方向へのオフセット)</summary>
    public int HpTextOffsetRight { get; set; }

    /// <summary>HP 数値テキストの Y 軸オフセット (BarY1 から上方向へのオフセット、未指定時は HeaderOffsetY を使用)</summary>
    public int HpTextOffsetY { get; set; }

    /// <summary>HP 数値文字色 (<see cref="Color" />)</summary>
    public Color HpTextColor { get; set; }

    /// <summary>状態バッジテキストの X 軸オフセット (BarX1 からの相対距離)</summary>
    public int StateBadgeOffsetX { get; set; }

    /// <summary>状態バッジテキストの Y 軸オフセット (BarY1 から上方向へのオフセット、未指定時は HeaderOffsetY を使用)</summary>
    public int StateBadgeOffsetY { get; set; }

    /// <summary>前隙予兆状態バッジ文字色 (<see cref="Color" />)</summary>
    public Color StateTelegraphingColor { get; set; }

    /// <summary>発動中状態バッジ文字色 (<see cref="Color" />)</summary>
    public Color StateCastingColor { get; set; }

    /// <summary>隙あり虚弱状態バッジ文字色 (<see cref="Color" />)</summary>
    public Color StateRecoveringColor { get; set; }

    /// <summary>フェーズ遷移演出状態バッジ文字色 (<see cref="Color" />)</summary>
    public Color StatePhaseTransitionColor { get; set; }

    /// <summary>通常巡航状態バッジ文字色 (<see cref="Color" />)</summary>
    public Color StateDefaultColor { get; set; }

    /// <summary>気絶・易傷状態バッジ文字色 (<see cref="Color" />)</summary>
    public Color StateStunnedColor { get; set; } = ColorPalette.Gold;

    /// <summary>Phase 分割マーカー半幅 (px)</summary>
    public int PhaseSplitWidth { get; set; } = 1;
}

/// <summary>
///     敗北 (Game Over) および勝利 (Victory) モーダルダイアログの描画設定クラス。
/// </summary>
public sealed class ModalRenderConfig {
    /// <summary>Game Over 矩形左上 X 座標</summary>
    public int GameOverX1 { get; set; }

    /// <summary>Game Over 矩形左上 Y 座標</summary>
    public int GameOverY1 { get; set; }

    /// <summary>Game Over 矩形右下 X 座標</summary>
    public int GameOverX2 { get; set; }

    /// <summary>Game Over 矩形右下 Y 座標</summary>
    public int GameOverY2 { get; set; }

    /// <summary>Game Over 背景色 (<see cref="Color" />)</summary>
    public Color GameOverBgColor { get; set; }

    /// <summary>Game Over 外枠線色 (<see cref="Color" />)</summary>
    public Color GameOverOuterBorderColor { get; set; }

    /// <summary>Game Over 内枠線色 (<see cref="Color" />)</summary>
    public Color GameOverInnerBorderColor { get; set; }

    /// <summary>Game Over タイトル X 座標</summary>
    public int GameOverTitleX { get; set; }

    /// <summary>Game Over タイトル Y 座標</summary>
    public int GameOverTitleY { get; set; }

    /// <summary>Game Over タイトル文字色 (<see cref="Color" />)</summary>
    public Color GameOverTitleColor { get; set; }

    /// <summary>Game Over タイトル文字列</summary>
    public string GameOverTitleText { get; set; } = string.Empty;

    /// <summary>Game Over リトライテキスト X 座標</summary>
    public int GameOverRetryX { get; set; }

    /// <summary>Game Over リトライテキスト Y 座標</summary>
    public int GameOverRetryY { get; set; }

    /// <summary>Game Over リトライ文字色 (<see cref="Color" />)</summary>
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

    /// <summary>Victory 背景色 (<see cref="Color" />)</summary>
    public Color VictoryBgColor { get; set; }

    /// <summary>Victory 外枠線色 (<see cref="Color" />)</summary>
    public Color VictoryOuterBorderColor { get; set; }

    /// <summary>Victory 内枠線色 (<see cref="Color" />)</summary>
    public Color VictoryInnerBorderColor { get; set; }

    /// <summary>Victory タイトル X 座標</summary>
    public int VictoryTitleX { get; set; }

    /// <summary>Victory タイトル Y 座標</summary>
    public int VictoryTitleY { get; set; }

    /// <summary>Victory タイトル文字色 (<see cref="Color" />)</summary>
    public Color VictoryTitleColor { get; set; }

    /// <summary>Victory タイトル文字列</summary>
    public string VictoryTitleText { get; set; } = string.Empty;

    /// <summary>Victory リトライテキスト X 座標</summary>
    public int VictoryRetryX { get; set; }

    /// <summary>Victory リトライテキスト Y 座標</summary>
    public int VictoryRetryY { get; set; }

    /// <summary>Victory リトライ文字色 (<see cref="Color" />)</summary>
    public Color VictoryRetryColor { get; set; }

    /// <summary>Victory リトライ案内文字列</summary>
    public string VictoryRetryText { get; set; } = string.Empty;

    /// <summary>モーダルダイアログ内枠線のパディング (px)</summary>
    public int InnerBorderPadding { get; set; } = 2;
}

/// <summary>
///     Hitbox / Hurtbox / 衝突境界線デバッグ可視化描画の設定クラス。
/// </summary>
public sealed class DebugRenderConfig {
    /// <summary>
    ///     デバッグモード全体の有効フラグ（Config/render.toml 由来）。
    ///     false の場合、F1～F4 キーを含む全てのデバッグ入力および描画が無効化されます。
    /// </summary>
    public bool DebugMode { get; set; }

    /// <summary>
    ///     F1 キーによってトグルされるデバッグオーバーレイの表示状態。
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>起動時にデバッグ描画を有効にするか</summary>
    public bool Enabled { get; set; }

    /// <summary>プレイヤー地形衝突ボックス枠色 (<see cref="Color" />)</summary>
    public Color PlayerTerrainBoxColor { get; set; }

    /// <summary>プレイヤー被弾判定円色 (<see cref="Color" />)</summary>
    public Color PlayerHurtboxColor { get; set; }

    /// <summary>Boss 攻撃・被弾衝突ボックス枠色 (<see cref="Color" />)</summary>
    public Color BossHitboxColor { get; set; }

    /// <summary>Boss 弾幕判定円枠色 (<see cref="Color" />)</summary>
    public Color BossBulletHitboxColor { get; set; }

    /// <summary>プレイヤー銃弾判定ボックス枠色 (<see cref="Color" />)</summary>
    public Color PlayerBulletHitboxColor { get; set; }

    /// <summary>足場衝突境界枠色 (<see cref="Color" />)</summary>
    public Color PlatformHitboxColor { get; set; }

    /// <summary>デバッグ状態インジケータ文字色 (<see cref="Color" />)</summary>
    public Color IndicatorColor { get; set; }

    /// <summary>デバッグ状態インジケータ表示 X 座標</summary>
    public int IndicatorX { get; set; } = 980;

    /// <summary>デバッグ状態インジケータ表示 Y 座標</summary>
    public int IndicatorY { get; set; } = 16;

    /// <summary>デバッグ状態インジケータ表示文字列</summary>
    public string IndicatorText { get; set; } = "[DEBUG MODE ACTIVE - F1 to toggle]";

    /// <summary>デバッグフェーズ切替操作案内テキスト</summary>
    public string PhaseSwitchGuideText { get; set; } = "[F2] Phase 1   [F3] Phase 2   [F4] Phase 3";

    /// <summary>デバッグフェーズ切替操作案内文字色 (<see cref="Color" />)</summary>
    public Color PhaseSwitchGuideColor { get; set; } = ColorPalette.Gold;

    /// <summary>デバッグ操作案内表示 Y オフセット (px)</summary>
    public int GuideOffsetY { get; set; } = 24;

    /// <summary>中立陣営当たり判定色 (<see cref="Color" />)</summary>
    public Color NeutralHitboxColor { get; set; } = ColorPalette.Gray;

    /// <summary>自機デバッグ情報テキスト表示 X オフセット (px)</summary>
    public int PlayerInfoOffsetX { get; set; } = -10;

    /// <summary>自機デバッグ情報テキスト表示 Y オフセット (px)</summary>
    public int PlayerInfoOffsetY { get; set; } = -16;

    /// <summary>点衝突形状描画半径 (px)</summary>
    public int PointShapeRadius { get; set; } = 2;

    /// <summary>ナビゲーションメッシュノード描画色 (<see cref="Color" />)</summary>
    public Color NavNodeColor { get; set; } = ColorPalette.Cyan;

    /// <summary>ナビゲーション自由落下リンク描画色 (<see cref="Color" />)</summary>
    public Color NavEdgeDropColor { get; set; } = ColorPalette.Gold;

    /// <summary>ナビゲーションジャンプリンク描画色 (<see cref="Color" />)</summary>
    public Color NavEdgeJumpColor { get; set; } = ColorPalette.Green;

    /// <summary>ナビゲーションメッシュノード描画半径 (px)</summary>
    public int NavNodeRadius { get; set; } = 4;
}

/// <summary>
///     タクティカルエイムレティクル（鼠标准星）の描画設定クラス。
/// </summary>
public sealed class AimReticleRenderConfig {
    /// <summary>レティクル外周円環の基準半径 (px)</summary>
    public float BaseRadius { get; set; }

    /// <summary>射撃開火時の最大スプリング拡張振幅 (px)</summary>
    public float RecoilExpansion { get; set; }

    /// <summary>スプリング反動復元周波数 (Hz)</summary>
    public float SpringFrequency { get; set; }

    /// <summary>スプリング減衰比 (0.0: 無減衰, 1.0: 臨界減衰)</summary>
    public float SpringDampingRatio { get; set; }

    /// <summary>上下左右 4 方向刻度ショートラインの長さ (px)</summary>
    public int TickLength { get; set; }

    /// <summary>円環と刻度線の間のギャップ幅 (px)</summary>
    public int TickGap { get; set; }

    /// <summary>中心照準ドット半径 (px)</summary>
    public int CenterDotRadius { get; set; } = 1;

    /// <summary>中心照準ドット表示色 (<see cref="Color" />)</summary>
    public Color CenterDotColor { get; set; } = ColorPalette.White;

    /// <summary>外周タクティカルリング表示色 (<see cref="Color" />)</summary>
    public Color RingColor { get; set; } = ColorPalette.CyanLight;

    /// <summary>上下左右刻度ショートライン表示色 (<see cref="Color" />)</summary>
    public Color TickColor { get; set; } = ColorPalette.CyanNeon;

    /// <summary>照準リング最小半径クランプ (px)</summary>
    public int MinRadius { get; set; } = 1;

    /// <summary>照準リング最大半径クランプ (px)</summary>
    public int MaxRadius { get; set; } = 100;

    /// <summary>反動拡張上限倍率</summary>
    public float RecoilExpansionClampRatio { get; set; } = 3.0f;
}

/// <summary>
///     描画視口およびカメラカリング領域設定クラス。
/// </summary>
public sealed class ViewportRenderConfig {
    /// <summary>ビューポート左上 X 座標（ピクセル）</summary>
    public float X { get; set; }

    /// <summary>ビューポート左上 Y 座標（ピクセル）</summary>
    public float Y { get; set; }

    /// <summary>ビューポート幅（ピクセル）</summary>
    public float Width { get; set; }

    /// <summary>ビューポート高さ（ピクセル）</summary>
    public float Height { get; set; }

    /// <summary>タイルマップ未指定時のプラットフォーム代替塗りつぶし色</summary>
    public Color PlatformFallbackFillColor { get; set; } = ColorPalette.DarkBg;

    /// <summary>タイルマップ未指定時のプラットフォーム代替枠線色</summary>
    public Color PlatformFallbackBorderColor { get; set; } = ColorPalette.Cyan;
}

/// <summary>
///     自機プレイヤー描画設定クラス。
/// </summary>
public sealed class PlayerRenderConfig {
    /// <summary>無敵時スプライト点滅周波数 (Hz)</summary>
    public float InvincibleBlinkFrequency { get; set; } = 30.0f;
}

/// <summary>
///     汎用視覚演出・パーティクル描画パラメータ設定クラス。
/// </summary>
public sealed class VisualEffectsRenderConfig {
    public int HitSparkCount { get; set; }
    public float HitSparkMinSpeed { get; set; }
    public float HitSparkSpeedRange { get; set; }
    public float HitSparkRadius { get; set; }
    public float HitSparkDuration { get; set; }

    public int PlayerDeathParticleCount { get; set; }
    public float PlayerDeathParticleBaseSpeed { get; set; }
    public float PlayerDeathParticleSpeedStep { get; set; }
    public float PlayerDeathParticleRadius { get; set; }
    public float PlayerDeathParticleDuration { get; set; }

    public float ShockwaveInitialRadius { get; set; }
    public float ShockwaveMaxRadius { get; set; }
    public float ShockwaveDuration { get; set; }

    public int VictoryExplosionCount { get; set; }
    public float VictoryExplosionBaseSpeed { get; set; }
    public float VictoryExplosionSpeedStep { get; set; }
    public float VictoryExplosionRadius { get; set; }
    public float VictoryExplosionDuration { get; set; }
}

/// <summary>
///     近接斬撃エフェクトスプライト描画設定クラス。
/// </summary>
public sealed class SlashEffectRenderConfig {
    public int SpriteColIndex { get; set; }
    public int SpriteCellSize { get; set; }
    public float BossBaseReferenceSize { get; set; }
    public float PlayerBaseReferenceSize { get; set; }
    public float CenterBiasOffset { get; set; }
    public float PlayerSlashAlpha { get; set; }
    public Color PlayerSlashColor { get; set; }
}

/// <summary>
///     Config/render.toml から読み込まれる全体の描画および UI 設定クラス。
/// </summary>
public sealed class RenderConfig {
    /// <summary>ビューポートおよびカリング領域設定</summary>
    public ViewportRenderConfig Viewport { get; set; } = new();

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

    /// <summary>タクティカルエイムレティクル（鼠标准星）描画設定</summary>
    public AimReticleRenderConfig AimReticle { get; set; } = new();

    /// <summary>自機プレイヤー描画設定</summary>
    public PlayerRenderConfig Player { get; set; } = new();

    /// <summary>視覚エフェクト・パーティクル共通描画設定</summary>
    public VisualEffectsRenderConfig VisualEffects { get; set; } = new();

    /// <summary>近接斬撃エフェクトスプライト描画設定</summary>
    public SlashEffectRenderConfig SlashEffect { get; set; } = new();
}