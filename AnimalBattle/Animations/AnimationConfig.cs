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

/// <summary>
/// 1つのアニメーションクリップの設定を表します。
/// </summary>
public sealed class AnimationClipConfig
{
    /// <summary>クリップ名 (例: "Idle", "Run", "Jump")</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>スプライトシート内の行インデックス (0始まり)</summary>
    public int Row { get; set; }

    /// <summary>このクリップ専用のセル幅 (0 の場合は AnimationConfig.CellWidth を使用)</summary>
    public int CellWidth { get; set; }

    /// <summary>このクリップ専用のセル高さ (0 の場合は AnimationConfig.CellHeight を使用)</summary>
    public int CellHeight { get; set; }

    /// <summary>フレーム数</summary>
    public int FrameCount { get; set; } = 1;

    /// <summary>再生フレームレート (FPS)</summary>
    public float Fps { get; set; } = 6.0f;

    /// <summary>ループ再生するかどうか</summary>
    public bool IsLooping { get; set; } = true;
}
