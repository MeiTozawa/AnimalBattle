using System.Drawing;
using System.Text.Json.Serialization;
using AnimalBattle.Core;
using AnimalBattle.Events;
using Microsoft.Extensions.Configuration;

namespace AnimalBattle.Configs;

/// <summary>
///     弾幕・射撃系技能パターン設定の共通基底クラス。
///     弾速、弾数、弾種別などの射撃パラメータを保持します。
/// </summary>
public abstract class BulletPatternConfigBase : SkillPatternConfigBase {
    private string _fillColorHex = string.Empty;
    private Color _fillColor;
    private string _outlineColorHex = string.Empty;
    private Color _outlineColor;

    /// <summary>発射間隔（秒）</summary>
    public float Interval { get; set; }

    /// <summary>弾速</summary>
    public float BulletSpeed { get; set; }

    /// <summary>弾の半径</summary>
    public float BulletRadius { get; set; }

    /// <summary>弾数</summary>
    public int BulletCount { get; set; }

    /// <summary>弾の塗りつぶしカラー Hex 文字列</summary>
    [ConfigurationKeyName("FillColor")]
    [JsonPropertyName("FillColor")]
    public string FillColorHex
    {
        get => _fillColorHex;
        set
        {
            _fillColorHex = value;
            _fillColor = value.ToColor();
        }
    }

    /// <summary>弾の塗りつぶしカラー</summary>
    [JsonIgnore]
    public Color FillColor => _fillColor;

    /// <summary>弾の外枠カラー Hex 文字列</summary>
    [ConfigurationKeyName("OutlineColor")]
    [JsonPropertyName("OutlineColor")]
    public string OutlineColorHex
    {
        get => _outlineColorHex;
        set
        {
            _outlineColorHex = value;
            _outlineColor = value.ToColor();
        }
    }

    /// <summary>弾の外枠カラー</summary>
    [JsonIgnore]
    public Color OutlineColor => _outlineColor;

    /// <summary>弾種別名（Magenta, Cyan, Yellow, Black, Normal）</summary>
    public string BulletType { get; set; } = string.Empty;

    /// <summary>
    ///     弾幕発射イベントを発行します。
    /// </summary>
    protected void PublishBulletFired()
    {
        var skillId = SkillId;
        if (!skillId.IsEmpty)
        {
            EventBus.Publish(new BossBulletFiredEvent(skillId));
        }
    }
}
