using System;
using AnimalBattle.Core;
using AnimalBattle.Core.Physics;

namespace AnimalBattle.Components;

/// <summary>
/// 2D 幾何衝突形状を表す代数的データ型 (Discriminated Union / 代数变体) の抽象基底レコード。
/// 外部派生を防ぐためコンストラクタを private に封鎖し、型安全なパターンマッチングと SimpleError による厳密なエラーハンドリングを提供します。
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

    /// <summary>
    /// 形状文字列（"Circle", "Box", "Point"）および各種寸法・オフセットから衝突形状インスタンスの生成を行います。
    /// 未対応の形状種別や寸法不正時は <see cref="SimpleError"/> を返します。
    /// </summary>
    /// <param name="shapeKind">形状種別文字列（"Circle", "Box", "Point"）</param>
    /// <param name="radius">円形半径（Circle 時に使用、> 0 である必要があります）</param>
    /// <param name="width">矩形幅（Box 時に使用、> 0 である必要があります）</param>
    /// <param name="height">矩形高さ（Box 時に使用、> 0 である必要があります）</param>
    /// <param name="offsetX">X 軸局所オフセット</param>
    /// <param name="offsetY">Y 軸局所オフセット</param>
    /// <returns>成功時は <see cref="CollisionShape"/>、失敗時は <see cref="SimpleError"/></returns>
    public static Result<CollisionShape> Create(
        string? shapeKind,
        float radius = 0.0f,
        float width = 0.0f,
        float height = 0.0f,
        float offsetX = 0.0f,
        float offsetY = 0.0f)
    {
        if (string.IsNullOrWhiteSpace(shapeKind))
        {
            return Result<CollisionShape>.Err(SimpleError.NotValid("衝突形状種別 (shapeKind) が null または空白です。"));
        }

        if (string.Equals(shapeKind, "Circle", StringComparison.OrdinalIgnoreCase))
        {
            if (radius <= 0.0f)
            {
                return Result<CollisionShape>.Err(SimpleError.ArgumentOutOfRange($"Circle の半径 (radius) は 0 より大きい正数である必要があります (指定値: {radius})。"));
            }

            return Result<CollisionShape>.Ok(new Circle(radius, offsetX, offsetY));
        }

        if (string.Equals(shapeKind, "Box", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(shapeKind, "Rectangle", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(shapeKind, "AABB", StringComparison.OrdinalIgnoreCase))
        {
            if (width <= 0.0f || height <= 0.0f)
            {
                return Result<CollisionShape>.Err(SimpleError.ArgumentOutOfRange($"Box の幅と高さ (width, height) は 0 より大きい正数である必要があります (指定値: {width}x{height})。"));
            }

            return Result<CollisionShape>.Ok(new Box(width, height, offsetX, offsetY));
        }

        if (string.Equals(shapeKind, "Point", StringComparison.OrdinalIgnoreCase))
        {
            return Result<CollisionShape>.Ok(new Point(offsetX, offsetY));
        }

        return Result<CollisionShape>.Err(SimpleError.NotFound($"未対応の衝突形状種別です: '{shapeKind}' (有効な値: 'Circle', 'Box', 'Point')。"));
    }

    /// <summary>
    /// 形状文字列から衝突形状インスタンスを生成します。
    /// 生成に失敗した場合は <see cref="SimpleError"/> の説明をログ出力し、安全なフォールバック形状を返します。
    /// </summary>
    /// <param name="shapeKind">形状種別文字列</param>
    /// <param name="radius">円形半径</param>
    /// <param name="width">矩形幅</param>
    /// <param name="height">矩形高さ</param>
    /// <param name="offsetX">X 軸局所オフセット</param>
    /// <param name="offsetY">Y 軸局所オフセット</param>
    /// <returns>構築された衝突形状バリアント</returns>
    public static CollisionShape CreateWithFallback(
        string? shapeKind,
        float radius = 0.0f,
        float width = 0.0f,
        float height = 0.0f,
        float offsetX = 0.0f,
        float offsetY = 0.0f)
    {
        Result<CollisionShape> result = Create(shapeKind, radius, width, height, offsetX, offsetY);
        if (result.IsOk && result.Value is not null)
        {
            return result.Value;
        }

        Console.WriteLine($"[警告] 衝突形状の生成に失敗しました ({result.Error?.Type}): {result.Error?.Description}。安全なフォールバック (Circle 8px) を使用します。");
        return new Circle(radius > 0.0f ? radius : 8.0f, offsetX, offsetY);
    }

    /// <summary>
    /// 2 つの Transform と CollisionShape 間の幾何学的交差（衝突）判定をパターンマッチングで網羅的に判定します。
    /// </summary>
    /// <param name="t1">第 1 エンティティの位置</param>
    /// <param name="s1">第 1 エンティティの形状バリアント</param>
    /// <param name="t2">第 2 エンティティの位置</param>
    /// <param name="s2">第 2 エンティティの形状バリアント</param>
    /// <returns>交差していれば true、それ以外は false</returns>
    public static bool Intersects(in Transform t1, CollisionShape s1, in Transform t2, CollisionShape s2)
    {
        Vector2D p1 = new(t1.X + s1.OffsetX, t1.Y + s1.OffsetY);
        Vector2D p2 = new(t2.X + s2.OffsetX, t2.Y + s2.OffsetY);

        return (s1, s2) switch
        {
            (Circle c1, Circle c2) =>
                PhysicsService.Intersects(new Circle2D(p1, c1.Radius), new Circle2D(p2, c2.Radius)),

            (Box b1, Box b2) =>
                PhysicsService.Intersects(new Box2D(p1, new Vector2D(b1.Width, b1.Height)), new Box2D(p2, new Vector2D(b2.Width, b2.Height))),

            (Circle c, Box b) =>
                PhysicsService.Intersects(new Circle2D(p1, c.Radius), new Box2D(p2, new Vector2D(b.Width, b.Height))),

            (Box b, Circle c) =>
                PhysicsService.Intersects(new Box2D(p1, new Vector2D(b.Width, b.Height)), new Circle2D(p2, c.Radius)),

            (Point, Circle c) =>
                PhysicsService.Intersects(p1, new Circle2D(p2, c.Radius)),

            (Circle c, Point) =>
                PhysicsService.Intersects(p2, new Circle2D(p1, c.Radius)),

            (Point, Box b) =>
                PhysicsService.Intersects(p1, new Box2D(p2, new Vector2D(b.Width, b.Height))),

            (Box b, Point) =>
                PhysicsService.Intersects(p2, new Box2D(p1, new Vector2D(b.Width, b.Height))),

            (Point, Point) =>
                MathF.Abs(p1.X - p2.X) < 0.001f && MathF.Abs(p1.Y - p2.Y) < 0.001f,

            _ => false
        };
    }
}
