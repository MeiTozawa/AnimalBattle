namespace AnimalBattle.Maps.Data;

/// <summary>
///     2D タイルマップのレベルデータ契約クラス。
///     Tiled / LDtk 等の外部レベルエディタから出力された JSON 構造を表現します。
/// </summary>
public sealed class LevelData
{
    /// <summary>マップの横方向タイル数</summary>
    public int Width { get; set; }

    /// <summary>マップの縦方向タイル数</summary>
    public int Height { get; set; }

    /// <summary>タイル単体の幅（ピクセル）</summary>
    public int TileWidth { get; set; }

    /// <summary>タイル単体の高さ（ピクセル）</summary>
    public int TileHeight { get; set; }

    /// <summary>含まれるタイルレイヤー一覧</summary>
    public List<TileLayerData> Layers { get; set; } = [];
}

/// <summary>
///     単一レイヤー内のグリッドタイル配置データ。
/// </summary>
public sealed class TileLayerData
{
    /// <summary>レイヤー識別名（例: "Platforms", "Background"）</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>1次元展開されたタイル ID 配列 (0 は空白/空気)</summary>
    public List<int> Data { get; set; } = [];
}
