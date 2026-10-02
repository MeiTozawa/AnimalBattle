using ErrorOr;

namespace AnimalBattle.Configs;

/// <summary>
///     タイルマップの描画・配置設定クラス。
/// </summary>
public sealed class TileMapConfig {
    /// <summary>タイルセットテクスチャの相対パス</summary>
    public string TileSheetPath { get; set; } = string.Empty;

    /// <summary>タイルセットメタデータ TOML の相対パス</summary>
    public string TileConfigPath { get; set; } = string.Empty;

    /// <summary>描画時のタイル単体幅（ピクセル）</summary>
    public int RenderTileWidth { get; set; }

    /// <summary>描画時のタイル単体高さ（ピクセル）</summary>
    public int RenderTileHeight { get; set; }

    /// <summary>マップ原点 X 座標（左上）</summary>
    public float OriginX { get; set; }

    /// <summary>マップ原点 Y 座標（左上）</summary>
    public float OriginY { get; set; }

    /// <summary>2D タイル ID グリッドデータ（0 = 空白/空気）</summary>
    public List<List<int>> Data { get; set; } = [];
}

/// <summary>
///     Config/arena.toml から読み込まれる戦闘アリーナ境界およびタイルマップ設定クラス。
/// </summary>
public sealed class ArenaConfig {
    /// <summary>デフォルトで生成・交戦する対象 Boss 識別名（"Boss1" または "Boss2"）</summary>
    public string TargetBoss { get; set; } = string.Empty;

    /// <summary>タイルマップ配置設定</summary>
    public TileMapConfig TileMap { get; set; } = new();

    /// <summary>プレイヤー移動可能な最小 X 境界</summary>
    public float BoundaryMinX { get; set; }

    /// <summary>プレイヤー移動可能な最大 X 境界</summary>
    public float BoundaryMaxX { get; set; }

    /// <summary>銃弾および弾幕のデスポーン最小 X 座標</summary>
    public float BulletDespawnMinX { get; set; }

    /// <summary>銃弾および弾幕のデスポーン最大 X 座標</summary>
    public float BulletDespawnMaxX { get; set; }

    /// <summary>銃弾および弾幕のデスポーン最小 Y 座標</summary>
    public float BulletDespawnMinY { get; set; }

    /// <summary>銃弾および弾幕のデスポーン最大 Y 座標</summary>
    public float BulletDespawnMaxY { get; set; }

    /// <summary>地上キャラクター（プレイヤー・Boss）の床面下限 Y 座標</summary>
    public float FloorLimitY { get; set; } = 560.0f;

    /// <summary>
    ///     指定された TOML ファイルから設定を読み込んだ <see cref="ConfigBase{ArenaConfig}" /> インスタンスを生成します。
    /// </summary>
    /// <param name="path">TOML ファイル名</param>
    /// <returns>読み込み結果</returns>
    public static ErrorOr<ConfigBase<ArenaConfig>> LoadFromFile(string path = ResourcePath.Config.Arena)
    {
        return ConfigBase<ArenaConfig>.LoadFromFile(path);
    }
}