using System.Drawing;
using AnimalBattle.Core;
using Microsoft.Extensions.Configuration;

namespace AnimalBattle.Configs;

/// <summary>
///     近接・斬撃系技能パターン設定の共通基底クラス。
///     突進速度、斬撃判定範囲、威力および持続時間を保持します。
/// </summary>
public abstract class MeleePatternConfigBase : SkillPatternConfigBase {
    private string _slashColorHex = string.Empty;
    private Color _slashColor;

    /// <summary>前進・突進速度</summary>
    public float DashSpeed { get; set; }

    /// <summary>斬撃判定範囲 Vec2f型</summary>
    public Vec2f SlashRange { get; set; }

    /// <summary>斬撃円形半径</summary>
    public float SlashRadius { get; set; }

    /// <summary>斬撃命中ダメージ</summary>
    public float SlashDamage { get; set; }

    /// <summary>攻撃判定の残留持続時間秒単位</summary>
    public float HitboxDuration { get; set; }

    /// <summary>斬撃エフェクトカラー Hex 文字列</summary>
    [ConfigurationKeyName("SlashColor")]
    public string SlashColorHex
    {
        get => _slashColorHex;
        set
        {
            _slashColorHex = value;
            _slashColor = value.ToColor();
        }
    }

    /// <summary>斬撃エフェクトカラー</summary>
    public Color SlashColor => _slashColor;
}
