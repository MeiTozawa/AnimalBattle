using AnimalBattle.Core;
using ErrorOr;

namespace AnimalBattle.Configs;

/// <summary>
///     2D 空間ハッシュグリッド設定クラス。
/// </summary>
public sealed class SpatialHashConfig {
    /// <summary>空間ハッシュの正方形セル一辺のサイズ（ピクセル）</summary>
    public float CellSize { get; set; }

    /// <summary>ハッシュテーブルのバケット数（2の冪乗）</summary>
    public int TableSize { get; set; }

    /// <summary>初期許容 Hurtbox エンティティ数</summary>
    public int InitialCapacity { get; set; }
}

/// <summary>
///     プラットフォームナビゲーショングラフおよび A* 探索設定クラス。
/// </summary>
public sealed class NavigationGraphConfig {
    /// <summary>放物線跳躍軌道の障害物遮断レイキャストサンプリング分割数</summary>
    public int TrajectorySampleSteps { get; set; }

    /// <summary>Boss 足元プラットフォーム判定の垂直許容誤差（ピクセル）</summary>
    public float BossNodeToleranceY { get; set; }

    /// <summary>Boss 足元プラットフォーム判定の水平はみ出しマージン（ピクセル）</summary>
    public float BossNodeMarginX { get; set; }

    /// <summary>目標プレイヤー足元プラットフォーム判定の垂直許容誤差（ピクセル）</summary>
    public float TargetToleranceY { get; set; }

    /// <summary>目標プレイヤー足元プラットフォーム判定の水平はみ出しマージン（ピクセル）</summary>
    public float TargetMarginX { get; set; }

    /// <summary>跳躍離陸点への接近判定距離（ピクセル）</summary>
    public float TakeoffTriggerDistance { get; set; }

    /// <summary>下落離脱点への接近判定距離（ピクセル）</summary>
    public float DropTriggerDistance { get; set; }

    /// <summary>上方跳躍として判定する最小高度差（ピクセル）</summary>
    public float MinJumpDeltaY { get; set; }

    /// <summary>下方下落として判定する高度差上限（ピクセル、负値）</summary>
    public float MinDropDeltaY { get; set; }

    /// <summary>プラットフォーム水平重なり判定の最小重なり幅（ピクセル）</summary>
    public float PlatformOverlapThreshold { get; set; }

    /// <summary>プラットフォーム端からの踏切・着地安全マージン（ピクセル）</summary>
    public float PlatformEdgeMargin { get; set; }

    /// <summary>上下重畳足場間の斜め跳躍時、踏切位置を重なり境界より外側へオフセットする距離（ピクセル）</summary>
    public float OverlapTakeoffOffsetX { get; set; }

    /// <summary>上下重畳足場間の斜め跳躍時、着地目標を足場内側へ深く踏み込ませるオフセット距離（ピクセル）</summary>
    public float OverlapLandingOffsetX { get; set; }

    /// <summary>足場端からの段差下落時、下方足場への着地予測点を外側へオフセットする距離（ピクセル）</summary>
    public float DropLandingOffsetX { get; set; }

    /// <summary>跳躍最高到達時間に対する滞空許容時間倍率</summary>
    public float JumpWindowTimeMultiplier { get; set; }

    /// <summary>滞空水平移動速度の最小許容倍率（巡航速度比）</summary>
    public float MinJumpVxRatio { get; set; }

    /// <summary>滞空水平移動速度の最大許容倍率（巡航速度比）</summary>
    public float MaxJumpVxRatio { get; set; }

    /// <summary>上方跳躍移動の基本コスト加算値</summary>
    public float JumpCostBase { get; set; }

    /// <summary>足場間ギャップを跨ぐ斜め跳躍の基本コスト加算値</summary>
    public float CrossGapJumpCostBase { get; set; }

    /// <summary>段差下落移動の基本コスト加算値</summary>
    public float DropCostBase { get; set; }

    /// <summary>段差下落移動の落下高度乗数</summary>
    public float DropCostHeightMultiplier { get; set; }
}

/// <summary>
///     タイルマップの描画・配置設定クラス。
/// </summary>
public sealed class TileMapConfig {
    /// <summary>タイルセットテクスチャの相対パス</summary>
    public string TileSheetPath { get; set; } = string.Empty;

    /// <summary>描画時のタイル単体サイズ (Vec2i)</summary>
    public Vec2i RenderTileSize { get; set; }

    /// <summary>マップ原点座標 (Vec2f)</summary>
    public Vec2f Origin { get; set; }

    /// <summary>有効なプラットフォームとして判定する上面 Y 座標の上限（ピクセル）</summary>
    public float MaxPlatformY { get; set; }

    /// <summary>有効なプラットフォームとして判定する最小幅（ピクセル）</summary>
    public float MinPlatformWidth { get; set; }

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

    /// <summary>空間ハッシュグリッド設定</summary>
    public SpatialHashConfig SpatialHash { get; set; } = new();

    /// <summary>ナビゲーショングラフ設定</summary>
    public NavigationGraphConfig NavigationGraph { get; set; } = new();

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
    public float FloorLimitY { get; set; }

    /// <summary>物理更新の最大デルタタイム保護上限（秒）</summary>
    public float MaxDeltaTime { get; set; }
}