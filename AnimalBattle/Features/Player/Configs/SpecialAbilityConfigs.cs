using System.Drawing;
using Microsoft.Extensions.Configuration;

namespace AnimalBattle.Configs;

/// <summary>
///     全特殊能力（必殺技）設定の基底インターフェース。
///     誤爆防止インターバル時間を共通提供します。
/// </summary>
public interface ISpecialAbilityConfig
{
    /// <summary>特殊能力発動後の連続誤爆防止クールダウン（秒）</summary>
    float Cooldown { get; }

    /// <summary>対象エンティティへ対応する能力コンポーネントをアタッチします。</summary>
    void AttachTo(Entity entity);
}

/// <summary>
///     Cat 固有特殊能力：巨型エネルギー弾の設定。
///     GiantOrb.toml からバインドされます。
/// </summary>
public sealed class GiantOrbAbilityConfig : ISpecialAbilityConfig
{
    /// <summary>特殊能力発動後の連続誤爆防止クールダウン（秒）</summary>
    public float Cooldown { get; set; }

    /// <summary>巨型エネルギー弾の1秒あたりの期待接触ダメージ</summary>
    public float DamagePerSecond { get; set; }

    /// <summary>巨型エネルギー弾の接触ダメージティック間隔（秒）</summary>
    public float TickInterval { get; set; }

    /// <summary>巨型エネルギー弾の初速（px/s）</summary>
    public float Speed { get; set; }

    /// <summary>巨型エネルギー弾の接触判定半径（px）</summary>
    public float Radius { get; set; }

    /// <summary>巨型エネルギー弾の最大生存時間（秒）</summary>
    public float Lifetime { get; set; }

    private Color _fillColor;
    private Color _outlineColor;

    /// <summary>巨型エネルギー弾の塗りつぶし色 Hex 文字列</summary>
    [ConfigurationKeyName("FillColor")]
    public string FillColorHex
    {
        get => string.Empty;
        set => _fillColor = value.ToColor();
    }

    /// <summary>巨型エネルギー弾の塗りつぶし色</summary>
    public Color FillColor => _fillColor;

    /// <summary>巨型エネルギー弾の外枠色 Hex 文字列</summary>
    [ConfigurationKeyName("OutlineColor")]
    public string OutlineColorHex
    {
        get => string.Empty;
        set => _outlineColor = value.ToColor();
    }

    /// <summary>巨型エネルギー弾の外枠色</summary>
    public Color OutlineColor => _outlineColor;

    /// <inheritdoc />
    public void AttachTo(Entity entity)
    {
        entity.Set(GiantOrbCapability.FromConfig(this));
    }
}

/// <summary>
///     Bunny 固有特殊能力：全画面斬撃の設定。
///     ScreenCleave.toml からバインドされます。
/// </summary>
public sealed class ScreenCleaveAbilityConfig : ISpecialAbilityConfig
{
    /// <summary>特殊能力発動後の連続誤爆防止クールダウン（秒）</summary>
    public float Cooldown { get; set; }

    /// <summary>全画面斬撃による全敵対ユニットへの期待総確定ダメージ</summary>
    public float TotalDamage { get; set; }

    /// <summary>全画面斬撃の継続ダメージ発生間隔（秒）</summary>
    public float TickInterval { get; set; }

    /// <summary>全画面斬撃発動時の自機無敵時間（秒）</summary>
    public float InvincibleDuration { get; set; }

    /// <summary>全画面斬撃時のカメラ衝撃トラウマ値</summary>
    public float CameraTrauma { get; set; }

    /// <summary>全画面斬撃エフェクトの発生総数</summary>
    public int SlashCount { get; set; }

    /// <summary>全画面斬撃シーケンスの総演出時間（秒）</summary>
    public float TotalDuration { get; set; }

    /// <summary>単一斬撃エフェクトの生存時間（秒）</summary>
    public float SlashLifetime { get; set; }

    /// <summary>斬撃エフェクト最小スケール</summary>
    public float SlashScaleMin { get; set; }

    /// <summary>斬撃エフェクト最大スケール</summary>
    public float SlashScaleMax { get; set; }

    /// <summary>斬撃エフェクトの推進速度（px/s）</summary>
    public float SlashSpeed { get; set; }

    private Color _slashColor;

    /// <summary>斬撃エフェクトの表示色 Hex 文字列</summary>
    [ConfigurationKeyName("SlashColor")]
    public string SlashColorHex
    {
        get => string.Empty;
        set => _slashColor = value.ToColor();
    }

    /// <summary>斬撃エフェクトの表示色</summary>
    public Color SlashColor => _slashColor;

    /// <inheritdoc />
    public void AttachTo(Entity entity)
    {
        entity.Set(ScreenCleaveCapability.FromConfig(this));
    }
}
