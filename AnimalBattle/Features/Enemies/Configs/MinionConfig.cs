using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using Microsoft.Extensions.Configuration;

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

    /// <summary>雑魚敵HPバーの設定。</summary>
    public MinionHpBarConfig HpBar { get; set; } = new();
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

    /// <summary>プレイヤーからの最小安全距離。単位はピクセル。</summary>
    public float MinPlayerDistance { get; set; }

    /// <summary>候補生成座標リスト。</summary>
    public List<Vec2f> SpawnPoints { get; set; } = [];

    /// <summary>被弾時のスカッシュ変形スケール（X: 横伸長, Y: 縦圧縮）。</summary>
    public Vec2f HitSquashScale { get; set; }

    /// <summary>被弾時スプリング変形の固有振動数（Hz）。</summary>
    public float SpringFrequency { get; set; }

    /// <summary>被弾時スプリング変形の減衰比（0.0〜1.0）。</summary>
    public float SpringDampingRatio { get; set; }
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

    /// <summary>プレイヤーからの最小安全距離。単位はピクセル。</summary>
    public float MinPlayerDistance { get; set; }

    /// <summary>候補生成座標リスト。</summary>
    public List<Vec2f> SpawnPoints { get; set; } = [];

    /// <summary>被弾時のスカッシュ変形スケール（X: 横伸長, Y: 縦圧縮）。</summary>
    public Vec2f HitSquashScale { get; set; } = new(1.15f, 0.85f);

    /// <summary>被弾時スプリング変形の固有振動数（Hz）。</summary>
    public float SpringFrequency { get; set; } = 15.0f;

    /// <summary>被弾時スプリング変形の減衰比（0.0〜1.0）。</summary>
    public float SpringDampingRatio { get; set; } = 0.5f;
}

/// <summary>
///     雑魚敵頭上HPバーの設定パラメータ。
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class MinionHpBarConfig
{
    /// <summary>外枠の幅。単位はピクセル。</summary>
    public float Width { get; set; }

    /// <summary>外枠の高さ。単位はピクセル。</summary>
    public float Height { get; set; }

    /// <summary>衝突判定上端からの垂直オフセット。単位はピクセル。</summary>
    public float OffsetY { get; set; }

    /// <summary>外枠の内側余白。単位はピクセル。</summary>
    public int BorderPadding { get; set; }

    /// <summary>低HP警告表示に切り替える残HP割合の閾値。0.0から1.0の範囲。</summary>
    public float LowHpThreshold { get; set; }

    private Color _bgColor;

    /// <summary>背景色のHex文字列。</summary>
    [ConfigurationKeyName("BgColor")]
    public string BgColorHex
    {
        get => string.Empty;
        set => _bgColor = value.ToColor();
    }

    /// <summary>背景色。</summary>
    public Color BgColor
    {
        get => _bgColor;
        set => _bgColor = value;
    }

    private Color _borderColor;

    /// <summary>外枠色のHex文字列。</summary>
    [ConfigurationKeyName("BorderColor")]
    public string BorderColorHex
    {
        get => string.Empty;
        set => _borderColor = value.ToColor();
    }

    /// <summary>外枠色。</summary>
    public Color BorderColor
    {
        get => _borderColor;
        set => _borderColor = value;
    }

    private Color _fillColor;

    /// <summary>通常ゲージ色のHex文字列。</summary>
    [ConfigurationKeyName("FillColor")]
    public string FillColorHex
    {
        get => string.Empty;
        set => _fillColor = value.ToColor();
    }

    /// <summary>通常ゲージ色。</summary>
    public Color FillColor
    {
        get => _fillColor;
        set => _fillColor = value;
    }

    private Color _lowHpColor;

    /// <summary>低HP警告ゲージ色のHex文字列。</summary>
    [ConfigurationKeyName("LowHpColor")]
    public string LowHpColorHex
    {
        get => string.Empty;
        set => _lowHpColor = value.ToColor();
    }

    /// <summary>低HP警告ゲージ色。</summary>
    public Color LowHpColor
    {
        get => _lowHpColor;
        set => _lowHpColor = value;
    }
}
