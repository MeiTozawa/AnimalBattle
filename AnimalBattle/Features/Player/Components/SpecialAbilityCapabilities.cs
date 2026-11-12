using System.Drawing;

namespace AnimalBattle.Player.Components;

/// <summary>
///     Cat 固有特殊能力：巨型エネルギー弾発射能力コンポーネント。
///     Prefab に付与され、実行時の数値と表現属性を提供します。
/// </summary>
public struct GiantOrbCapability
{
    /// <summary>特殊能力発動後の連続誤爆防止クールダウン（秒）</summary>
    public float Cooldown;

    /// <summary>巨型エネルギー弾の1秒あたりの期待接触ダメージ</summary>
    public float DamagePerSecond;

    /// <summary>巨型エネルギー弾の接触ダメージティック間隔（秒）</summary>
    public float TickInterval;

    /// <summary>巨型エネルギー弾の初速（px/s）</summary>
    public float Speed;

    /// <summary>巨型エネルギー弾の接触判定半径（px）</summary>
    public float Radius;

    /// <summary>巨型エネルギー弾の最大生存時間（秒）</summary>
    public float Lifetime;

    /// <summary>巨型エネルギー弾の塗りつぶし色</summary>
    public Color FillColor;

    /// <summary>巨型エネルギー弾の外枠色</summary>
    public Color OutlineColor;

    /// <summary>
    ///     設定オブジェクトから巨型エネルギー弾能力コンポーネントを構築します。
    /// </summary>
    /// <param name="cfg">巨型エネルギー弾設定</param>
    /// <returns>構築された <see cref="GiantOrbCapability"/> インスタンス</returns>
    public static GiantOrbCapability FromConfig(GiantOrbAbilityConfig cfg)
    {
        Debug.Assert(cfg != null, "GiantOrbAbilityConfig は null であってはなりません。");
        return new GiantOrbCapability
        {
            Cooldown = cfg.Cooldown,
            DamagePerSecond = cfg.DamagePerSecond,
            TickInterval = cfg.TickInterval,
            Speed = cfg.Speed,
            Radius = cfg.Radius,
            Lifetime = cfg.Lifetime,
            FillColor = cfg.FillColor,
            OutlineColor = cfg.OutlineColor
        };
    }
}

/// <summary>
///     Bunny 固有特殊能力：全画面斬撃能力コンポーネント。
///     Prefab に付与され、実行時の数値と表現属性を提供します。
/// </summary>
public struct ScreenCleaveCapability
{
    /// <summary>特殊能力発動後の連続誤爆防止クールダウン（秒）</summary>
    public float Cooldown;

    /// <summary>全画面斬撃による全敵対ユニットへの期待総確定ダメージ</summary>
    public float TotalDamage;

    /// <summary>全画面斬撃の継続ダメージ発生間隔（秒）</summary>
    public float TickInterval;

    /// <summary>全画面斬撃発動時の自機無敵時間（秒）</summary>
    public float InvincibleDuration;

    /// <summary>全画面斬撃時のカメラ衝撃トラウマ値</summary>
    public float CameraTrauma;

    /// <summary>全画面斬撃エフェクトの発生総数</summary>
    public int SlashCount;

    /// <summary>全画面斬撃シーケンスの総演出時間（秒）</summary>
    public float TotalDuration;

    /// <summary>単一斬撃エフェクトの生存時間（秒）</summary>
    public float SlashLifetime;

    /// <summary>斬撃エフェクト最小スケール</summary>
    public float ScaleMin;

    /// <summary>斬撃エフェクト最大スケール</summary>
    public float ScaleMax;

    /// <summary>斬撃エフェクトの推進速度（px/s）</summary>
    public float SlashSpeed;

    /// <summary>斬撃エフェクトの表示色</summary>
    public Color SlashColor;

    /// <summary>
    ///     設定オブジェクトから全画面斬撃能力コンポーネントを構築します。
    /// </summary>
    /// <param name="cfg">全画面斬撃設定</param>
    /// <returns>構築された <see cref="ScreenCleaveCapability"/> インスタンス</returns>
    public static ScreenCleaveCapability FromConfig(ScreenCleaveAbilityConfig cfg)
    {
        Debug.Assert(cfg != null, "ScreenCleaveAbilityConfig は null であってはなりません。");
        return new ScreenCleaveCapability
        {
            Cooldown = cfg.Cooldown,
            TotalDamage = cfg.TotalDamage,
            TickInterval = cfg.TickInterval,
            InvincibleDuration = cfg.InvincibleDuration,
            CameraTrauma = cfg.CameraTrauma,
            SlashCount = cfg.SlashCount,
            TotalDuration = cfg.TotalDuration,
            SlashLifetime = cfg.SlashLifetime,
            ScaleMin = cfg.SlashScaleMin,
            ScaleMax = cfg.SlashScaleMax,
            SlashSpeed = cfg.SlashSpeed,
            SlashColor = cfg.SlashColor
        };
    }
}
