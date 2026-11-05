using System.Drawing;
using Microsoft.Extensions.Configuration;

namespace AnimalBattle.Presentation.Rendering;

/// <summary>
///     Config/player.render.toml から読み込まれるプレイヤー描画設定ルートクラス。
/// </summary>
public sealed class PlayerRenderConfig
{
    /// <summary>自機基本視覚効果設定（無敵点滅、残像等）</summary>
    public PlayerVisualRenderConfig Player { get; set; } = new();

    /// <summary>自機 HP 肉球バッジおよび頭上インジケータ描画設定</summary>
    public PlayerHpBadgeRenderConfig PlayerHpBadge { get; set; } = new();

    /// <summary>近接斬撃エフェクトスプライト描画設定</summary>
    public SlashEffectRenderConfig SlashEffect { get; set; } = new();
}

/// <summary>
///     自機プレイヤー基本視覚パラメータ設定クラス。
/// </summary>
public sealed class PlayerVisualRenderConfig
{
    /// <summary>無敵時スプライト点滅周波数</summary>
    public float InvincibleBlinkFrequency { get; set; }

    private Color _ghostTrailColor;

    /// <summary>ダッシュ残像半透明色 Hex 文字列</summary>
    [ConfigurationKeyName("GhostTrailColor")]
    public string GhostTrailColorHex
    {
        get => string.Empty;
        set { _ghostTrailColor = value.ToColor(); }
    }

    /// <summary>ダッシュ残像半透明色 <see cref="Color" /></summary>
    public Color GhostTrailColor => _ghostTrailColor;
}

/// <summary>
///     自機 HP 肉球バッジおよび頭上インジケータの描画設定クラス。
/// </summary>
public sealed class PlayerHpBadgeRenderConfig
{
    /// <summary>HUD 基準原点座標</summary>
    public Vec2i Origin { get; set; }

    /// <summary>バッジ間の横方向間隔</summary>
    public float BadgeSpacing { get; set; }

    /// <summary>バッジ外枠半径</summary>
    public float BadgeRadius { get; set; }

    /// <summary>バッジ内における肉球全体（掌球＋指球）の垂直オフセット</summary>
    public float PawOffsetY { get; set; }
    
    /// <summary>掌球（主肉球）の半径</summary>
    public float PalmRadius { get; set; }

    /// <summary>掌球の垂直オフセット</summary>
    public float PalmOffsetY { get; set; }

    /// <summary>指球（爪先肉球）の半径</summary>
    public float ToeRadius { get; set; }

    /// <summary>中心から指球までの距離</summary>
    public float ToeDistance { get; set; }

    /// <summary>指球の角度リスト（度数法）</summary>
    public List<float> ToeAnglesDeg { get; set; } = [];

    private Color _catFillColor;
    /// <summary>猫自機の肉球塗りつぶし色 Hex 文字列</summary>
    [ConfigurationKeyName("CatFillColor")]
    public string CatFillColorHex
    {
        get => string.Empty;
        set => _catFillColor = value.ToColor();
    }
    /// <summary>猫自機の肉球塗りつぶし色 <see cref="Color" /></summary>
    public Color CatFillColor => _catFillColor;

    private Color _catGlowColor;
    /// <summary>猫自機の外枠・発光色 Hex 文字列</summary>
    [ConfigurationKeyName("CatGlowColor")]
    public string CatGlowColorHex
    {
        get => string.Empty;
        set => _catGlowColor = value.ToColor();
    }
    /// <summary>猫自機の外枠・発光色 <see cref="Color" /></summary>
    public Color CatGlowColor => _catGlowColor;

    private Color _bunnyFillColor;
    /// <summary>兎自機の肉球塗りつぶし色 Hex 文字列</summary>
    [ConfigurationKeyName("BunnyFillColor")]
    public string BunnyFillColorHex
    {
        get => string.Empty;
        set => _bunnyFillColor = value.ToColor();
    }
    /// <summary>兎自機の肉球塗りつぶし色 <see cref="Color" /></summary>
    public Color BunnyFillColor => _bunnyFillColor;

    private Color _bunnyGlowColor;
    /// <summary>兎自機の外枠・発光色 Hex 文字列</summary>
    [ConfigurationKeyName("BunnyGlowColor")]
    public string BunnyGlowColorHex
    {
        get => string.Empty;
        set => _bunnyGlowColor = value.ToColor();
    }
    /// <summary>兎自機の外枠・発光色 <see cref="Color" /></summary>
    public Color BunnyGlowColor => _bunnyGlowColor;

    private Color _emptyBorderColor;
    /// <summary>破損・空スロット枠色 Hex 文字列</summary>
    [ConfigurationKeyName("EmptyBorderColor")]
    public string EmptyBorderColorHex
    {
        get => string.Empty;
        set => _emptyBorderColor = value.ToColor();
    }
    /// <summary>破損・空スロット枠色 <see cref="Color" /></summary>
    public Color EmptyBorderColor => _emptyBorderColor;

    private Color _emptyFillColor;
    /// <summary>破損・空スロット背景色 Hex 文字列</summary>
    [ConfigurationKeyName("EmptyFillColor")]
    public string EmptyFillColorHex
    {
        get => string.Empty;
        set => _emptyFillColor = value.ToColor();
    }
    /// <summary>破損・空スロット背景色 <see cref="Color" /></summary>
    public Color EmptyFillColor => _emptyFillColor;

    private Color _damagedFlashColor;
    /// <summary>被弾フラッシュ色 Hex 文字列</summary>
    [ConfigurationKeyName("DamagedFlashColor")]
    public string DamagedFlashColorHex
    {
        get => string.Empty;
        set => _damagedFlashColor = value.ToColor();
    }
    /// <summary>被弾フラッシュ色 <see cref="Color" /></summary>
    public Color DamagedFlashColor => _damagedFlashColor;

    /// <summary>被弾振動持続時間（秒）</summary>
    public float ShakeDuration { get; set; }

    /// <summary>被弾振動振幅（ピクセル）</summary>
    public float ShakeAmplitude { get; set; }

    /// <summary>被弾フラッシュ持続時間（秒）</summary>
    public float FlashDuration { get; set; }

    /// <summary>瀕死時脈動速度</summary>
    public float LowHpPulseSpeed { get; set; }

    /// <summary>頭上インジケータ有効フラグ</summary>
    public bool OverheadEnabled { get; set; }

    /// <summary>自機原点からの頭上オフセット Y</summary>
    public float OverheadOffsetY { get; set; }

    /// <summary>頭上マイクロインジケータ点半径</summary>
    public float OverheadPipRadius { get; set; }

    /// <summary>頭上マイクロインジケータ点間隔</summary>
    public float OverheadPipSpacing { get; set; }

    /// <summary>頭上インジケータ覚醒後フェード時間（秒）</summary>
    public float OverheadFadeTime { get; set; }

    /// <summary>頭上インジケータ覚醒時最大不透明度 (0-255)</summary>
    public int OverheadActiveAlpha { get; set; }
}

/// <summary>
///     近接斬撃エフェクトスプライト描画設定クラス。
/// </summary>
public sealed class SlashEffectRenderConfig
{
    public int SpriteColIndex { get; set; }
    public int SpriteCellSize { get; set; }
    public float BossBaseReferenceSize { get; set; }
    public float PlayerBaseReferenceSize { get; set; }
    public float CenterBiasOffset { get; set; }
    private Color _playerSlashColor;

    /// <summary>自機近接斬撃エフェクト色 Hex 文字列</summary>
    [ConfigurationKeyName("PlayerSlashColor")]
    public string PlayerSlashColorHex
    {
        get => string.Empty;
        set { _playerSlashColor = value.ToColor(); }
    }

    /// <summary>自機近接斬撃エフェクト色 <see cref="Color" /></summary>
    public Color PlayerSlashColor => _playerSlashColor;
}
