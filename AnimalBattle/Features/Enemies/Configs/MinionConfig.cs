using System.Diagnostics.CodeAnalysis;

namespace AnimalBattle.Configs;

/// <summary>
///     Config/minions.toml から読み込まれる雑魚敵設定クラス。
///     地上近接怪および空中浮遊怪の各種パラメータを保持します。
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class MinionConfig
{
    /// <summary>地面近接追撃怪の設定。</summary>
    public GroundMinionConfig GroundMinion { get; set; } = new();

    /// <summary>空中浮遊弾幕怪の設定。</summary>
    public FlyingMinionConfig FlyingMinion { get; set; } = new();
}

/// <summary>
///     地面近接追撃怪の設定パラメータ。
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class GroundMinionConfig
{
    /// <summary>最大体力。</summary>
    public float MaxHp { get; set; } = 25.0f;

    /// <summary>移動速度。単位はピクセル毎秒。</summary>
    public float MoveSpeed { get; set; } = 110.0f;

    /// <summary>プレイヤー接触時の与ダメージ。</summary>
    public float ContactDamage { get; set; } = 1.0f;

    /// <summary>足場衝突および被弾判定サイズ。</summary>
    public Vec2f CollisionSize { get; set; } = new(32.0f, 32.0f);

    /// <summary>スプライトの描画スケール。</summary>
    public Vec2f Scale { get; set; } = new(0.65f, 0.65f);

    /// <summary>被弾時ホワイトフラッシュの持続秒数。</summary>
    public float HitFlashDuration { get; set; } = 0.12f;
}

/// <summary>
///     空中浮遊弾幕怪の設定パラメータ。
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class FlyingMinionConfig
{
    /// <summary>最大体力。</summary>
    public float MaxHp { get; set; } = 18.0f;

    /// <summary>水平追跡移動速度。単位はピクセル毎秒。</summary>
    public float HoverSpeed { get; set; } = 90.0f;

    /// <summary>垂直正弦波浮遊の振幅。単位はピクセル。</summary>
    public float HoverAmpY { get; set; } = 24.0f;

    /// <summary>垂直正弦波浮遊の周波数。単位はラジアン毎秒。</summary>
    public float HoverFreq { get; set; } = 2.2f;

    /// <summary>足場衝突および被弾判定サイズ。</summary>
    public Vec2f CollisionSize { get; set; } = new(28.0f, 28.0f);

    /// <summary>スプライトの描画スケール。</summary>
    public Vec2f Scale { get; set; } = new(0.55f, 0.55f);

    /// <summary>被弾時ホワイトフラッシュの持続秒数。</summary>
    public float HitFlashDuration { get; set; } = 0.12f;

    /// <summary>プレイヤー接触時の与ダメージ。</summary>
    public float ContactDamage { get; set; } = 1.0f;

    /// <summary>使用するスキル識別子。</summary>
    public string SkillTag { get; set; } = "RingBurst";

    /// <summary>スキル発動のクールダウン秒数。</summary>
    public float SkillCooldown { get; set; } = 5.0f;
}
