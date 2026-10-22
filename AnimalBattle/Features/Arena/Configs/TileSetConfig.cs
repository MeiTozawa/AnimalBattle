using ErrorOr;

namespace AnimalBattle.Configs;

/// <summary>
///     Resource/Platform/sheet.toml 等から読み込まれるタイルセット設定クラス。
/// </summary>
public sealed class TileSetConfig {
    /// <summary>タイル単体の幅ピクセル単位</summary>
    public int TileWidth { get; set; }

    /// <summary>タイル単体の高さピクセル単位</summary>
    public int TileHeight { get; set; }

    /// <summary>テクスチャシート内の横方向タイル列数</summary>
    public int Columns { get; set; }

    /// <summary>テクスチャシート内の縦方向タイル行数</summary>
    public int Rows { get; set; }

    /// <summary>左端ブロックのタイル ID 1基準</summary>
    public int LeftBlock { get; set; }

    /// <summary>中間ブロックのタイル ID 1基準</summary>
    public int MiddleBlock { get; set; }

    /// <summary>右端ブロックのタイル ID 1基準</summary>
    public int RightBlock { get; set; }

    /// <summary>衝突判定 左上 X 座標</summary>
    public int CollisionX1 { get; set; }

    /// <summary>衝突判定 左上 Y 座標</summary>
    public int CollisionY1 { get; set; }

    /// <summary>衝突判定 右下 X 座標</summary>
    public int CollisionX2 { get; set; }

    /// <summary>衝突判定 右下 Y 座標</summary>
    public int CollisionY2 { get; set; }

    /// <summary>衝突判定領域の幅</summary>
    public int CollisionWidth => CollisionX2 - CollisionX1;

    /// <summary>衝突判定領域の高さ</summary>
    public int CollisionHeight => CollisionY2 - CollisionY1;
}