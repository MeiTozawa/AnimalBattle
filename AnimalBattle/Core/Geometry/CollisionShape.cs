using ErrorOr;

namespace AnimalBattle.Core.Geometry;

/// <summary>
///     2D 幾何衝突形状種別。
/// </summary>
public enum CollisionShapeType : byte {
    Circle = 0,
    Box = 1,
    Point = 2
}

/// <summary>
///     2D 幾何衝突形状を表す完全値型 (readonly record struct) の判別共用体。
///     GC アロケーションゼロで動作し、Blittable なメモリレイアウトを提供します。
/// </summary>
public readonly record struct CollisionShape {
    /// <summary>形状種別</summary>
    public CollisionShapeType Type { get; init; }

    /// <summary>円の半径（ピクセル）</summary>
    public float Radius { get; init; }

    /// <summary>矩形の全幅（ピクセル）</summary>
    public float Width { get; init; }

    /// <summary>矩形の全高（ピクセル）</summary>
    public float Height { get; init; }

    /// <summary>エンティティ中心座標 (Transform.X) からの X 軸局所オフセット</summary>
    public float OffsetX { get; init; }

    /// <summary>エンティティ中心座標 (Transform.Y) からの Y 軸局所オフセット</summary>
    public float OffsetY { get; init; }

    /// <summary>
    ///     円形衝突形状ヘルパー構造体（暗黙変換により CollisionShape として代入可能）。
    /// </summary>
    public readonly record struct Circle(float Radius, float OffsetX = 0.0f, float OffsetY = 0.0f) {
        public static implicit operator CollisionShape(Circle c)
        {
            return new CollisionShape
                { Type = CollisionShapeType.Circle, Radius = c.Radius, OffsetX = c.OffsetX, OffsetY = c.OffsetY };
        }

        public static implicit operator ErrorOr<CollisionShape>(Circle c)
        {
            return (CollisionShape)c;
        }
    }

    /// <summary>
    ///     矩形 (AABB) 衝突形状ヘルパー構造体（暗黙変換により CollisionShape として代入可能）。
    /// </summary>
    public readonly record struct Box(float Width, float Height, float OffsetX = 0.0f, float OffsetY = 0.0f) {
        public static implicit operator CollisionShape(Box b)
        {
            return new CollisionShape {
                Type = CollisionShapeType.Box, Width = b.Width, Height = b.Height, OffsetX = b.OffsetX,
                OffsetY = b.OffsetY
            };
        }

        public static implicit operator ErrorOr<CollisionShape>(Box b)
        {
            return (CollisionShape)b;
        }
    }

    /// <summary>
    ///     点 (Point) 衝突形状ヘルパー構造体（暗黙変換により CollisionShape として代入可能）。
    /// </summary>
    public readonly record struct Point(float OffsetX = 0.0f, float OffsetY = 0.0f) {
        public static implicit operator CollisionShape(Point p)
        {
            return new CollisionShape { Type = CollisionShapeType.Point, OffsetX = p.OffsetX, OffsetY = p.OffsetY };
        }

        public static implicit operator ErrorOr<CollisionShape>(Point p)
        {
            return (CollisionShape)p;
        }
    }
}
