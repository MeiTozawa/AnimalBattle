using System.Drawing;
using Microsoft.Extensions.Configuration;

namespace AnimalBattle.Presentation.Rendering;

/// <summary>
///     Config/vfx.render.toml から読み込まれる視覚エフェクト・パーティクル共通描画設定ルートクラス。
/// </summary>
public sealed class VfxRenderConfig
{
    /// <summary>視覚エフェクト・パーティクル共通描画設定</summary>
    public VisualEffectsRenderConfig VisualEffects { get; set; } = new();
}

/// <summary>
///     汎用視覚演出・パーティクル描画パラメータ設定クラス。
/// </summary>
public sealed class VisualEffectsRenderConfig
{
    public int HitSparkCount { get; set; }
    public float HitSparkMinSpeed { get; set; }
    public float HitSparkSpeedRange { get; set; }
    public float HitSparkRadius { get; set; }
    public float HitSparkDuration { get; set; }

    public int PlayerDeathParticleCount { get; set; }
    public float PlayerDeathParticleBaseSpeed { get; set; }
    public float PlayerDeathParticleSpeedStep { get; set; }
    public float PlayerDeathParticleRadius { get; set; }
    public float PlayerDeathParticleDuration { get; set; }

    public float ShockwaveInitialRadius { get; set; }
    public float ShockwaveMaxRadius { get; set; }
    public float ShockwaveDuration { get; set; }

    public int VictoryExplosionCount { get; set; }
    public float VictoryExplosionBaseSpeed { get; set; }
    public float VictoryExplosionSpeedStep { get; set; }
    public float VictoryExplosionRadius { get; set; }
    public float VictoryExplosionDuration { get; set; }

    private Color _jumpRingColor;

    /// <summary>二段ジャンプ空気リング色 Hex 文字列</summary>
    [ConfigurationKeyName("JumpRingColor")]
    public string JumpRingColorHex
    {
        get => string.Empty;
        set { _jumpRingColor = value.ToColor(); }
    }

    /// <summary>二段ジャンプ空気リング色 <see cref="Color" /></summary>
    public Color JumpRingColor => _jumpRingColor;

    private Color _muzzleFlashFillColor;

    /// <summary>銃口閃光塗りつぶし色 Hex 文字列</summary>
    [ConfigurationKeyName("MuzzleFlashFillColor")]
    public string MuzzleFlashFillColorHex
    {
        get => string.Empty;
        set { _muzzleFlashFillColor = value.ToColor(); }
    }

    /// <summary>銃口閃光塗りつぶし色 <see cref="Color" /></summary>
    public Color MuzzleFlashFillColor => _muzzleFlashFillColor;

    private Color _muzzleFlashOutlineColor;

    /// <summary>銃口閃光外枠色 Hex 文字列</summary>
    [ConfigurationKeyName("MuzzleFlashOutlineColor")]
    public string MuzzleFlashOutlineColorHex
    {
        get => string.Empty;
        set { _muzzleFlashOutlineColor = value.ToColor(); }
    }

    /// <summary>銃口閃光外枠色 <see cref="Color" /></summary>
    public Color MuzzleFlashOutlineColor => _muzzleFlashOutlineColor;

    private Color _hitSparkFillColor;

    /// <summary>被弾火花塗りつぶし色 Hex 文字列</summary>
    [ConfigurationKeyName("HitSparkFillColor")]
    public string HitSparkFillColorHex
    {
        get => string.Empty;
        set { _hitSparkFillColor = value.ToColor(); }
    }

    /// <summary>被弾火花塗りつぶし色 <see cref="Color" /></summary>
    public Color HitSparkFillColor => _hitSparkFillColor;

    private Color _hitSparkOutlineColor;

    /// <summary>被弾火花外枠色 Hex 文字列</summary>
    [ConfigurationKeyName("HitSparkOutlineColor")]
    public string HitSparkOutlineColorHex
    {
        get => string.Empty;
        set { _hitSparkOutlineColor = value.ToColor(); }
    }

    /// <summary>被弾火花外枠色 <see cref="Color" /></summary>
    public Color HitSparkOutlineColor => _hitSparkOutlineColor;

    private Color _impactSplatterFillColor;

    /// <summary>着弾破片塗りつぶし色 Hex 文字列</summary>
    [ConfigurationKeyName("ImpactSplatterFillColor")]
    public string ImpactSplatterFillColorHex
    {
        get => string.Empty;
        set { _impactSplatterFillColor = value.ToColor(); }
    }

    /// <summary>着弾破片塗りつぶし色 <see cref="Color" /></summary>
    public Color ImpactSplatterFillColor => _impactSplatterFillColor;

    private Color _impactSplatterOutlineColor;

    /// <summary>着弾破片外枠色 Hex 文字列</summary>
    [ConfigurationKeyName("ImpactSplatterOutlineColor")]
    public string ImpactSplatterOutlineColorHex
    {
        get => string.Empty;
        set { _impactSplatterOutlineColor = value.ToColor(); }
    }

    /// <summary>着弾破片外枠色 <see cref="Color" /></summary>
    public Color ImpactSplatterOutlineColor => _impactSplatterOutlineColor;

    private Color _playerDeathParticleFillColor;

    /// <summary>自機死亡破片塗りつぶし色 Hex 文字列</summary>
    [ConfigurationKeyName("PlayerDeathParticleFillColor")]
    public string PlayerDeathParticleFillColorHex
    {
        get => string.Empty;
        set { _playerDeathParticleFillColor = value.ToColor(); }
    }

    /// <summary>自機死亡破片塗りつぶし色 <see cref="Color" /></summary>
    public Color PlayerDeathParticleFillColor => _playerDeathParticleFillColor;

    private Color _playerDeathParticleOutlineColor;

    /// <summary>自機死亡破片外枠色 Hex 文字列</summary>
    [ConfigurationKeyName("PlayerDeathParticleOutlineColor")]
    public string PlayerDeathParticleOutlineColorHex
    {
        get => string.Empty;
        set { _playerDeathParticleOutlineColor = value.ToColor(); }
    }

    /// <summary>自機死亡破片外枠色 <see cref="Color" /></summary>
    public Color PlayerDeathParticleOutlineColor => _playerDeathParticleOutlineColor;

    private Color _victoryExplosionFillColor;

    /// <summary>勝利祝賀爆発塗りつぶし色 Hex 文字列</summary>
    [ConfigurationKeyName("VictoryExplosionFillColor")]
    public string VictoryExplosionFillColorHex
    {
        get => string.Empty;
        set { _victoryExplosionFillColor = value.ToColor(); }
    }

    /// <summary>勝利祝賀爆発塗りつぶし色 <see cref="Color" /></summary>
    public Color VictoryExplosionFillColor => _victoryExplosionFillColor;

    private Color _victoryExplosionOutlineColor;

    /// <summary>勝利祝賀爆発外枠色 Hex 文字列</summary>
    [ConfigurationKeyName("VictoryExplosionOutlineColor")]
    public string VictoryExplosionOutlineColorHex
    {
        get => string.Empty;
        set { _victoryExplosionOutlineColor = value.ToColor(); }
    }

    /// <summary>勝利祝賀爆発外枠色 <see cref="Color" /></summary>
    public Color VictoryExplosionOutlineColor => _victoryExplosionOutlineColor;

    private Color _shockwaveRingColor;

    /// <summary>衝撃波リング色 Hex 文字列</summary>
    [ConfigurationKeyName("ShockwaveRingColor")]
    public string ShockwaveRingColorHex
    {
        get => string.Empty;
        set { _shockwaveRingColor = value.ToColor(); }
    }

    /// <summary>衝撃波リング色 <see cref="Color" /></summary>
    public Color ShockwaveRingColor => _shockwaveRingColor;
}
