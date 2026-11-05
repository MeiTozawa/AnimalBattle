using System.Drawing;
using Microsoft.Extensions.Configuration;

namespace AnimalBattle.Presentation.Rendering;

/// <summary>
///     Config/boss.render.toml から読み込まれる Boss 関連の描画設定ルートクラス。
/// </summary>
public sealed class BossRenderConfig
{
    /// <summary>Boss 描画エフェクト設定</summary>
    public BossEffectsRenderConfig BossEffects { get; set; } = new();

    /// <summary>画面下部 Boss HP バー描画設定</summary>
    public BossHpBarRenderConfig BossHpBar { get; set; } = new();
}

/// <summary>
///     Boss の攻撃予兆・無敵シールド等の描画エフェクト設定クラス。
/// </summary>
public sealed class BossEffectsRenderConfig
{
    /// <summary>危険判定矩形 の半透明アルファ値 0から255の範囲</summary>
    public int DangerBoxAlpha { get; set; }

    /// <summary>照準レーザー の半透明アルファ値 0から255の範囲</summary>
    public int LaserAlpha { get; set; }

    /// <summary>突進軌道レーン の半透明アルファ値 0から255の範囲</summary>
    public int ChargeTrackAlpha { get; set; }

    private Color _warningTextColor;

    /// <summary>警告テキスト・マーカー表示色 Hex 文字列</summary>
    [ConfigurationKeyName("WarningTextColor")]
    public string WarningTextColorHex
    {
        get => string.Empty;
        set { _warningTextColor = value.ToColor(); }
    }

    /// <summary>警告テキスト・マーカー表示色 <see cref="Color" /></summary>
    public Color WarningTextColor => _warningTextColor;

    /// <summary>照準レーザー線の太さ ピクセル単位</summary>
    public int LaserThickness { get; set; }

    /// <summary>俯衝突進：DiveRam予兆テキストの表示オフセット ピクセル単位</summary>
    public Vec2f DiveRamTextOffset { get; set; }

    /// <summary>泰山圧頂：LeapSmash予兆テキストの表示オフセット ピクセル単位</summary>
    public Vec2f LeapSmashTextOffset { get; set; }

    /// <summary>撼地踏撃：Stomp予兆テキストの表示オフセット ピクセル単位</summary>
    public Vec2f StompTextOffset { get; set; }

    /// <summary>落雨落石：Skyfall予兆テキストの表示オフセット ピクセル単位</summary>
    public Vec2f SkyfallTextOffset { get; set; }

    /// <summary>隙あり・反撃チャンス表示テキスト</summary>
    public string ChanceMarkerText { get; set; } = string.Empty;

    /// <summary>隙あり表示 Y 軸オフセット ピクセル単位</summary>
    public float ChanceMarkerOffsetY { get; set; }

    private Color _chanceMarkerColor;

    /// <summary>隙あり表示色 Hex 文字列</summary>
    [ConfigurationKeyName("ChanceMarkerColor")]
    public string ChanceMarkerColorHex
    {
        get => string.Empty;
        set { _chanceMarkerColor = value.ToColor(); }
    }

    /// <summary>隙あり表示色 <see cref="Color" /></summary>
    public Color ChanceMarkerColor => _chanceMarkerColor;

    /// <summary>隙ありオーラ脈動角速度</summary>
    public float VulnerablePulseSpeed { get; set; }

    /// <summary>隙ありオーラ基礎アルファ値 0から255の範囲</summary>
    public int VulnerableAuraBaseAlpha { get; set; }

    /// <summary>隙ありオーラ脈動アルファ振幅</summary>
    public int VulnerableAuraPulseAlpha { get; set; }

    /// <summary>隙ありオーラ拡大量比率</summary>
    public float VulnerableAuraScale { get; set; }

    /// <summary>隙ありマーカー浮動揺れ角速度</summary>
    public float ChanceMarkerBobbingSpeed { get; set; }

    /// <summary>隙ありマーカー浮動揺れ振幅 ピクセル単位</summary>
    public float ChanceMarkerBobbingAmplitude { get; set; }

    /// <summary>無敵・フェーズ遷移シールドの基準半径 ピクセル単位</summary>
    public float InvincibleShieldRadius { get; set; }

    /// <summary>金身オーラ拡大倍率</summary>
    public float InvincibleAuraScale { get; set; }

    /// <summary>金身脈動角速度</summary>
    public float InvinciblePulseSpeed { get; set; }

    /// <summary>金身オーラ基礎アルファ値 0から255の範囲</summary>
    public int InvincibleAuraBaseAlpha { get; set; }

    /// <summary>金身オーラ脈動振幅アルファ値</summary>
    public int InvincibleAuraPulseAlpha { get; set; }

    private Color _invincibleAuraColor;

    /// <summary>金身オーラ発光色 Hex 文字列</summary>
    [ConfigurationKeyName("InvincibleAuraColor")]
    public string InvincibleAuraColorHex
    {
        get => string.Empty;
        set { _invincibleAuraColor = value.ToColor(); }
    }

    /// <summary>金身オーラ発光色 <see cref="Color" /></summary>
    public Color InvincibleAuraColor => _invincibleAuraColor;

    /// <summary>シールド結界呼吸周波数</summary>
    public float InvincibleBreathSpeed { get; set; }

    /// <summary>シールド結界呼吸振幅 ピクセル単位</summary>
    public float InvincibleBreathAmplitude { get; set; }

    /// <summary>シールド内層塗りつぶし基礎アルファ値 0から255の範囲</summary>
    public int InvincibleFillBaseAlpha { get; set; }

    /// <summary>シールド内層塗りつぶし脈動アルファ値</summary>
    public int InvincibleFillPulseAlpha { get; set; }

    /// <summary>シールド外周枠線基礎アルファ値 0から255の範囲</summary>
    public int InvincibleRingBaseAlpha { get; set; }

    /// <summary>シールド外周枠線脈動アルファ値</summary>
    public int InvincibleRingPulseAlpha { get; set; }

    /// <summary>排斥衝撃波拡散速度</summary>
    public float InvincibleRippleSpeed { get; set; }

    /// <summary>排斥衝撃波開始半径比率</summary>
    public float InvincibleRippleStartRatio { get; set; }

    /// <summary>排斥衝撃波最大拡大量比率</summary>
    public float InvincibleRippleExpandRatio { get; set; }

    /// <summary>排斥衝撃波最大アルファ値 0から255の範囲</summary>
    public int InvincibleRippleMaxAlpha { get; set; }

    /// <summary>四元防護ノード公転角速度</summary>
    public float InvincibleOrbitSpeed { get; set; }

    /// <summary>防護ノード外周光輪半径 ピクセル単位</summary>
    public int InvincibleNodeOuterRadius { get; set; }

    /// <summary>防護ノード中心白熱コア半径 ピクセル単位</summary>
    public int InvincibleNodeInnerRadius { get; set; }

    /// <summary>防護ノードアルファ値 0から255の範囲</summary>
    public int InvincibleNodeAlpha { get; set; }

    /// <summary>防護結界光線基礎アルファ値 0から255の範囲</summary>
    public int InvinciblePrismAlphaBase { get; set; }

    /// <summary>防護結界光線脈動アルファ値</summary>
    public int InvinciblePrismAlphaPulse { get; set; }

    /// <summary>防護結界光線線の太さ ピクセル単位</summary>
    public int InvinciblePrismThickness { get; set; }

    /// <summary>核心十字星芒自転角速度</summary>
    public float InvincibleCrossAngleSpeed { get; set; }

    /// <summary>核心十字星芒長さ比率 シールド半径に対する比率</summary>
    public float InvincibleCrossLengthRatio { get; set; }

    /// <summary>核心十字星芒基礎アルファ値 0から255の範囲</summary>
    public int InvincibleCrossAlphaBase { get; set; }

    /// <summary>核心十字星芒脈動アルファ値</summary>
    public int InvincibleCrossAlphaPulse { get; set; }

    /// <summary>核心十字星芒線の太さ ピクセル単位</summary>
    public int InvincibleCrossThickness { get; set; }

    /// <summary>Boss 浮遊時の揺れ角度振幅</summary>
    public double BossHoverAngleAmp { get; set; }

    /// <summary>Boss 浮遊時の揺れ角速度</summary>
    public float BossHoverAngleSpeed { get; set; }

    private Color _invincibleShieldFillColor;

    /// <summary>無敵シールド内部塗りつぶし色 Hex 文字列</summary>
    [ConfigurationKeyName("InvincibleShieldFillColor")]
    public string InvincibleShieldFillColorHex
    {
        get => string.Empty;
        set { _invincibleShieldFillColor = value.ToColor(); }
    }

    /// <summary>無敵シールド内部塗りつぶし色 <see cref="Color" /></summary>
    public Color InvincibleShieldFillColor => _invincibleShieldFillColor;

    private Color _invincibleShieldRingColor;

    /// <summary>無敵シールド外枠色 Hex 文字列</summary>
    [ConfigurationKeyName("InvincibleShieldRingColor")]
    public string InvincibleShieldRingColorHex
    {
        get => string.Empty;
        set { _invincibleShieldRingColor = value.ToColor(); }
    }

    /// <summary>無敵シールド外枠色 <see cref="Color" /></summary>
    public Color InvincibleShieldRingColor => _invincibleShieldRingColor;

    private Color _invincibleRippleColor;

    /// <summary>無敵衝撃波拡散色 Hex 文字列</summary>
    [ConfigurationKeyName("InvincibleRippleColor")]
    public string InvincibleRippleColorHex
    {
        get => string.Empty;
        set { _invincibleRippleColor = value.ToColor(); }
    }

    /// <summary>無敵衝撃波拡散色 <see cref="Color" /></summary>
    public Color InvincibleRippleColor => _invincibleRippleColor;

    private Color _invincibleRippleCoreColor;

    /// <summary>無敵衝撃波コア色 Hex 文字列</summary>
    [ConfigurationKeyName("InvincibleRippleCoreColor")]
    public string InvincibleRippleCoreColorHex
    {
        get => string.Empty;
        set { _invincibleRippleCoreColor = value.ToColor(); }
    }

    /// <summary>無敵衝撃波コア色 <see cref="Color" /></summary>
    public Color InvincibleRippleCoreColor => _invincibleRippleCoreColor;

    private Color _invinciblePrismColor;

    /// <summary>無敵結界光線色 Hex 文字列</summary>
    [ConfigurationKeyName("InvinciblePrismColor")]
    public string InvinciblePrismColorHex
    {
        get => string.Empty;
        set { _invinciblePrismColor = value.ToColor(); }
    }

    /// <summary>無敵結界光線色 <see cref="Color" /></summary>
    public Color InvinciblePrismColor => _invinciblePrismColor;

    private Color _invincibleNodeOuterColor;

    /// <summary>無敵防護ノード外周色 Hex 文字列</summary>
    [ConfigurationKeyName("InvincibleNodeOuterColor")]
    public string InvincibleNodeOuterColorHex
    {
        get => string.Empty;
        set { _invincibleNodeOuterColor = value.ToColor(); }
    }

    /// <summary>無敵防護ノード外周色 <see cref="Color" /></summary>
    public Color InvincibleNodeOuterColor => _invincibleNodeOuterColor;

    private Color _invincibleNodeInnerColor;

    /// <summary>無敵防護ノード中心色 Hex 文字列</summary>
    [ConfigurationKeyName("InvincibleNodeInnerColor")]
    public string InvincibleNodeInnerColorHex
    {
        get => string.Empty;
        set { _invincibleNodeInnerColor = value.ToColor(); }
    }

    /// <summary>無敵防護ノード中心色 <see cref="Color" /></summary>
    public Color InvincibleNodeInnerColor => _invincibleNodeInnerColor;

    private Color _invincibleCrossColor;

    /// <summary>無敵十字星芒色 Hex 文字列</summary>
    [ConfigurationKeyName("InvincibleCrossColor")]
    public string InvincibleCrossColorHex
    {
        get => string.Empty;
        set { _invincibleCrossColor = value.ToColor(); }
    }

    /// <summary>無敵十字星芒色 <see cref="Color" /></summary>
    public Color InvincibleCrossColor => _invincibleCrossColor;

    private Color _chargeFillColor;

    /// <summary>予兆チャージ充填・閃光色 Hex 文字列</summary>
    [ConfigurationKeyName("ChargeFillColor")]
    public string ChargeFillColorHex
    {
        get => string.Empty;
        set { _chargeFillColor = value.ToColor(); }
    }

    /// <summary>予兆チャージ充填・閃光色 <see cref="Color" /></summary>
    public Color ChargeFillColor => _chargeFillColor;

    /// <summary>覇体（SuperArmor）時の外層オーラ発光色 Hex 文字列</summary>
    private Color _superArmorAuraColor;

    [ConfigurationKeyName("SuperArmorAuraColor")]
    public string SuperArmorAuraColorHex
    {
        get => string.Empty;
        set { _superArmorAuraColor = value.ToColor(); }
    }

    /// <summary>覇体時の外層オーラ発光色 <see cref="Color" /></summary>
    public Color SuperArmorAuraColor => _superArmorAuraColor;

    /// <summary>覇体時の内層コア発光色 Hex 文字列</summary>
    private Color _superArmorCoreColor;

    [ConfigurationKeyName("SuperArmorCoreColor")]
    public string SuperArmorCoreColorHex
    {
        get => string.Empty;
        set { _superArmorCoreColor = value.ToColor(); }
    }

    /// <summary>覇体時の内層コア発光色 <see cref="Color" /></summary>
    public Color SuperArmorCoreColor => _superArmorCoreColor;

    /// <summary>覇体時の外層オーラ拡大量比率</summary>
    public float SuperArmorAuraScale { get; set; } = 1.06f;

    /// <summary>覇体時の内層コア拡大量比率</summary>
    public float SuperArmorCoreScale { get; set; } = 1.02f;

    /// <summary>覇体時のオーラ脈動角速度</summary>
    public float SuperArmorPulseSpeed { get; set; } = 12.0f;

    /// <summary>覇体時のオーラ基礎アルファ値 0から255の範囲</summary>
    public int SuperArmorAuraBaseAlpha { get; set; } = 120;

    /// <summary>覇体時のオーラ脈動アルファ振幅</summary>
    public int SuperArmorAuraPulseAlpha { get; set; } = 80;

    /// <summary>覇体時の防護ブラケット線の太さ ピクセル単位</summary>
    public int SuperArmorBracketThickness { get; set; } = 2;

    /// <summary>覇体時の防護ブラケット発光色 Hex 文字列</summary>
    private Color _superArmorBracketColor;

    [ConfigurationKeyName("SuperArmorBracketColor")]
    public string SuperArmorBracketColorHex
    {
        get => string.Empty;
        set { _superArmorBracketColor = value.ToColor(); }
    }

    /// <summary>覇体時の防護ブラケット発光色 <see cref="Color" /></summary>
    public Color SuperArmorBracketColor => _superArmorBracketColor;

    /// <summary>覇体時の頭上状態表示テキスト</summary>
    public string SuperArmorMarkerText { get; set; } = "▼ SUPER ARMOR ▼";

    /// <summary>覇体時の頭上状態表示 Y 軸オフセット ピクセル単位</summary>
    public float SuperArmorMarkerOffsetY { get; set; } = 75.0f;

    private Color _superArmorMarkerColor;

    [ConfigurationKeyName("SuperArmorMarkerColor")]
    public string SuperArmorMarkerColorHex
    {
        get => string.Empty;
        set { _superArmorMarkerColor = value.ToColor(); }
    }

    /// <summary>覇体時の頭上状態表示色 <see cref="Color" /></summary>
    public Color SuperArmorMarkerColor => _superArmorMarkerColor;

    /// <summary>覇体マーカー浮動揺れ角速度</summary>
    public float SuperArmorMarkerBobbingSpeed { get; set; } = 8.0f;

    /// <summary>覇体マーカー浮動揺れ振幅 ピクセル単位</summary>
    public float SuperArmorMarkerBobbingAmplitude { get; set; } = 4.0f;
}

/// <summary>
///     画面下部 Boss HP バーおよび状態表示の設定クラス。
/// </summary>
public sealed class BossHpBarRenderConfig
{
    /// <summary>HP バー左上座標</summary>
    public Vec2i BarPos1 { get; set; }

    /// <summary>HP バー右下座標</summary>
    public Vec2i BarPos2 { get; set; }

    private Color _phase1Color;

    /// <summary>Phase 1 HP ゲージ色 Hex 文字列</summary>
    [ConfigurationKeyName("Phase1Color")]
    public string Phase1ColorHex
    {
        get => string.Empty;
        set { _phase1Color = value.ToColor(); }
    }

    /// <summary>Phase 1 HP ゲージ色 <see cref="Color" /></summary>
    public Color Phase1Color => _phase1Color;

    private Color _phase2Color;

    /// <summary>Phase 2 HP ゲージ色 Hex 文字列</summary>
    [ConfigurationKeyName("Phase2Color")]
    public string Phase2ColorHex
    {
        get => string.Empty;
        set { _phase2Color = value.ToColor(); }
    }

    /// <summary>Phase 2 HP ゲージ色 <see cref="Color" /></summary>
    public Color Phase2Color => _phase2Color;

    private Color _phase3Color;

    /// <summary>Phase 3 HP ゲージ色 Hex 文字列</summary>
    [ConfigurationKeyName("Phase3Color")]
    public string Phase3ColorHex
    {
        get => string.Empty;
        set { _phase3Color = value.ToColor(); }
    }

    /// <summary>Phase 3 HP ゲージ色 <see cref="Color" /></summary>
    public Color Phase3Color => _phase3Color;

    /// <summary>HP バー背景外枠パディング</summary>
    public int BgPadding { get; set; }

    private Color _bgColor;

    /// <summary>HP バー暗色背景色 Hex 文字列</summary>
    [ConfigurationKeyName("BgColor")]
    public string BgColorHex
    {
        get => string.Empty;
        set { _bgColor = value.ToColor(); }
    }

    /// <summary>HP バー暗色背景色 <see cref="Color" /></summary>
    public Color BgColor => _bgColor;

    private Color _borderColor;

    /// <summary>HP バー外枠線色 Hex 文字列</summary>
    [ConfigurationKeyName("BorderColor")]
    public string BorderColorHex
    {
        get => string.Empty;
        set { _borderColor = value.ToColor(); }
    }

    /// <summary>HP バー外枠線色 <see cref="Color" /></summary>
    public Color BorderColor => _borderColor;

    /// <summary>HP バー上面ハイライト厚み</summary>
    public int TopHighlightThickness { get; set; }

    private Color _topHighlightColor;

    /// <summary>HP バー上面ハイライト色 Hex 文字列</summary>
    [ConfigurationKeyName("TopHighlightColor")]
    public string TopHighlightColorHex
    {
        get => string.Empty;
        set { _topHighlightColor = value.ToColor(); }
    }

    /// <summary>HP バー上面ハイライト色 <see cref="Color" /></summary>
    public Color TopHighlightColor => _topHighlightColor;

    /// <summary>ヘッダー文字の Y 座標オフセット</summary>
    public int HeaderOffsetY { get; set; }

    /// <summary>Boss タイトル表示オフセット BarPos1からの相対距離</summary>
    public Vec2i TitleOffset { get; set; }

    private Color _titleColor;

    /// <summary>Boss タイトル文字色 Hex 文字列</summary>
    [ConfigurationKeyName("TitleColor")]
    public string TitleColorHex
    {
        get => string.Empty;
        set { _titleColor = value.ToColor(); }
    }

    /// <summary>Boss タイトル文字色 <see cref="Color" /></summary>
    public Color TitleColor => _titleColor;

    /// <summary>HP 数値テキストの Y 軸オフセット BarPos1.Yから上方向へのオフセット。未指定時はHeaderOffsetYを使用</summary>
    public int HpTextOffsetY { get; set; }

    private Color _hpTextColor;

    /// <summary>HP 数値文字色 Hex 文字列</summary>
    [ConfigurationKeyName("HpTextColor")]
    public string HpTextColorHex
    {
        get => string.Empty;
        set { _hpTextColor = value.ToColor(); }
    }

    /// <summary>HP 数値文字色 <see cref="Color" /></summary>
    public Color HpTextColor => _hpTextColor;

    /// <summary>状態バッジテキストのオフセット BarPos1からの相対距離</summary>
    public Vec2i StateBadgeOffset { get; set; }

    private Color _stateTelegraphingColor;

    /// <summary>前隙予兆状態バッジ文字色 Hex 文字列</summary>
    [ConfigurationKeyName("StateTelegraphingColor")]
    public string StateTelegraphingColorHex
    {
        get => string.Empty;
        set { _stateTelegraphingColor = value.ToColor(); }
    }

    /// <summary>前隙予兆状態バッジ文字色 <see cref="Color" /></summary>
    public Color StateTelegraphingColor => _stateTelegraphingColor;

    private Color _stateCastingColor;

    /// <summary>発動中状態バッジ文字色 Hex 文字列</summary>
    [ConfigurationKeyName("StateCastingColor")]
    public string StateCastingColorHex
    {
        get => string.Empty;
        set { _stateCastingColor = value.ToColor(); }
    }

    /// <summary>発動中状態バッジ文字色 <see cref="Color" /></summary>
    public Color StateCastingColor => _stateCastingColor;

    private Color _stateRecoveringColor;

    /// <summary>隙あり虚弱状態バッジ文字色 Hex 文字列</summary>
    [ConfigurationKeyName("StateRecoveringColor")]
    public string StateRecoveringColorHex
    {
        get => string.Empty;
        set { _stateRecoveringColor = value.ToColor(); }
    }

    /// <summary>隙あり虚弱状態バッジ文字色 <see cref="Color" /></summary>
    public Color StateRecoveringColor => _stateRecoveringColor;

    private Color _statePhaseTransitionColor;

    /// <summary>フェーズ遷移演出状態バッジ文字色 Hex 文字列</summary>
    [ConfigurationKeyName("StatePhaseTransitionColor")]
    public string StatePhaseTransitionColorHex
    {
        get => string.Empty;
        set { _statePhaseTransitionColor = value.ToColor(); }
    }

    /// <summary>フェーズ遷移演出状態バッジ文字色 <see cref="Color" /></summary>
    public Color StatePhaseTransitionColor => _statePhaseTransitionColor;

    private Color _stateDefaultColor;

    /// <summary>通常巡航状態バッジ文字色 Hex 文字列</summary>
    [ConfigurationKeyName("StateDefaultColor")]
    public string StateDefaultColorHex
    {
        get => string.Empty;
        set { _stateDefaultColor = value.ToColor(); }
    }

    /// <summary>通常巡航状態バッジ文字色 <see cref="Color" /></summary>
    public Color StateDefaultColor => _stateDefaultColor;

    private Color _stateStunnedColor;

    /// <summary>気絶・隙あり状態バッジ文字色 Hex 文字列</summary>
    [ConfigurationKeyName("StateStunnedColor")]
    public string StateStunnedColorHex
    {
        get => string.Empty;
        set { _stateStunnedColor = value.ToColor(); }
    }

    /// <summary>気絶・隙あり状態バッジ文字色 <see cref="Color" /></summary>
    public Color StateStunnedColor => _stateStunnedColor;

    /// <summary>Phase 分割マーカー半幅 ピクセル単位</summary>
    public int PhaseSplitWidth { get; set; }

    private Color _stateDefeatedColor;

    /// <summary>撃破状態バッジ文字色 Hex 文字列</summary>
    [ConfigurationKeyName("StateDefeatedColor")]
    public string StateDefeatedColorHex
    {
        get => string.Empty;
        set { _stateDefeatedColor = value.ToColor(); }
    }

    /// <summary>撃破状態バッジ文字色 <see cref="Color" /></summary>
    public Color StateDefeatedColor => _stateDefeatedColor;
}
