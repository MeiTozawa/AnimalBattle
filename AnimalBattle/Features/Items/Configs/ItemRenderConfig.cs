namespace AnimalBattle.Features.Items.Configs;

using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using AnimalBattle.Core;
using Microsoft.Extensions.Configuration;

/// <summary>
///     Config/items.render.toml から読み込まれるアイテム描画設定クラス。
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class ItemRenderConfig
{
    /// <summary>血包（Health Pack）の描画設定データ。</summary>
    public HealthPackRenderConfig HealthPackRender { get; set; } = null!;
}

/// <summary>
///     血包（Health Pack）の配色・寸法・幾何描画設定クラス。
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class HealthPackRenderConfig
{
    private Color _auraColor;

    /// <summary>外周オーラ色 Hex 文字列</summary>
    [ConfigurationKeyName("AuraColor")]
    public string AuraColorHex
    {
        get => string.Empty;
        set => _auraColor = value.ToColor();
    }

    /// <summary>外周オーラ色 <see cref="Color" /></summary>
    public Color AuraColor => _auraColor;

    private Color _coreColor;

    /// <summary>内部コア発光色 Hex 文字列</summary>
    [ConfigurationKeyName("CoreColor")]
    public string CoreColorHex
    {
        get => string.Empty;
        set => _coreColor = value.ToColor();
    }

    /// <summary>内部コア発光色 <see cref="Color" /></summary>
    public Color CoreColor => _coreColor;

    private Color _outlineColor;

    /// <summary>コア境界輪郭線色 Hex 文字列</summary>
    [ConfigurationKeyName("OutlineColor")]
    public string OutlineColorHex
    {
        get => string.Empty;
        set => _outlineColor = value.ToColor();
    }

    /// <summary>コア境界輪郭線色 <see cref="Color" /></summary>
    public Color OutlineColor => _outlineColor;

    private Color _crossColor;

    /// <summary>救急十字アイコン色 Hex 文字列</summary>
    [ConfigurationKeyName("CrossColor")]
    public string CrossColorHex
    {
        get => string.Empty;
        set => _crossColor = value.ToColor();
    }

    /// <summary>救急十字アイコン色 <see cref="Color" /></summary>
    public Color CrossColor => _crossColor;

    /// <summary>外周オーラ基本半径（px）。</summary>
    public float OuterRadius { get; set; }

    /// <summary>内部コア基本半径（px）。</summary>
    public float InnerRadius { get; set; }

    /// <summary>救急十字アイコンのアーム半長（px）。</summary>
    public int CrossArmLength { get; set; }

    /// <summary>救急十字アイコンのアーム半幅（px）。</summary>
    public int CrossArmThickness { get; set; }
}
