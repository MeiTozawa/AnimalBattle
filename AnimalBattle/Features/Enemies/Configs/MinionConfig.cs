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
    public float MaxHp { get; set; }

    /// <summary>移動速度。単位はピクセル毎秒。</summary>
    public float MoveSpeed { get; set; }

    /// <summary>プレイヤー接触時の与ダメージ。</summary>
    public float ContactDamage { get; set; }

    /// <summary>足場衝突および被弾判定サイズ。</summary>
    public Vec2f CollisionSize { get; set; }

    /// <summary>スプライトの描画スケール。</summary>
    public Vec2f Scale { get; set; }

    /// <summary>被弾時ホワイトフラッシュの持続秒数。</summary>
    public float HitFlashDuration { get; set; }

    /// <summary>生成基準オフセット座標。</summary>
    public Vec2f SpawnPos { get; set; }

    /// <summary>生成オフセット座標の別名。</summary>
    public Vec2f SpawnOffset
    {
        get => SpawnPos;
        set => SpawnPos = value;
    }

    /// <summary>跳躍初速度。単位はピクセル毎秒。</summary>
    public float JumpForce { get; set; }

    /// <summary>跳躍実行のクールダウン秒数。</summary>
    public float JumpCooldown { get; set; }
}

/// <summary>
///     空中浮遊弾幕怪の設定パラメータ。
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class FlyingMinionConfig
{
    /// <summary>最大体力。</summary>
    public float MaxHp { get; set; }

    /// <summary>水平追跡移動速度。単位はピクセル毎秒。</summary>
    public float HoverSpeed { get; set; }

    /// <summary>垂直正弦波浮遊の振幅。単位はピクセル。</summary>
    public float HoverAmpY { get; set; }

    /// <summary>垂直正弦波浮遊の周波数。単位はラジアン毎秒。</summary>
    public float HoverFreq { get; set; }

    /// <summary>足場衝突および被弾判定サイズ。</summary>
    public Vec2f CollisionSize { get; set; }

    /// <summary>スプライトの描画スケール。</summary>
    public Vec2f Scale { get; set; }

    /// <summary>被弾時ホワイトフラッシュの持続秒数。</summary>
    public float HitFlashDuration { get; set; }

    /// <summary>プレイヤー接触時の与ダメージ。</summary>
    public float ContactDamage { get; set; }

    /// <summary>使用するスキル識別子。</summary>
    public string SkillTag { get; set; } = string.Empty;

    /// <summary>スキル発動のクールダウン秒数。</summary>
    public float SkillCooldown { get; set; }

    /// <summary>生成基準オフセット座標。</summary>
    public Vec2f SpawnPos { get; set; }

    /// <summary>生成オフセット座標の別名。</summary>
    public Vec2f SpawnOffset
    {
        get => SpawnPos;
        set => SpawnPos = value;
    }
}
