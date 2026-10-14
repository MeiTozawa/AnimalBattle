using System.Drawing;

namespace AnimalBattle.Configs;

/// <summary>
///     近接・斬撃系技能パターン設定の共通基底クラス。
///     突進速度、斬撃判定範囲、威力および持続時間を保持します。
/// </summary>
public abstract class MeleePatternConfigBase : SkillPatternConfigBase {
    /// <summary>前進・突進速度</summary>
    public float DashSpeed { get; set; }

    /// <summary>斬撃判定 X 半径</summary>
    public float SlashRangeX { get; set; }

    /// <summary>斬撃判定 Y 半径</summary>
    public float SlashRangeY { get; set; }

    /// <summary>斬撃円形半径</summary>
    public float SlashRadius { get; set; }

    /// <summary>斬撃命中ダメージ</summary>
    public float SlashDamage { get; set; }

    /// <summary>攻撃判定の残留持続時間（秒）</summary>
    public float HitboxDuration { get; set; }

    /// <summary>斬撃エフェクトカラー</summary>
    public Color SlashColor { get; set; }
}
