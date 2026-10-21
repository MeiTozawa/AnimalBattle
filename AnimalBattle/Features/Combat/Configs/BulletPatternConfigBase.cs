using System.Drawing;
using AnimalBattle.Core;
using AnimalBattle.Events;

namespace AnimalBattle.Configs;

/// <summary>
///     弾幕・射撃系技能パターン設定の共通基底クラス。
///     弾速、弾数、弾種別などの射撃パラメータを保持します。
/// </summary>
public abstract class BulletPatternConfigBase : SkillPatternConfigBase {
    /// <summary>発射間隔（秒）</summary>
    public float Interval { get; set; }

    /// <summary>弾速</summary>
    public float BulletSpeed { get; set; }

    /// <summary>弾の半径</summary>
    public float BulletRadius { get; set; }

    /// <summary>弾数</summary>
    public int BulletCount { get; set; }

    /// <summary>弾の塗りつぶしカラー</summary>
    public Color FillColor { get; set; }

    /// <summary>弾の外枠カラー</summary>
    public Color OutlineColor { get; set; }

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
