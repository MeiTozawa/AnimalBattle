using System.Collections.Generic;

namespace AnimalBattle.Animations;
/// <summary>
/// キャラクターアニメーションの設定を表します。
/// resource/Characters/*.toml として配置され、スプライトシートのセル定義を記述します。
/// </summary>
public sealed class AnimationConfig
{
    /// <summary>このキャラクターの表示名</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>スプライトシートの基本セル幅 (px)</summary>
    public int CellWidth { get; set; } = 32;

    /// <summary>スプライトシートの基本セル高さ (px)</summary>
    public int CellHeight { get; set; } = 32;

    /// <summary>アニメーションクリップ定義のリスト</summary>
    public List<AnimationClipConfig> Clips { get; set; } = [];

    /// <summary>エフェクトスプライトシートのファイル名（省略可）</summary>
    public string EffectSprite { get; set; } = string.Empty;

    /// <summary>エフェクトクリップ定義のリスト</summary>
    public List<AnimationClipConfig> EffectClips { get; set; } = [];
}
