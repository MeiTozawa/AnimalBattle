using System.Drawing;
using Microsoft.Extensions.Configuration;

namespace AnimalBattle.Presentation.Rendering;

/// <summary>
///     Config/combat.render.toml から読み込まれる戦闘・弾幕・出現バッファ描画設定ルートクラス。
/// </summary>
public sealed class CombatRenderConfig
{
    /// <summary>銃弾および弾幕描画設定</summary>
    public BulletRenderConfig Bullets { get; set; } = new();

    /// <summary>出現バッファ半透明呼吸描画設定</summary>
    public SpawnBufferRenderConfig SpawnBuffer { get; set; } = new();
}

/// <summary>
///     出現バッファ時の半透明呼吸点滅描画設定クラス。
/// </summary>
public sealed class SpawnBufferRenderConfig
{
    /// <summary>呼吸点滅の基準アルファ値（0〜255）</summary>
    public int BaseAlpha { get; set; } = 150;

    /// <summary>呼吸点滅の振幅アルファ値</summary>
    public int PulseAlpha { get; set; } = 75;

    /// <summary>呼吸周波数（ラジアン毎秒）</summary>
    public float Frequency { get; set; } = 12.0f;
}

/// <summary>
///     銃弾および弾幕の描画設定クラス。
/// </summary>
public sealed class BulletRenderConfig
{
    /// <summary>プレイヤー弾外層グロー半径</summary>
    public float PlayerBulletOuterRadius { get; set; }

    private Color _playerBulletOuterColor;

    /// <summary>プレイヤー弾外層グロー色 Hex 文字列</summary>
    [ConfigurationKeyName("PlayerBulletOuterColor")]
    public string PlayerBulletOuterColorHex
    {
        get => string.Empty;
        set { _playerBulletOuterColor = value.ToColor(); }
    }

    /// <summary>プレイヤー弾外層グロー色 <see cref="Color" /></summary>
    public Color PlayerBulletOuterColor => _playerBulletOuterColor;

    /// <summary>プレイヤー弾内層コア半径</summary>
    public float PlayerBulletInnerRadius { get; set; }

    private Color _playerBulletInnerColor;

    /// <summary>プレイヤー弾内層コア色 Hex 文字列</summary>
    [ConfigurationKeyName("PlayerBulletInnerColor")]
    public string PlayerBulletInnerColorHex
    {
        get => string.Empty;
        set { _playerBulletInnerColor = value.ToColor(); }
    }

    /// <summary>プレイヤー弾内層コア色 <see cref="Color" /></summary>
    public Color PlayerBulletInnerColor => _playerBulletInnerColor;

    /// <summary>プレイヤー弾ビームトレイル半サイズ ピクセル単位</summary>
    public Vec2i PlayerBulletTrailHalfSize { get; set; }

    private Color _playerBulletTrailColor;

    /// <summary>プレイヤー弾ビームトレイル色 Hex 文字列</summary>
    [ConfigurationKeyName("PlayerBulletTrailColor")]
    public string PlayerBulletTrailColorHex
    {
        get => string.Empty;
        set { _playerBulletTrailColor = value.ToColor(); }
    }

    /// <summary>プレイヤー弾ビームトレイル色 <see cref="Color" /></summary>
    public Color PlayerBulletTrailColor => _playerBulletTrailColor;

    /// <summary>プレイヤー弾トレイル第1階層のアルファ値 0から255の範囲</summary>
    public int PlayerBulletTrailAlpha1 { get; set; }

    /// <summary>プレイヤー弾トレイル第2階層のアルファ値 0から255の範囲</summary>
    public int PlayerBulletTrailAlpha2 { get; set; }

    /// <summary>プレイヤー弾トレイル第3階層のアルファ値 0から255の範囲</summary>
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

    /// <summary>エネミー弾内層ホワイトコア半径比率</summary>
    public float EnemyBulletCoreRadiusRatio { get; set; }
    public float BossBulletCoreRadiusRatio { get => EnemyBulletCoreRadiusRatio; set => EnemyBulletCoreRadiusRatio = value; }

    /// <summary>エネミー弾内層ホワイトコア最小ピクセル半径</summary>
    public int EnemyBulletCoreMinRadius { get; set; }
    public int BossBulletCoreMinRadius { get => EnemyBulletCoreMinRadius; set => EnemyBulletCoreMinRadius = value; }

    private Color _enemyBulletCoreColor;

    /// <summary>エネミー弾内層ホワイトコア色 Hex 文字列</summary>
    [ConfigurationKeyName("EnemyBulletCoreColor")]
    public string EnemyBulletCoreColorHex
    {
        get => string.Empty;
        set { _enemyBulletCoreColor = value.ToColor(); }
    }

    [ConfigurationKeyName("BossBulletCoreColor")]
    public string BossBulletCoreColorHex
    {
        get => string.Empty;
        set { if (_enemyBulletCoreColor == default) _enemyBulletCoreColor = value.ToColor(); }
    }

    /// <summary>エネミー弾内層ホワイトコア色 <see cref="Color" /></summary>
    public Color EnemyBulletCoreColor => _enemyBulletCoreColor;
    public Color BossBulletCoreColor => EnemyBulletCoreColor;

    /// <summary>エネミー弾外枠付加ピクセル半径</summary>
    public int EnemyBulletOutlinePadding { get; set; }
    public int BossBulletOutlinePadding { get => EnemyBulletOutlinePadding; set => EnemyBulletOutlinePadding = value; }

    /// <summary>画面外カリング：描画除外マージン ピクセル単位</summary>
    public Vec2i ViewportCullingMargin { get; set; }

    /// <summary>マゼンタ弾オーラ基礎半径比率</summary>
    public float SpecialMagentaAuraBaseRatio { get; set; }

    /// <summary>マゼンタ弾オーラ脈動比率</summary>
    public float SpecialMagentaAuraPulseRatio { get; set; }

    /// <summary>マゼンタ弾脈動角速度</summary>
    public float SpecialMagentaPulseSpeed { get; set; }

    /// <summary>マゼンタ弾オーラアルファ値 0から255の範囲</summary>
    public int SpecialMagentaAuraAlpha { get; set; }

    /// <summary>シアン弾外周リング拡張幅 ピクセル単位</summary>
    public int SpecialCyanRingPadding { get; set; }

    private Color _specialCyanRingColor;

    /// <summary>シアン弾外周リング発光色 Hex 文字列</summary>
    [ConfigurationKeyName("SpecialCyanRingColor")]
    public string SpecialCyanRingColorHex
    {
        get => string.Empty;
        set { _specialCyanRingColor = value.ToColor(); }
    }

    /// <summary>シアン弾外周リング発光色 <see cref="Color" /></summary>
    public Color SpecialCyanRingColor => _specialCyanRingColor;

    /// <summary>ブラック弾オーラ基礎半径比率</summary>
    public float SpecialBlackAuraBaseRatio { get; set; }

    /// <summary>ブラック弾オーラ脈動比率</summary>
    public float SpecialBlackAuraPulseRatio { get; set; }

    /// <summary>ブラック弾脈動角速度</summary>
    public float SpecialBlackPulseSpeed { get; set; }

    private Color _specialBlackAuraColor;

    /// <summary>ブラック弾オーラ色 Hex 文字列</summary>
    [ConfigurationKeyName("SpecialBlackAuraColor")]
    public string SpecialBlackAuraColorHex
    {
        get => string.Empty;
        set { _specialBlackAuraColor = value.ToColor(); }
    }

    /// <summary>ブラック弾オーラ色 <see cref="Color" /></summary>
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

    private Color _specialBlackCoreInnerColor;

    /// <summary>ブラック弾内層微小核色 Hex 文字列</summary>
    [ConfigurationKeyName("SpecialBlackCoreInnerColor")]
    public string SpecialBlackCoreInnerColorHex
    {
        get => string.Empty;
        set { _specialBlackCoreInnerColor = value.ToColor(); }
    }

    /// <summary>ブラック弾内層微小核色 <see cref="Color" /></summary>
    public Color SpecialBlackCoreInnerColor => _specialBlackCoreInnerColor;

    /// <summary>パーティクルエフェクト外枠ピクセル拡張幅</summary>
    public int ParticleOutlinePadding { get; set; }

    private Color _bulletNormalFillColor;

    /// <summary>通常弾塗りつぶし色 Hex 文字列</summary>
    [ConfigurationKeyName("BulletNormalFillColor")]
    public string BulletNormalFillColorHex
    {
        get => string.Empty;
        set { _bulletNormalFillColor = value.ToColor(); }
    }

    /// <summary>通常弾塗りつぶし色 <see cref="Color" /></summary>
    public Color BulletNormalFillColor => _bulletNormalFillColor;

    private Color _bulletNormalOutlineColor;

    /// <summary>通常弾外枠色 Hex 文字列</summary>
    [ConfigurationKeyName("BulletNormalOutlineColor")]
    public string BulletNormalOutlineColorHex
    {
        get => string.Empty;
        set { _bulletNormalOutlineColor = value.ToColor(); }
    }

    /// <summary>通常弾外枠色 <see cref="Color" /></summary>
    public Color BulletNormalOutlineColor => _bulletNormalOutlineColor;

    private Color _bulletMagentaFillColor;

    /// <summary>品紅弾塗りつぶし色 Hex 文字列</summary>
    [ConfigurationKeyName("BulletMagentaFillColor")]
    public string BulletMagentaFillColorHex
    {
        get => string.Empty;
        set { _bulletMagentaFillColor = value.ToColor(); }
    }

    /// <summary>品紅弾塗りつぶし色 <see cref="Color" /></summary>
    public Color BulletMagentaFillColor => _bulletMagentaFillColor;

    private Color _bulletMagentaOutlineColor;

    /// <summary>品紅弾外枠色 Hex 文字列</summary>
    [ConfigurationKeyName("BulletMagentaOutlineColor")]
    public string BulletMagentaOutlineColorHex
    {
        get => string.Empty;
        set { _bulletMagentaOutlineColor = value.ToColor(); }
    }

    /// <summary>品紅弾外枠色 <see cref="Color" /></summary>
    public Color BulletMagentaOutlineColor => _bulletMagentaOutlineColor;

    private Color _bulletYellowFillColor;

    /// <summary>黄弾塗りつぶし色 Hex 文字列</summary>
    [ConfigurationKeyName("BulletYellowFillColor")]
    public string BulletYellowFillColorHex
    {
        get => string.Empty;
        set { _bulletYellowFillColor = value.ToColor(); }
    }

    /// <summary>黄弾塗りつぶし色 <see cref="Color" /></summary>
    public Color BulletYellowFillColor => _bulletYellowFillColor;

    private Color _bulletYellowOutlineColor;

    /// <summary>黄弾外枠色 Hex 文字列</summary>
    [ConfigurationKeyName("BulletYellowOutlineColor")]
    public string BulletYellowOutlineColorHex
    {
        get => string.Empty;
        set { _bulletYellowOutlineColor = value.ToColor(); }
    }

    /// <summary>黄弾外枠色 <see cref="Color" /></summary>
    public Color BulletYellowOutlineColor => _bulletYellowOutlineColor;

    private Color _bulletCyanFillColor;

    /// <summary>青弾塗りつぶし色 Hex 文字列</summary>
    [ConfigurationKeyName("BulletCyanFillColor")]
    public string BulletCyanFillColorHex
    {
        get => string.Empty;
        set { _bulletCyanFillColor = value.ToColor(); }
    }

    /// <summary>青弾塗りつぶし色 <see cref="Color" /></summary>
    public Color BulletCyanFillColor => _bulletCyanFillColor;

    private Color _bulletCyanOutlineColor;

    /// <summary>青弾外枠色 Hex 文字列</summary>
    [ConfigurationKeyName("BulletCyanOutlineColor")]
    public string BulletCyanOutlineColorHex
    {
        get => string.Empty;
        set { _bulletCyanOutlineColor = value.ToColor(); }
    }

    /// <summary>青弾外枠色 <see cref="Color" /></summary>
    public Color BulletCyanOutlineColor => _bulletCyanOutlineColor;

    private Color _bulletBlackFillColor;

    /// <summary>黒弾塗りつぶし色 Hex 文字列</summary>
    [ConfigurationKeyName("BulletBlackFillColor")]
    public string BulletBlackFillColorHex
    {
        get => string.Empty;
        set { _bulletBlackFillColor = value.ToColor(); }
    }

    /// <summary>黒弾塗りつぶし色 <see cref="Color" /></summary>
    public Color BulletBlackFillColor => _bulletBlackFillColor;

    private Color _bulletBlackOutlineColor;

    /// <summary>黒弾外枠色 Hex 文字列</summary>
    [ConfigurationKeyName("BulletBlackOutlineColor")]
    public string BulletBlackOutlineColorHex
    {
        get => string.Empty;
        set { _bulletBlackOutlineColor = value.ToColor(); }
    }

    /// <summary>黒弾外枠色 <see cref="Color" /></summary>
    public Color BulletBlackOutlineColor => _bulletBlackOutlineColor;
}
