using System.Drawing;
using AnimalBattle.Core;
using ErrorOr;
using Microsoft.Extensions.Configuration;

namespace AnimalBattle.Presentation.Rendering;

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

    private string _warningTextColorHex = string.Empty;
    private Color _warningTextColor;

    /// <summary>警告テキスト・マーカー表示色 Hex 文字列</summary>
    [ConfigurationKeyName("WarningTextColor")]
    public string WarningTextColorHex
    {
        get => _warningTextColorHex;
        set { _warningTextColorHex = value; _warningTextColor = value.ToColor(); }
    }

    /// <summary>警告テキスト・マーカー表示色 (<see cref="Color" />)</summary>
    public Color WarningTextColor => _warningTextColor;

    /// <summary>照準レーザー線の太さ (px)</summary>
    public int LaserThickness { get; set; }

    /// <summary>俯衝突進（DiveRam）予兆テキストの表示オフセット (px)</summary>
    public Vec2f DiveRamTextOffset { get; set; }

    /// <summary>泰山圧頂（LeapSmash）予兆テキストの表示オフセット (px)</summary>
    public Vec2f LeapSmashTextOffset { get; set; }

    /// <summary>撼地踏撃（Stomp）予兆テキストの表示オフセット (px)</summary>
    public Vec2f StompTextOffset { get; set; }

    /// <summary>落雨落石（Skyfall）予兆テキストの表示オフセット (px)</summary>
    public Vec2f SkyfallTextOffset { get; set; }

    /// <summary>易傷・隙あり（Vulnerable）時の醒目攻撃チャンス表示テキスト</summary>
    public string ChanceMarkerText { get; set; } = string.Empty;

    /// <summary>易傷時の醒目攻撃チャンス表示 Y 軸オフセット (px)</summary>
    public float ChanceMarkerOffsetY { get; set; }

    private string _chanceMarkerColorHex = string.Empty;
    private Color _chanceMarkerColor;

    /// <summary>易傷時の醒目攻撃チャンス表示色 Hex 文字列</summary>
    [ConfigurationKeyName("ChanceMarkerColor")]
    public string ChanceMarkerColorHex
    {
        get => _chanceMarkerColorHex;
        set { _chanceMarkerColorHex = value; _chanceMarkerColor = value.ToColor(); }
    }

    /// <summary>易傷時の醒目攻撃チャンス表示色 (<see cref="Color" />)</summary>
    public Color ChanceMarkerColor => _chanceMarkerColor;

    /// <summary>易傷時のオーラ脈動角速度</summary>
    public float VulnerablePulseSpeed { get; set; }

    /// <summary>易傷時のオーラ基礎アルファ値 (0～255)</summary>
    public int VulnerableAuraBaseAlpha { get; set; }

    /// <summary>易傷時のオーラ脈動アルファ振幅</summary>
    public int VulnerableAuraPulseAlpha { get; set; }

    /// <summary>易傷時のオーラ拡大量比率</summary>
    public float VulnerableAuraScale { get; set; }

    /// <summary>易傷マーカー浮動揺れ角速度</summary>
    public float ChanceMarkerBobbingSpeed { get; set; }

    /// <summary>易傷マーカー浮動揺れ振幅 (px)</summary>
    public float ChanceMarkerBobbingAmplitude { get; set; }

    /// <summary>易傷マーカー文字 1 文字あたりの推定幅 (px)</summary>
    public int ChanceMarkerCharWidth { get; set; }

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

    private string _invincibleAuraColorHex = string.Empty;
    private Color _invincibleAuraColor;

    /// <summary>金身オーラ発光色 Hex 文字列</summary>
    [ConfigurationKeyName("InvincibleAuraColor")]
    public string InvincibleAuraColorHex
    {
        get => _invincibleAuraColorHex;
        set { _invincibleAuraColorHex = value; _invincibleAuraColor = value.ToColor(); }
    }

    /// <summary>金身オーラ発光色 (<see cref="Color" />)</summary>
    public Color InvincibleAuraColor => _invincibleAuraColor;

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

    private string _playerBulletOuterColorHex = string.Empty;
    private Color _playerBulletOuterColor;

    /// <summary>プレイヤー弾外層グロー色 Hex 文字列</summary>
    [ConfigurationKeyName("PlayerBulletOuterColor")]
    public string PlayerBulletOuterColorHex
    {
        get => _playerBulletOuterColorHex;
        set { _playerBulletOuterColorHex = value; _playerBulletOuterColor = value.ToColor(); }
    }

    /// <summary>プレイヤー弾外層グロー色 (<see cref="Color" />)</summary>
    public Color PlayerBulletOuterColor => _playerBulletOuterColor;

    /// <summary>プレイヤー弾内層コア半径</summary>
    public float PlayerBulletInnerRadius { get; set; }

    private string _playerBulletInnerColorHex = string.Empty;
    private Color _playerBulletInnerColor;

    /// <summary>プレイヤー弾内層コア色 Hex 文字列</summary>
    [ConfigurationKeyName("PlayerBulletInnerColor")]
    public string PlayerBulletInnerColorHex
    {
        get => _playerBulletInnerColorHex;
        set { _playerBulletInnerColorHex = value; _playerBulletInnerColor = value.ToColor(); }
    }

    /// <summary>プレイヤー弾内層コア色 (<see cref="Color" />)</summary>
    public Color PlayerBulletInnerColor => _playerBulletInnerColor;

    /// <summary>プレイヤー弾ビームトレイル半サイズ (px)</summary>
    public Vec2i PlayerBulletTrailHalfSize { get; set; }

    private string _playerBulletTrailColorHex = string.Empty;
    private Color _playerBulletTrailColor;

    /// <summary>プレイヤー弾ビームトレイル色 Hex 文字列</summary>
    [ConfigurationKeyName("PlayerBulletTrailColor")]
    public string PlayerBulletTrailColorHex
    {
        get => _playerBulletTrailColorHex;
        set { _playerBulletTrailColorHex = value; _playerBulletTrailColor = value.ToColor(); }
    }

    /// <summary>プレイヤー弾ビームトレイル色 (<see cref="Color" />)</summary>
    public Color PlayerBulletTrailColor => _playerBulletTrailColor;

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

    private string _bossBulletCoreColorHex = string.Empty;
    private Color _bossBulletCoreColor;

    /// <summary>Boss 弾内層ホワイトコア色 Hex 文字列</summary>
    [ConfigurationKeyName("BossBulletCoreColor")]
    public string BossBulletCoreColorHex
    {
        get => _bossBulletCoreColorHex;
        set { _bossBulletCoreColorHex = value; _bossBulletCoreColor = value.ToColor(); }
    }

    /// <summary>Boss 弾内層ホワイトコア色 (<see cref="Color" />)</summary>
    public Color BossBulletCoreColor => _bossBulletCoreColor;

    /// <summary>Boss 弾外枠付加ピクセル半径</summary>
    public int BossBulletOutlinePadding { get; set; }

    /// <summary>画面外カリング（描画除外）マージン (ピクセル)</summary>
    public Vec2i ViewportCullingMargin { get; set; }

    /// <summary>マゼンタ弾オーラ基礎半径比率</summary>
    public float SpecialMagentaAuraBaseRatio { get; set; }

    /// <summary>マゼンタ弾オーラ脈動比率</summary>
    public float SpecialMagentaAuraPulseRatio { get; set; }

    /// <summary>マゼンタ弾脈動角速度</summary>
    public float SpecialMagentaPulseSpeed { get; set; }

    /// <summary>マゼンタ弾オーラアルファ値 (0～255)</summary>
    public int SpecialMagentaAuraAlpha { get; set; }

    /// <summary>シアン弾外周リング拡張幅 (px)</summary>
    public int SpecialCyanRingPadding { get; set; }

    private string _specialCyanRingColorHex = string.Empty;
    private Color _specialCyanRingColor;

    /// <summary>シアン弾外周リング発光色 Hex 文字列</summary>
    [ConfigurationKeyName("SpecialCyanRingColor")]
    public string SpecialCyanRingColorHex
    {
        get => _specialCyanRingColorHex;
        set { _specialCyanRingColorHex = value; _specialCyanRingColor = value.ToColor(); }
    }

    /// <summary>シアン弾外周リング発光色 (<see cref="Color" />)</summary>
    public Color SpecialCyanRingColor => _specialCyanRingColor;

    /// <summary>ブラック弾オーラ基礎半径比率</summary>
    public float SpecialBlackAuraBaseRatio { get; set; }

    /// <summary>ブラック弾オーラ脈動比率</summary>
    public float SpecialBlackAuraPulseRatio { get; set; }

    /// <summary>ブラック弾脈動角速度</summary>
    public float SpecialBlackPulseSpeed { get; set; }

    private string _specialBlackAuraColorHex = string.Empty;
    private Color _specialBlackAuraColor;

    /// <summary>ブラック弾オーラ色 Hex 文字列</summary>
    [ConfigurationKeyName("SpecialBlackAuraColor")]
    public string SpecialBlackAuraColorHex
    {
        get => _specialBlackAuraColorHex;
        set { _specialBlackAuraColorHex = value; _specialBlackAuraColor = value.ToColor(); }
    }

    /// <summary>ブラック弾オーラ色 (<see cref="Color" />)</summary>
    public Color SpecialBlackAuraColor => _specialBlackAuraColor;

    /// <summary>ブラック弾衛星公転半径比率</summary>
    public float SpecialBlackSatelliteOrbitRatio { get; set; }

    /// <summary>ブラック弾衛星公転角速度</summary>
    public float SpecialBlackSatelliteOrbitSpeed { get; set; }

    /// <summary>ブラック弾衛星半径比率</summary>
    public float SpecialBlackSatelliteRadiusRatio { get; set; }

    /// <summary>ブラック弾衛星最小ピクセル半径</summary>
    public int SpecialBlackSatelliteMinRadius { get; set; }

    /// <summary>ブラック弾内層微小核最小ピクセル半径</summary>
    public int SpecialBlackCoreInnerMinRadius { get; set; }

    /// <summary>ブラック弾内層微小核半径除数</summary>
    public int SpecialBlackCoreInnerDivisor { get; set; }

    private string _specialBlackCoreInnerColorHex = string.Empty;
    private Color _specialBlackCoreInnerColor;

    /// <summary>ブラック弾内層微小核色 Hex 文字列</summary>
    [ConfigurationKeyName("SpecialBlackCoreInnerColor")]
    public string SpecialBlackCoreInnerColorHex
    {
        get => _specialBlackCoreInnerColorHex;
        set { _specialBlackCoreInnerColorHex = value; _specialBlackCoreInnerColor = value.ToColor(); }
    }

    /// <summary>ブラック弾内層微小核色 (<see cref="Color" />)</summary>
    public Color SpecialBlackCoreInnerColor => _specialBlackCoreInnerColor;

    /// <summary>パーティクルエフェクト外枠ピクセル拡張幅</summary>
    public int ParticleOutlinePadding { get; set; }
}

/// <summary>
///     左上 HUD 操作案内テキストの 1 行（テキストと色）を表す設定クラス。
/// </summary>
public sealed class HudTextItem {
    /// <summary>案内テキスト文字列</summary>
    public string Text { get; set; } = string.Empty;

    private string _colorHex = string.Empty;
    private Color _color;

    /// <summary>文字色 Hex 文字列</summary>
    [ConfigurationKeyName("Color")]
    public string ColorHex
    {
        get => _colorHex;
        set { _colorHex = value; _color = value.ToColor(); }
    }

    /// <summary>文字色 (<see cref="Color" />)</summary>
    public Color Color => _color;
}

/// <summary>
///     左上 HUD 操作案内表示の設定クラス。
/// </summary>
public sealed class HudRenderConfig {
    /// <summary>タイトル表示座標</summary>
    public Vec2i TitlePos { get; set; }

    private string _titleColorHex = string.Empty;
    private Color _titleColor;

    /// <summary>タイトル文字色 Hex 文字列</summary>
    [ConfigurationKeyName("TitleColor")]
    public string TitleColorHex
    {
        get => _titleColorHex;
        set { _titleColorHex = value; _titleColor = value.ToColor(); }
    }

    /// <summary>タイトル文字色 (<see cref="Color" />)</summary>
    public Color TitleColor => _titleColor;

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

    private string _bossSwitchGuideColorHex = string.Empty;
    private Color _bossSwitchGuideColor;

    /// <summary>Boss 切り替え操作案内文字色 Hex 文字列</summary>
    [ConfigurationKeyName("BossSwitchGuideColor")]
    public string BossSwitchGuideColorHex
    {
        get => _bossSwitchGuideColorHex;
        set { _bossSwitchGuideColorHex = value; _bossSwitchGuideColor = value.ToColor(); }
    }

    /// <summary>Boss 切り替え操作案内文字色 (<see cref="Color" />)</summary>
    public Color BossSwitchGuideColor => _bossSwitchGuideColor;

    /// <summary>キャラクター切り替え操作案内テキスト</summary>
    public string CharacterSwitchGuideText { get; set; } = string.Empty;

    private string _characterSwitchGuideColorHex = string.Empty;
    private Color _characterSwitchGuideColor;

    /// <summary>キャラクター切り替え操作案内文字色 Hex 文字列</summary>
    [ConfigurationKeyName("CharacterSwitchGuideColor")]
    public string CharacterSwitchGuideColorHex
    {
        get => _characterSwitchGuideColorHex;
        set { _characterSwitchGuideColorHex = value; _characterSwitchGuideColor = value.ToColor(); }
    }

    /// <summary>キャラクター切り替え操作案内文字色 (<see cref="Color" />)</summary>
    public Color CharacterSwitchGuideColor => _characterSwitchGuideColor;

    /// <summary>二段ジャンプインジケータ表示座標</summary>
    public Vec2i DoubleJumpPos { get; set; }

    /// <summary>二段ジャンプ表示ラベル文字列</summary>
    public string DoubleJumpText { get; set; } = string.Empty;

    private string _doubleJumpActiveColorHex = string.Empty;
    private Color _doubleJumpActiveColor;

    /// <summary>二段ジャンプ有効時シンボル色 Hex 文字列</summary>
    [ConfigurationKeyName("DoubleJumpActiveColor")]
    public string DoubleJumpActiveColorHex
    {
        get => _doubleJumpActiveColorHex;
        set { _doubleJumpActiveColorHex = value; _doubleJumpActiveColor = value.ToColor(); }
    }

    /// <summary>二段ジャンプ有効時シンボル色 (<see cref="Color" />)</summary>
    public Color DoubleJumpActiveColor => _doubleJumpActiveColor;

    private string _doubleJumpInactiveColorHex = string.Empty;
    private Color _doubleJumpInactiveColor;

    /// <summary>二段ジャンプ消費時シンボル色 Hex 文字列</summary>
    [ConfigurationKeyName("DoubleJumpInactiveColor")]
    public string DoubleJumpInactiveColorHex
    {
        get => _doubleJumpInactiveColorHex;
        set { _doubleJumpInactiveColorHex = value; _doubleJumpInactiveColor = value.ToColor(); }
    }

    /// <summary>二段ジャンプ消費時シンボル色 (<see cref="Color" />)</summary>
    public Color DoubleJumpInactiveColor => _doubleJumpInactiveColor;

    /// <summary>プレイヤー HP 表示座標</summary>
    public Vec2i PlayerHpPos { get; set; }

    /// <summary>プレイヤー HP 表示ラベル文字列</summary>
    public string PlayerHpText { get; set; } = string.Empty;

    private string _playerHpHighColorHex = string.Empty;
    private Color _playerHpHighColor;

    /// <summary>プレイヤー HP 高（3以上）文字色 Hex 文字列</summary>
    [ConfigurationKeyName("PlayerHpHighColor")]
    public string PlayerHpHighColorHex
    {
        get => _playerHpHighColorHex;
        set { _playerHpHighColorHex = value; _playerHpHighColor = value.ToColor(); }
    }

    /// <summary>プレイヤー HP 高（3以上）文字色 (<see cref="Color" />)</summary>
    public Color PlayerHpHighColor => _playerHpHighColor;

    private string _playerHpMidColorHex = string.Empty;
    private Color _playerHpMidColor;

    /// <summary>プレイヤー HP 中（2）文字色 Hex 文字列</summary>
    [ConfigurationKeyName("PlayerHpMidColor")]
    public string PlayerHpMidColorHex
    {
        get => _playerHpMidColorHex;
        set { _playerHpMidColorHex = value; _playerHpMidColor = value.ToColor(); }
    }

    /// <summary>プレイヤー HP 中（2）文字色 (<see cref="Color" />)</summary>
    public Color PlayerHpMidColor => _playerHpMidColor;

    private string _playerHpLowColorHex = string.Empty;
    private Color _playerHpLowColor;

    /// <summary>プレイヤー HP 低（1）文字色 Hex 文字列</summary>
    [ConfigurationKeyName("PlayerHpLowColor")]
    public string PlayerHpLowColorHex
    {
        get => _playerHpLowColorHex;
        set { _playerHpLowColorHex = value; _playerHpLowColor = value.ToColor(); }
    }

    /// <summary>プレイヤー HP 低（1）文字色 (<see cref="Color" />)</summary>
    public Color PlayerHpLowColor => _playerHpLowColor;

    private string _playerHpDeadColorHex = string.Empty;
    private Color _playerHpDeadColor;

    /// <summary>プレイヤー HP 死亡（0）文字色 Hex 文字列</summary>
    [ConfigurationKeyName("PlayerHpDeadColor")]
    public string PlayerHpDeadColorHex
    {
        get => _playerHpDeadColorHex;
        set { _playerHpDeadColorHex = value; _playerHpDeadColor = value.ToColor(); }
    }

    /// <summary>プレイヤー HP 死亡（0）文字色 (<see cref="Color" />)</summary>
    public Color PlayerHpDeadColor => _playerHpDeadColor;

    /// <summary>プレイヤー HP 高判定閾値比率</summary>
    public float PlayerHpHighThreshold { get; set; }

    /// <summary>プレイヤー HP 中判定閾値比率</summary>
    public float PlayerHpMidThreshold { get; set; }
}

/// <summary>
///     画面下部 Boss HP バーおよび状態表示の設定クラス。
/// </summary>
public sealed class BossHpBarRenderConfig {
    /// <summary>HP バー左上座標</summary>
    public Vec2i BarPos1 { get; set; }

    /// <summary>HP バー右下座標</summary>
    public Vec2i BarPos2 { get; set; }

    private string _phase1ColorHex = string.Empty;
    private Color _phase1Color;

    /// <summary>Phase 1 HP ゲージ色 Hex 文字列</summary>
    [ConfigurationKeyName("Phase1Color")]
    public string Phase1ColorHex
    {
        get => _phase1ColorHex;
        set { _phase1ColorHex = value; _phase1Color = value.ToColor(); }
    }

    /// <summary>Phase 1 HP ゲージ色 (<see cref="Color" />)</summary>
    public Color Phase1Color => _phase1Color;

    private string _phase2ColorHex = string.Empty;
    private Color _phase2Color;

    /// <summary>Phase 2 HP ゲージ色 Hex 文字列</summary>
    [ConfigurationKeyName("Phase2Color")]
    public string Phase2ColorHex
    {
        get => _phase2ColorHex;
        set { _phase2ColorHex = value; _phase2Color = value.ToColor(); }
    }

    /// <summary>Phase 2 HP ゲージ色 (<see cref="Color" />)</summary>
    public Color Phase2Color => _phase2Color;

    private string _phase3ColorHex = string.Empty;
    private Color _phase3Color;

    /// <summary>Phase 3 HP ゲージ色 Hex 文字列</summary>
    [ConfigurationKeyName("Phase3Color")]
    public string Phase3ColorHex
    {
        get => _phase3ColorHex;
        set { _phase3ColorHex = value; _phase3Color = value.ToColor(); }
    }

    /// <summary>Phase 3 HP ゲージ色 (<see cref="Color" />)</summary>
    public Color Phase3Color => _phase3Color;

    /// <summary>HP バー背景外枠パディング</summary>
    public int BgPadding { get; set; }

    private string _bgColorHex = string.Empty;
    private Color _bgColor;

    /// <summary>HP バー暗色背景色 Hex 文字列</summary>
    [ConfigurationKeyName("BgColor")]
    public string BgColorHex
    {
        get => _bgColorHex;
        set { _bgColorHex = value; _bgColor = value.ToColor(); }
    }

    /// <summary>HP バー暗色背景色 (<see cref="Color" />)</summary>
    public Color BgColor => _bgColor;

    private string _borderColorHex = string.Empty;
    private Color _borderColor;

    /// <summary>HP バー外枠線色 Hex 文字列</summary>
    [ConfigurationKeyName("BorderColor")]
    public string BorderColorHex
    {
        get => _borderColorHex;
        set { _borderColorHex = value; _borderColor = value.ToColor(); }
    }

    /// <summary>HP バー外枠線色 (<see cref="Color" />)</summary>
    public Color BorderColor => _borderColor;

    /// <summary>HP バー上面ハイライト厚み</summary>
    public int TopHighlightThickness { get; set; }

    private string _topHighlightColorHex = string.Empty;
    private Color _topHighlightColor;

    /// <summary>HP バー上面ハイライト色 Hex 文字列</summary>
    [ConfigurationKeyName("TopHighlightColor")]
    public string TopHighlightColorHex
    {
        get => _topHighlightColorHex;
        set { _topHighlightColorHex = value; _topHighlightColor = value.ToColor(); }
    }

    /// <summary>HP バー上面ハイライト色 (<see cref="Color" />)</summary>
    public Color TopHighlightColor => _topHighlightColor;

    /// <summary>ヘッダー文字の Y 座標オフセット</summary>
    public int HeaderOffsetY { get; set; }

    /// <summary>Boss タイトル表示オフセット (BarPos1 からの相対距離)</summary>
    public Vec2i TitleOffset { get; set; }

    private string _titleColorHex = string.Empty;
    private Color _titleColor;

    /// <summary>Boss タイトル文字色 Hex 文字列</summary>
    [ConfigurationKeyName("TitleColor")]
    public string TitleColorHex
    {
        get => _titleColorHex;
        set { _titleColorHex = value; _titleColor = value.ToColor(); }
    }

    /// <summary>Boss タイトル文字色 (<see cref="Color" />)</summary>
    public Color TitleColor => _titleColor;

    /// <summary>HP 数値テキストの右端 X 軸オフセット (BarPos2.X から左方向へのオフセット)</summary>
    public int HpTextOffsetRight { get; set; }

    /// <summary>HP 数値テキストの Y 軸オフセット (BarPos1.Y から上方向へのオフセット、未指定時は HeaderOffsetY を使用)</summary>
    public int HpTextOffsetY { get; set; }

    private string _hpTextColorHex = string.Empty;
    private Color _hpTextColor;

    /// <summary>HP 数値文字色 Hex 文字列</summary>
    [ConfigurationKeyName("HpTextColor")]
    public string HpTextColorHex
    {
        get => _hpTextColorHex;
        set { _hpTextColorHex = value; _hpTextColor = value.ToColor(); }
    }

    /// <summary>HP 数値文字色 (<see cref="Color" />)</summary>
    public Color HpTextColor => _hpTextColor;

    /// <summary>状態バッジテキストのオフセット (BarPos1 からの相対距離)</summary>
    public Vec2i StateBadgeOffset { get; set; }

    private string _stateTelegraphingColorHex = string.Empty;
    private Color _stateTelegraphingColor;

    /// <summary>前隙予兆状態バッジ文字色 Hex 文字列</summary>
    [ConfigurationKeyName("StateTelegraphingColor")]
    public string StateTelegraphingColorHex
    {
        get => _stateTelegraphingColorHex;
        set { _stateTelegraphingColorHex = value; _stateTelegraphingColor = value.ToColor(); }
    }

    /// <summary>前隙予兆状態バッジ文字色 (<see cref="Color" />)</summary>
    public Color StateTelegraphingColor => _stateTelegraphingColor;

    private string _stateCastingColorHex = string.Empty;
    private Color _stateCastingColor;

    /// <summary>発動中状態バッジ文字色 Hex 文字列</summary>
    [ConfigurationKeyName("StateCastingColor")]
    public string StateCastingColorHex
    {
        get => _stateCastingColorHex;
        set { _stateCastingColorHex = value; _stateCastingColor = value.ToColor(); }
    }

    /// <summary>発動中状態バッジ文字色 (<see cref="Color" />)</summary>
    public Color StateCastingColor => _stateCastingColor;

    private string _stateRecoveringColorHex = string.Empty;
    private Color _stateRecoveringColor;

    /// <summary>隙あり虚弱状態バッジ文字色 Hex 文字列</summary>
    [ConfigurationKeyName("StateRecoveringColor")]
    public string StateRecoveringColorHex
    {
        get => _stateRecoveringColorHex;
        set { _stateRecoveringColorHex = value; _stateRecoveringColor = value.ToColor(); }
    }

    /// <summary>隙あり虚弱状態バッジ文字色 (<see cref="Color" />)</summary>
    public Color StateRecoveringColor => _stateRecoveringColor;

    private string _statePhaseTransitionColorHex = string.Empty;
    private Color _statePhaseTransitionColor;

    /// <summary>フェーズ遷移演出状態バッジ文字色 Hex 文字列</summary>
    [ConfigurationKeyName("StatePhaseTransitionColor")]
    public string StatePhaseTransitionColorHex
    {
        get => _statePhaseTransitionColorHex;
        set { _statePhaseTransitionColorHex = value; _statePhaseTransitionColor = value.ToColor(); }
    }

    /// <summary>フェーズ遷移演出状態バッジ文字色 (<see cref="Color" />)</summary>
    public Color StatePhaseTransitionColor => _statePhaseTransitionColor;

    private string _stateDefaultColorHex = string.Empty;
    private Color _stateDefaultColor;

    /// <summary>通常巡航状態バッジ文字色 Hex 文字列</summary>
    [ConfigurationKeyName("StateDefaultColor")]
    public string StateDefaultColorHex
    {
        get => _stateDefaultColorHex;
        set { _stateDefaultColorHex = value; _stateDefaultColor = value.ToColor(); }
    }

    /// <summary>通常巡航状態バッジ文字色 (<see cref="Color" />)</summary>
    public Color StateDefaultColor => _stateDefaultColor;

    private string _stateStunnedColorHex = string.Empty;
    private Color _stateStunnedColor;

    /// <summary>気絶・易傷状態バッジ文字色 Hex 文字列</summary>
    [ConfigurationKeyName("StateStunnedColor")]
    public string StateStunnedColorHex
    {
        get => _stateStunnedColorHex;
        set { _stateStunnedColorHex = value; _stateStunnedColor = value.ToColor(); }
    }

    /// <summary>気絶・易傷状態バッジ文字色 (<see cref="Color" />)</summary>
    public Color StateStunnedColor => _stateStunnedColor;

    /// <summary>Phase 分割マーカー半幅 (px)</summary>
    public int PhaseSplitWidth { get; set; }
}

/// <summary>
///     敗北 (Game Over) および勝利 (Victory) モーダルダイアログの描画設定クラス。
/// </summary>
public sealed class ModalRenderConfig {
    /// <summary>Game Over 矩形左上座標</summary>
    public Vec2i GameOverPos1 { get; set; }

    /// <summary>Game Over 矩形右下座標</summary>
    public Vec2i GameOverPos2 { get; set; }

    private string _gameOverBgColorHex = string.Empty;
    private Color _gameOverBgColor;

    /// <summary>Game Over 背景色 Hex 文字列</summary>
    [ConfigurationKeyName("GameOverBgColor")]
    public string GameOverBgColorHex
    {
        get => _gameOverBgColorHex;
        set { _gameOverBgColorHex = value; _gameOverBgColor = value.ToColor(); }
    }

    /// <summary>Game Over 背景色 (<see cref="Color" />)</summary>
    public Color GameOverBgColor => _gameOverBgColor;

    private string _gameOverOuterBorderColorHex = string.Empty;
    private Color _gameOverOuterBorderColor;

    /// <summary>Game Over 外枠線色 Hex 文字列</summary>
    [ConfigurationKeyName("GameOverOuterBorderColor")]
    public string GameOverOuterBorderColorHex
    {
        get => _gameOverOuterBorderColorHex;
        set { _gameOverOuterBorderColorHex = value; _gameOverOuterBorderColor = value.ToColor(); }
    }

    /// <summary>Game Over 外枠線色 (<see cref="Color" />)</summary>
    public Color GameOverOuterBorderColor => _gameOverOuterBorderColor;

    private string _gameOverInnerBorderColorHex = string.Empty;
    private Color _gameOverInnerBorderColor;

    /// <summary>Game Over 内枠線色 Hex 文字列</summary>
    [ConfigurationKeyName("GameOverInnerBorderColor")]
    public string GameOverInnerBorderColorHex
    {
        get => _gameOverInnerBorderColorHex;
        set { _gameOverInnerBorderColorHex = value; _gameOverInnerBorderColor = value.ToColor(); }
    }

    /// <summary>Game Over 内枠線色 (<see cref="Color" />)</summary>
    public Color GameOverInnerBorderColor => _gameOverInnerBorderColor;

    /// <summary>Game Over タイトル表示座標</summary>
    public Vec2i GameOverTitlePos { get; set; }

    private string _gameOverTitleColorHex = string.Empty;
    private Color _gameOverTitleColor;

    /// <summary>Game Over タイトル文字色 Hex 文字列</summary>
    [ConfigurationKeyName("GameOverTitleColor")]
    public string GameOverTitleColorHex
    {
        get => _gameOverTitleColorHex;
        set { _gameOverTitleColorHex = value; _gameOverTitleColor = value.ToColor(); }
    }

    /// <summary>Game Over タイトル文字色 (<see cref="Color" />)</summary>
    public Color GameOverTitleColor => _gameOverTitleColor;

    /// <summary>Game Over タイトル文字列</summary>
    public string GameOverTitleText { get; set; } = string.Empty;

    /// <summary>Game Over リトライテキスト表示座標</summary>
    public Vec2i GameOverRetryPos { get; set; }

    private string _gameOverRetryColorHex = string.Empty;
    private Color _gameOverRetryColor;

    /// <summary>Game Over リトライ文字色 Hex 文字列</summary>
    [ConfigurationKeyName("GameOverRetryColor")]
    public string GameOverRetryColorHex
    {
        get => _gameOverRetryColorHex;
        set { _gameOverRetryColorHex = value; _gameOverRetryColor = value.ToColor(); }
    }

    /// <summary>Game Over リトライ文字色 (<see cref="Color" />)</summary>
    public Color GameOverRetryColor => _gameOverRetryColor;

    /// <summary>Game Over リトライ案内文字列</summary>
    public string GameOverRetryText { get; set; } = string.Empty;

    /// <summary>Victory 矩形左上座標</summary>
    public Vec2i VictoryPos1 { get; set; }

    /// <summary>Victory 矩形右下座標</summary>
    public Vec2i VictoryPos2 { get; set; }

    private string _victoryBgColorHex = string.Empty;
    private Color _victoryBgColor;

    /// <summary>Victory 背景色 Hex 文字列</summary>
    [ConfigurationKeyName("VictoryBgColor")]
    public string VictoryBgColorHex
    {
        get => _victoryBgColorHex;
        set { _victoryBgColorHex = value; _victoryBgColor = value.ToColor(); }
    }

    /// <summary>Victory 背景色 (<see cref="Color" />)</summary>
    public Color VictoryBgColor => _victoryBgColor;

    private string _victoryOuterBorderColorHex = string.Empty;
    private Color _victoryOuterBorderColor;

    /// <summary>Victory 外枠線色 Hex 文字列</summary>
    [ConfigurationKeyName("VictoryOuterBorderColor")]
    public string VictoryOuterBorderColorHex
    {
        get => _victoryOuterBorderColorHex;
        set { _victoryOuterBorderColorHex = value; _victoryOuterBorderColor = value.ToColor(); }
    }

    /// <summary>Victory 外枠線色 (<see cref="Color" />)</summary>
    public Color VictoryOuterBorderColor => _victoryOuterBorderColor;

    private string _victoryInnerBorderColorHex = string.Empty;
    private Color _victoryInnerBorderColor;

    /// <summary>Victory 内枠線色 Hex 文字列</summary>
    [ConfigurationKeyName("VictoryInnerBorderColor")]
    public string VictoryInnerBorderColorHex
    {
        get => _victoryInnerBorderColorHex;
        set { _victoryInnerBorderColorHex = value; _victoryInnerBorderColor = value.ToColor(); }
    }

    /// <summary>Victory 内枠線色 (<see cref="Color" />)</summary>
    public Color VictoryInnerBorderColor => _victoryInnerBorderColor;

    /// <summary>Victory タイトル表示座標</summary>
    public Vec2i VictoryTitlePos { get; set; }

    private string _victoryTitleColorHex = string.Empty;
    private Color _victoryTitleColor;

    /// <summary>Victory タイトル文字色 Hex 文字列</summary>
    [ConfigurationKeyName("VictoryTitleColor")]
    public string VictoryTitleColorHex
    {
        get => _victoryTitleColorHex;
        set { _victoryTitleColorHex = value; _victoryTitleColor = value.ToColor(); }
    }

    /// <summary>Victory タイトル文字色 (<see cref="Color" />)</summary>
    public Color VictoryTitleColor => _victoryTitleColor;

    /// <summary>Victory タイトル文字列</summary>
    public string VictoryTitleText { get; set; } = string.Empty;

    /// <summary>Victory リトライテキスト表示座標</summary>
    public Vec2i VictoryRetryPos { get; set; }

    private string _victoryRetryColorHex = string.Empty;
    private Color _victoryRetryColor;

    /// <summary>Victory リトライ文字色 Hex 文字列</summary>
    [ConfigurationKeyName("VictoryRetryColor")]
    public string VictoryRetryColorHex
    {
        get => _victoryRetryColorHex;
        set { _victoryRetryColorHex = value; _victoryRetryColor = value.ToColor(); }
    }

    /// <summary>Victory リトライ文字色 (<see cref="Color" />)</summary>
    public Color VictoryRetryColor => _victoryRetryColor;

    /// <summary>Victory リトライ案内文字列</summary>
    public string VictoryRetryText { get; set; } = string.Empty;

    /// <summary>モーダルダイアログ内枠線のパディング (px)</summary>
    public int InnerBorderPadding { get; set; }
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

    private string _playerTerrainBoxColorHex = string.Empty;
    private Color _playerTerrainBoxColor;

    /// <summary>プレイヤー地形衝突ボックス枠色 Hex 文字列</summary>
    [ConfigurationKeyName("PlayerTerrainBoxColor")]
    public string PlayerTerrainBoxColorHex
    {
        get => _playerTerrainBoxColorHex;
        set { _playerTerrainBoxColorHex = value; _playerTerrainBoxColor = value.ToColor(); }
    }

    /// <summary>プレイヤー地形衝突ボックス枠色 (<see cref="Color" />)</summary>
    public Color PlayerTerrainBoxColor => _playerTerrainBoxColor;

    private string _playerHurtboxColorHex = string.Empty;
    private Color _playerHurtboxColor;

    /// <summary>プレイヤー被弾判定円色 Hex 文字列</summary>
    [ConfigurationKeyName("PlayerHurtboxColor")]
    public string PlayerHurtboxColorHex
    {
        get => _playerHurtboxColorHex;
        set { _playerHurtboxColorHex = value; _playerHurtboxColor = value.ToColor(); }
    }

    /// <summary>プレイヤー被弾判定円色 (<see cref="Color" />)</summary>
    public Color PlayerHurtboxColor => _playerHurtboxColor;

    private string _bossHitboxColorHex = string.Empty;
    private Color _bossHitboxColor;

    /// <summary>Boss 攻撃・被弾衝突ボックス枠色 Hex 文字列</summary>
    [ConfigurationKeyName("BossHitboxColor")]
    public string BossHitboxColorHex
    {
        get => _bossHitboxColorHex;
        set { _bossHitboxColorHex = value; _bossHitboxColor = value.ToColor(); }
    }

    /// <summary>Boss 攻撃・被弾衝突ボックス枠色 (<see cref="Color" />)</summary>
    public Color BossHitboxColor => _bossHitboxColor;

    private string _bossBulletHitboxColorHex = string.Empty;
    private Color _bossBulletHitboxColor;

    /// <summary>Boss 弾幕判定円枠色 Hex 文字列</summary>
    [ConfigurationKeyName("BossBulletHitboxColor")]
    public string BossBulletHitboxColorHex
    {
        get => _bossBulletHitboxColorHex;
        set { _bossBulletHitboxColorHex = value; _bossBulletHitboxColor = value.ToColor(); }
    }

    /// <summary>Boss 弾幕判定円枠色 (<see cref="Color" />)</summary>
    public Color BossBulletHitboxColor => _bossBulletHitboxColor;

    private string _playerBulletHitboxColorHex = string.Empty;
    private Color _playerBulletHitboxColor;

    /// <summary>プレイヤー銃弾判定ボックス枠色 Hex 文字列</summary>
    [ConfigurationKeyName("PlayerBulletHitboxColor")]
    public string PlayerBulletHitboxColorHex
    {
        get => _playerBulletHitboxColorHex;
        set { _playerBulletHitboxColorHex = value; _playerBulletHitboxColor = value.ToColor(); }
    }

    /// <summary>プレイヤー銃弾判定ボックス枠色 (<see cref="Color" />)</summary>
    public Color PlayerBulletHitboxColor => _playerBulletHitboxColor;

    private string _platformHitboxColorHex = string.Empty;
    private Color _platformHitboxColor;

    /// <summary>足場衝突境界枠色 Hex 文字列</summary>
    [ConfigurationKeyName("PlatformHitboxColor")]
    public string PlatformHitboxColorHex
    {
        get => _platformHitboxColorHex;
        set { _platformHitboxColorHex = value; _platformHitboxColor = value.ToColor(); }
    }

    /// <summary>足場衝突境界枠色 (<see cref="Color" />)</summary>
    public Color PlatformHitboxColor => _platformHitboxColor;

    private string _indicatorColorHex = string.Empty;
    private Color _indicatorColor;

    /// <summary>デバッグ状態インジケータ文字色 Hex 文字列</summary>
    [ConfigurationKeyName("IndicatorColor")]
    public string IndicatorColorHex
    {
        get => _indicatorColorHex;
        set { _indicatorColorHex = value; _indicatorColor = value.ToColor(); }
    }

    /// <summary>デバッグ状態インジケータ文字色 (<see cref="Color" />)</summary>
    public Color IndicatorColor => _indicatorColor;

    /// <summary>デバッグ状態インジケータ表示座標</summary>
    public Vec2i IndicatorPos { get; set; }

    /// <summary>デバッグ状態インジケータ表示文字列</summary>
    public string IndicatorText { get; set; } = string.Empty;

    /// <summary>デバッグフェーズ切替操作案内テキスト</summary>
    public string PhaseSwitchGuideText { get; set; } = string.Empty;

    private string _phaseSwitchGuideColorHex = string.Empty;
    private Color _phaseSwitchGuideColor;

    /// <summary>デバッグフェーズ切替操作案内文字色 Hex 文字列</summary>
    [ConfigurationKeyName("PhaseSwitchGuideColor")]
    public string PhaseSwitchGuideColorHex
    {
        get => _phaseSwitchGuideColorHex;
        set { _phaseSwitchGuideColorHex = value; _phaseSwitchGuideColor = value.ToColor(); }
    }

    /// <summary>デバッグフェーズ切替操作案内文字色 (<see cref="Color" />)</summary>
    public Color PhaseSwitchGuideColor => _phaseSwitchGuideColor;

    /// <summary>デバッグ操作案内表示 Y オフセット (px)</summary>
    public int GuideOffsetY { get; set; }

    private string _neutralHitboxColorHex = string.Empty;
    private Color _neutralHitboxColor;

    /// <summary>中立陣営当たり判定色 Hex 文字列</summary>
    [ConfigurationKeyName("NeutralHitboxColor")]
    public string NeutralHitboxColorHex
    {
        get => _neutralHitboxColorHex;
        set { _neutralHitboxColorHex = value; _neutralHitboxColor = value.ToColor(); }
    }

    /// <summary>中立陣営当たり判定色 (<see cref="Color" />)</summary>
    public Color NeutralHitboxColor => _neutralHitboxColor;

    /// <summary>自機デバッグ情報テキスト表示オフセット (px)</summary>
    public Vec2i PlayerInfoOffset { get; set; }

    /// <summary>点衝突形状描画半径 (px)</summary>
    public int PointShapeRadius { get; set; }

    private string _navNodeColorHex = string.Empty;
    private Color _navNodeColor;

    /// <summary>ナビゲーションメッシュノード描画色 Hex 文字列</summary>
    [ConfigurationKeyName("NavNodeColor")]
    public string NavNodeColorHex
    {
        get => _navNodeColorHex;
        set { _navNodeColorHex = value; _navNodeColor = value.ToColor(); }
    }

    /// <summary>ナビゲーションメッシュノード描画色 (<see cref="Color" />)</summary>
    public Color NavNodeColor => _navNodeColor;

    private string _navEdgeDropColorHex = string.Empty;
    private Color _navEdgeDropColor;

    /// <summary>ナビゲーション自由落下リンク描画色 Hex 文字列</summary>
    [ConfigurationKeyName("NavEdgeDropColor")]
    public string NavEdgeDropColorHex
    {
        get => _navEdgeDropColorHex;
        set { _navEdgeDropColorHex = value; _navEdgeDropColor = value.ToColor(); }
    }

    /// <summary>ナビゲーション自由落下リンク描画色 (<see cref="Color" />)</summary>
    public Color NavEdgeDropColor => _navEdgeDropColor;

    private string _navEdgeJumpColorHex = string.Empty;
    private Color _navEdgeJumpColor;

    /// <summary>ナビゲーションジャンプリンク描画色 Hex 文字列</summary>
    [ConfigurationKeyName("NavEdgeJumpColor")]
    public string NavEdgeJumpColorHex
    {
        get => _navEdgeJumpColorHex;
        set { _navEdgeJumpColorHex = value; _navEdgeJumpColor = value.ToColor(); }
    }

    /// <summary>ナビゲーションジャンプリンク描画色 (<see cref="Color" />)</summary>
    public Color NavEdgeJumpColor => _navEdgeJumpColor;

    /// <summary>ナビゲーションメッシュノード描画半径 (px)</summary>
    public int NavNodeRadius { get; set; }
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
    public int CenterDotRadius { get; set; }

    private string _centerDotColorHex = string.Empty;
    private Color _centerDotColor;

    /// <summary>中心照準ドット表示色 Hex 文字列</summary>
    [ConfigurationKeyName("CenterDotColor")]
    public string CenterDotColorHex
    {
        get => _centerDotColorHex;
        set { _centerDotColorHex = value; _centerDotColor = value.ToColor(); }
    }

    /// <summary>中心照準ドット表示色 (<see cref="Color" />)</summary>
    public Color CenterDotColor => _centerDotColor;

    private string _ringColorHex = string.Empty;
    private Color _ringColor;

    /// <summary>外周タクティカルリング表示色 Hex 文字列</summary>
    [ConfigurationKeyName("RingColor")]
    public string RingColorHex
    {
        get => _ringColorHex;
        set { _ringColorHex = value; _ringColor = value.ToColor(); }
    }

    /// <summary>外周タクティカルリング表示色 (<see cref="Color" />)</summary>
    public Color RingColor => _ringColor;

    private string _tickColorHex = string.Empty;
    private Color _tickColor;

    /// <summary>上下左右刻度ショートライン表示色 Hex 文字列</summary>
    [ConfigurationKeyName("TickColor")]
    public string TickColorHex
    {
        get => _tickColorHex;
        set { _tickColorHex = value; _tickColor = value.ToColor(); }
    }

    /// <summary>上下左右刻度ショートライン表示色 (<see cref="Color" />)</summary>
    public Color TickColor => _tickColor;

    /// <summary>照準リング最小半径クランプ (px)</summary>
    public int MinRadius { get; set; }

    /// <summary>照準リング最大半径クランプ (px)</summary>
    public int MaxRadius { get; set; }

    /// <summary>反動拡張上限倍率</summary>
    public float RecoilExpansionClampRatio { get; set; }
}

/// <summary>
///     描画視口およびカメラカリング領域設定クラス。
/// </summary>
public sealed class ViewportRenderConfig {
    /// <summary>ビューポート左上座標（ピクセル）</summary>
    public Vec2f Pos { get; set; }

    /// <summary>ビューポートサイズ（ピクセル）</summary>
    public Vec2f Size { get; set; }

    private string _platformFallbackFillColorHex = string.Empty;
    private Color _platformFallbackFillColor;

    /// <summary>タイルマップ未指定時のプラットフォーム代替塗りつぶし色 Hex 文字列</summary>
    [ConfigurationKeyName("PlatformFallbackFillColor")]
    public string PlatformFallbackFillColorHex
    {
        get => _platformFallbackFillColorHex;
        set { _platformFallbackFillColorHex = value; _platformFallbackFillColor = value.ToColor(); }
    }

    /// <summary>タイルマップ未指定時のプラットフォーム代替塗りつぶし色</summary>
    public Color PlatformFallbackFillColor => _platformFallbackFillColor;

    private string _platformFallbackBorderColorHex = string.Empty;
    private Color _platformFallbackBorderColor;

    /// <summary>タイルマップ未指定時のプラットフォーム代替枠線色 Hex 文字列</summary>
    [ConfigurationKeyName("PlatformFallbackBorderColor")]
    public string PlatformFallbackBorderColorHex
    {
        get => _platformFallbackBorderColorHex;
        set { _platformFallbackBorderColorHex = value; _platformFallbackBorderColor = value.ToColor(); }
    }

    /// <summary>タイルマップ未指定時のプラットフォーム代替枠線色</summary>
    public Color PlatformFallbackBorderColor => _platformFallbackBorderColor;
}

/// <summary>
///     自機プレイヤー描画設定クラス。
/// </summary>
public sealed class PlayerRenderConfig {
    /// <summary>無敵時スプライト点滅周波数 (Hz)</summary>
    public float InvincibleBlinkFrequency { get; set; }
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

    private string _playerSlashColorHex = string.Empty;
    private Color _playerSlashColor;

    /// <summary>自機近接斬撃エフェクト色 Hex 文字列</summary>
    [ConfigurationKeyName("PlayerSlashColor")]
    public string PlayerSlashColorHex
    {
        get => _playerSlashColorHex;
        set { _playerSlashColorHex = value; _playerSlashColor = value.ToColor(); }
    }

    /// <summary>自機近接斬撃エフェクト色 (<see cref="Color" />)</summary>
    public Color PlayerSlashColor => _playerSlashColor;
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