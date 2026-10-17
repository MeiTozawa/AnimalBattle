namespace AnimalBattle.Configs;

/// <summary>
///     個別のアニメーションクリップ（行・フレーム範囲・FPS・ループ有無等）の定義クラス。
/// </summary>
public sealed class AnimationClipConfig {
    /// <summary>アニメーション名（例: "Idle", "Run", "Jump"）</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>このクリップ専用のスプライトシート相対パス（空の場合は親の CharacterSprites を使用）</summary>
    public string SpritePath { get; set; } = string.Empty;

    /// <summary>スプライトシート上の行インデックス（0 始まり）</summary>
    public int Row { get; set; }

    /// <summary>開始フレーム列インデックス（0 始まり）</summary>
    public int StartFrame { get; set; }

    /// <summary>終了フレーム列インデックス（0 始まり）</summary>
    public int EndFrame { get; set; }

    /// <summary>このクリップ専用のセル幅 (0 の場合は AnimationConfig.CellWidth を使用)</summary>
    public int CellWidth { get; set; }

    /// <summary>このクリップ専用のセル高さ (0 の場合は AnimationConfig.CellHeight を使用)</summary>
    public int CellHeight { get; set; }

    /// <summary>再生速度（フレーム毎秒）</summary>
    public float Fps { get; set; }

    /// <summary>ループ再生するかどうか</summary>
    public bool IsLooping { get; set; }
}