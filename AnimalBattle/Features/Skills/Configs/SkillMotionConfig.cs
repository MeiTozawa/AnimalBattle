using System.Drawing;
using Microsoft.Extensions.Configuration;

namespace AnimalBattle.Skills;

/// <summary>
/// 技能実行中の施術者運動モード。
/// </summary>
public enum SkillMotionKind
{
    Default,
    EaseToTarget,
    LeapSmash
}

/// <summary>
/// 技能実行中の固有移動軌道設定クラス。
/// </summary>
public sealed class SkillMotionConfig
{
    private Color _trailColor = Color.Empty;

    /// <summary>移動挙動の種別</summary>
    public SkillMotionKind Kind { get; set; } = SkillMotionKind.Default;

    /// <summary>突進・補間に使用するイージング関数</summary>
    public EaseType EaseType { get; set; } = EaseType.InQuad;

    /// <summary>残影トレイルカラー（Hex 文字列）</summary>
    [ConfigurationKeyName("TrailColor")]
    public string TrailColorHex
    {
        get => string.Empty;
        set => _trailColor = value.ToColor();
    }

    /// <summary>パース済み残影トレイルカラー</summary>
    public Color TrailColor => _trailColor;

    /// <summary>目標上空への上昇所要時間（秒）</summary>
    public float FlyDuration { get; set; }

    /// <summary>最高到達点の滞空 Y 高度（px）</summary>
    public float PeakAltitudeY { get; set; }

    /// <summary>最高到達点での滞空時間（秒）</summary>
    public float ApexHangDuration { get; set; }

    /// <summary>垂直急降下叩きつけ速度（px/s）</summary>
    public float DescentSpeed { get; set; }
}
