using System.Drawing;
using Microsoft.Extensions.Configuration;

namespace AnimalBattle.Skills;

/// <summary>
/// 攻撃予兆演出の幾何・描画種別。
/// </summary>
public enum TelegraphKind
{
    None,
    AimLaser,
    RadialSpikes,
    SkyfallColumns,
    DiveTrajectory,
    MeleeDangerBox,
    ChargeTrack,
    GroundShockwave,
    TargetLock
}

/// <summary>
/// 技能の予兆演出パラメータ設定クラス。
/// </summary>
public sealed class TelegraphConfig
{
    private Color _color = Color.Empty;

    /// <summary>予兆描画の種別</summary>
    public TelegraphKind Kind { get; set; } = TelegraphKind.None;

    /// <summary>予兆カラー（Hex 文字列）</summary>
    [ConfigurationKeyName("Color")]
    public string ColorHex
    {
        get => string.Empty;
        set => _color = value.ToColor();
    }

    /// <summary>パース済み予兆カラー</summary>
    public Color Color => _color;

    /// <summary>照射線数・スパイク数・落石柱数</summary>
    public int Count { get; set; }

    /// <summary>扇状照射角度（ラジアン）</summary>
    public float SpreadAngle { get; set; }

    /// <summary>危険円・スパイク描画半径（px）</summary>
    public float Radius { get; set; }

    /// <summary>回転角速度（ラジアン/秒）</summary>
    public float SpinSpeed { get; set; }

    /// <summary>危険矩形サイズ（px）</summary>
    public Vec2f Size { get; set; }

    /// <summary>天降光柱の生成最小 X 座標</summary>
    public float MinX { get; set; }

    /// <summary>天降光柱の生成最大 X 座標</summary>
    public float MaxX { get; set; }

    /// <summary>光柱または突進軌道の高さ（px）</summary>
    public float Height { get; set; }

    /// <summary>光柱上に表示する警告テキスト</summary>
    public string WarningText { get; set; } = string.Empty;

    /// <summary>スナイパー照準線モードフラグ</summary>
    public bool IsSniper { get; set; }
}
