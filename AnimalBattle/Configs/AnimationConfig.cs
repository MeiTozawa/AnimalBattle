using System.Collections.Generic;

namespace AnimalBattle.Configs;

/// <summary>
/// スプライトシート単位のアニメーション設定クラス。
/// セル寸法およびクリップ一覧を保持します。
/// </summary>
public sealed class AnimationConfig
{
    /// <summary>キャラクター/エフェクトの表示名</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>デフォルトのセル幅（ピクセル）</summary>
    public int CellWidth { get; set; } = 32;

    /// <summary>デフォルトのセル高さ（ピクセル）</summary>
    public int CellHeight { get; set; } = 32;

    /// <summary>クリップ一覧</summary>
    public List<AnimationClipConfig> Clips { get; set; } = [];

    /// <summary>エフェクト専用クリップ一覧（後方互換プロパティ）</summary>
    public List<AnimationClipConfig> EffectClips { get; set; } = [];
}
