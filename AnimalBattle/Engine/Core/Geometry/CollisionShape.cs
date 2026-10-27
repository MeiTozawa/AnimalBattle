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
///     2D 幾何衝突形状を表す完全値型 値型構造体 の判別共用体。
///     GC アロケーションゼロで動作し、Blittable なメモリレイアウトを提供します。
/// </summary>
public readonly record struct CollisionShape {
    /// <summary>形状種別</summary>
    public CollisionShapeType Type { get; init; }

    /// <summary>円の半径ピクセル単位</summary>
    public float Radius { get; init; }

    /// <summary>矩形の寸法 幅と高さ</summary>
    public Vec2f Size { get; init; }

    /// <summary>エンティティ中心座標からの局所オフセット</summary>
    public Vec2f Offset { get; init; }

    /// <summary>
    ///     円形衝突形状ヘルパー構造体：暗黙変換により CollisionShape として代入可能。
    /// </summary>
    public readonly record struct Circle(float Radius, Vec2f Offset = default) {
        public static implicit operator CollisionShape(Circle c)
        {
            return new CollisionShape
                { Type = CollisionShapeType.Circle, Radius = c.Radius, Offset = c.Offset };
        }

        public static implicit operator ErrorOr<CollisionShape>(Circle c)
        {
            return (CollisionShape)c;
        }
    }

    /// <summary>
    ///     矩形 AABB形式 衝突形状ヘルパー構造体：暗黙変換により CollisionShape として代入可能。
    /// </summary>
    public readonly record struct Box(Vec2f Size, Vec2f Offset = default) {
        public static implicit operator CollisionShape(Box b)
        {
            return new CollisionShape {
                Type = CollisionShapeType.Box, Size = b.Size, Offset = b.Offset
            };
        }

        public static implicit operator ErrorOr<CollisionShape>(Box b)
        {
            return (CollisionShape)b;
        }
    }

    /// <summary>
    ///     点 点 衝突形状ヘルパー構造体：暗黙変換により CollisionShape として代入可能。
    /// </summary>
    public readonly record struct Point(Vec2f Offset = default) {
        public static implicit operator CollisionShape(Point p)
        {
            return new CollisionShape { Type = CollisionShapeType.Point, Offset = p.Offset };
        }

        public static implicit operator ErrorOr<CollisionShape>(Point p)
        {
            return (CollisionShape)p;
        }
    }
}
