using System.Drawing;
using Microsoft.Extensions.Configuration;

namespace AnimalBattle.Skills;

/// <summary>
/// 技能効果の発火契機種別。
/// </summary>
public enum SkillTriggerKind
{
    OnTime,
    OnGrounded
}

/// <summary>
/// 技能効果の実行種別。
/// </summary>
public enum SkillEffectKind
{
    None,
    Bullet,
    Hitbox,
    Velocity,
    ScreenShake,
    Summon
}

/// <summary>
/// 弾幕生成パターンの幾何トポロジー種別。
/// </summary>
public enum BulletPatternKind
{
    None,
    AimedSpread,
    RadialRing,
    DoubleSpiral,
    CrossWave,
    Skyfall,
    ImpactRing,
    GroundShockwave,
    ShrapnelFan
}

/// <summary>
/// 近接攻撃判定の展開方向モード。
/// </summary>
public enum HitboxDirectionMode
{
    Forward,
    DownOnGround
}

/// <summary>
/// 弾幕発射パラメータ設定クラス。
/// </summary>
public sealed class BulletEmitConfig
{
    private Color _fillColor = Color.Empty;
    private Color _outlineColor = Color.Empty;
    private Color _fillColor2 = Color.Empty;
    private Color _outlineColor2 = Color.Empty;

    /// <summary>弾幕パターンのトポロジー</summary>
    public BulletPatternKind Pattern { get; set; } = BulletPatternKind.None;

    /// <summary>発射弾数</summary>
    public int Count { get; set; } = 1;

    /// <summary>初速（px/s）</summary>
    public float Speed { get; set; }

    /// <summary>弾の判定半径（px）</summary>
    public float Radius { get; set; } = 8.0f;

    /// <summary>扇状拡散角度（ラジアン）</summary>
    public float SpreadAngle { get; set; }

    /// <summary>弾間またはフレームごとの回転角度差（ラジアン）</summary>
    public float AngleStep { get; set; }

    /// <summary>破片飛散開始角度比率（PI に対する割合）</summary>
    public float AngleStartRatio { get; set; }

    /// <summary>破片飛散扇状角度幅比率（PI に対する割合）</summary>
    public float AngleSpanRatio { get; set; }

    /// <summary>ランダム天降弾の最小 X 座標</summary>
    public float MinX { get; set; }

    /// <summary>ランダム天降弾の最大 X 座標</summary>
    public float MaxX { get; set; }

    /// <summary>生成位置の水平オフセット（px）</summary>
    public float OffsetX { get; set; }

    /// <summary>生成位置の垂直オフセット（px）</summary>
    public float OffsetY { get; set; }

    /// <summary>地面接触面の垂直オフセット（px）</summary>
    public float GroundOffsetY { get; set; }

    /// <summary>弾ダメージ</summary>
    public float Damage { get; set; } = 1.0f;

    /// <summary>特殊弾種別（Normal, Cyan, Magenta, Yellow, Black）</summary>
    public string BulletType { get; set; } = string.Empty;

    /// <summary>弾の塗りつぶしカラー（Hex 文字列）</summary>
    [ConfigurationKeyName("FillColor")]
    public string FillColorHex
    {
        get => string.Empty;
        set => _fillColor = value.ToColor();
    }

    /// <summary>パース済み弾塗りつぶしカラー</summary>
    public Color FillColor => _fillColor;

    /// <summary>弾の外枠カラー（Hex 文字列）</summary>
    [ConfigurationKeyName("OutlineColor")]
    public string OutlineColorHex
    {
        get => string.Empty;
        set => _outlineColor = value.ToColor();
    }

    /// <summary>パース済み弾外枠カラー</summary>
    public Color OutlineColor => _outlineColor;

    /// <summary>第 2 弾塗りつぶしカラー（双螺旋等、Hex 文字列）</summary>
    [ConfigurationKeyName("FillColor2")]
    public string FillColor2Hex
    {
        get => string.Empty;
        set => _fillColor2 = value.ToColor();
    }

    /// <summary>パース済み第 2 弾塗りつぶしカラー</summary>
    public Color FillColor2 => _fillColor2;

    /// <summary>第 2 弾外枠カラー（Hex 文字列）</summary>
    [ConfigurationKeyName("OutlineColor2")]
    public string OutlineColor2Hex
    {
        get => string.Empty;
        set => _outlineColor2 = value.ToColor();
    }

    /// <summary>パース済み第 2 弾外枠カラー</summary>
    public Color OutlineColor2 => _outlineColor2;

    /// <summary>近接攻撃で破壊不能かどうかのフラグ</summary>
    public bool IsIndestructible { get; set; }
}

/// <summary>
/// 近接攻撃判定生成パラメータ設定クラス。
/// </summary>
public sealed class HitboxEmitConfig
{
    private Color _color = Color.Empty;

    /// <summary>判定の展開方向モード</summary>
    public HitboxDirectionMode DirectionMode { get; set; } = HitboxDirectionMode.Forward;

    /// <summary>攻撃判定矩形サイズ（px）</summary>
    public Vec2f Size { get; set; }

    /// <summary>発生座標オフセット（px）</summary>
    public Vec2f Offset { get; set; }

    /// <summary>与ダメージ</summary>
    public float Damage { get; set; } = 1.0f;

    /// <summary>判定の残留持続時間（秒）</summary>
    public float Duration { get; set; } = 0.25f;

    /// <summary>近接判定エフェクトカラー（Hex 文字列）</summary>
    [ConfigurationKeyName("Color")]
    public string ColorHex
    {
        get => string.Empty;
        set => _color = value.ToColor();
    }

    /// <summary>パース済み判定エフェクトカラー</summary>
    public Color Color => _color;
}

public sealed class SummonEmitConfig
{
    /// <summary>発生座標オフセット（px）</summary>
    public Vec2f SpawnPosition { get; set; }

    /// <summary>与ダメージ</summary>
    public Entity Character { get; set; }


}


/// <summary>
/// タイムラインで実行される単一アクションステップ設定クラス。
/// </summary>
public sealed class SkillEffectConfig
{
    /// <summary>Active 段階開始からの発火時刻（秒）</summary>
    public float Time { get; set; }

    /// <summary>発火契機種別（時間または着地）</summary>
    public SkillTriggerKind Trigger { get; set; } = SkillTriggerKind.OnTime;

    /// <summary>周期実行間隔（秒）。0 超で CastDuration まで繰り返し発火</summary>
    public float Interval { get; set; }

    /// <summary>周期実行の最大回数（0 の場合は CastDuration 終了まで継続）</summary>
    public int RepeatCount { get; set; }

    /// <summary>実行効果種別</summary>
    public SkillEffectKind Kind { get; set; } = SkillEffectKind.None;

    /// <summary>弾幕発射パラメータ（Kind == Bullet 時）</summary>
    public BulletEmitConfig? Bullet { get; set; }

    /// <summary>近接判定パラメータ（Kind == Hitbox 時）</summary>
    public HitboxEmitConfig? Hitbox { get; set; }

    /// <summary>瞬間移動速度（Kind == Velocity 時、px/s）</summary>
    public Vec2f Velocity { get; set; }

    /// <summary>画面振動強度（Kind == ScreenShake 時）</summary>
    public float ShakeIntensity { get; set; }

    /// <summary>画面振動持続時間（Kind == ScreenShake 時、秒）</summary>
    public float ShakeDuration { get; set; }
}
