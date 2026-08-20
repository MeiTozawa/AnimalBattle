namespace AnimalBattle.Components;

/// <summary>
/// 2D 幾何衝突形状を表す代数的データ型 (Discriminated Union / 代数变体) の抽象基底レコード。
/// 外部派生を防ぐためコンストラクタを private に封鎖し、型安全なパターンマッチングを提供します。
/// </summary>
public abstract record CollisionShape
{
    /// <summary>エンティティ中心座標 (Transform.X) からの X 軸局所オフセット</summary>
    public float OffsetX { get; init; }

    /// <summary>エンティティ中心座標 (Transform.Y) からの Y 軸局所オフセット</summary>
    public float OffsetY { get; init; }

    // 外部からの不正派生を防止し、閉じた判別共用体 (Discriminated Union) を構築
    private CollisionShape(float offsetX, float offsetY)
    {
        OffsetX = offsetX;
        OffsetY = offsetY;
    }

    /// <summary>
    /// 円形衝突判定レコード（弾幕、擦弾点、コア等）。
    /// </summary>
    /// <param name="Radius">円の半径（ピクセル）</param>
    /// <param name="OffsetX">X 軸局所オフセット（初期値: 0）</param>
    /// <param name="OffsetY">Y 軸局所オフセット（初期値: 0）</param>
    public sealed record Circle(float Radius, float OffsetX = 0.0f, float OffsetY = 0.0f) : CollisionShape(OffsetX, OffsetY);

    /// <summary>
    /// 軸平行矩形 (AABB) 衝突判定レコード（地形、キャラクター胴体等）。
    /// </summary>
    /// <param name="Width">矩形の全幅（ピクセル）</param>
    /// <param name="Height">矩形の全高（ピクセル）</param>
    /// <param name="OffsetX">X 軸局所オフセット（初期値: 0）</param>
    /// <param name="OffsetY">Y 軸局所オフセット（初期値: 0）</param>
    public sealed record Box(float Width, float Height, float OffsetX = 0.0f, float OffsetY = 0.0f) : CollisionShape(OffsetX, OffsetY);

    /// <summary>
    /// 点 (Point) 衝突判定レコード（精密な極小判定等）。
    /// </summary>
    /// <param name="OffsetX">X 軸局所オフセット（初期値: 0）</param>
    /// <param name="OffsetY">Y 軸局所オフセット（初期値: 0）</param>
    public sealed record Point(float OffsetX = 0.0f, float OffsetY = 0.0f) : CollisionShape(OffsetX, OffsetY);
}

