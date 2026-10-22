using AnimalBattle.Core;
using ErrorOr;

namespace AnimalBattle.Core.Geometry;

/// <summary>
///     設定パラメータや文字列データから <see cref="CollisionShape" /> インスタンスを安全に生成・検証する静的ファクトリクラス。
/// </summary>
public static class CollisionShapeFactory {
    /// <summary>
    ///     形状文字列："Circle", "Box", "Point"および寸法・オフセットから衝突形状インスタンスの生成を行います。
    ///     未対応の形状種別や寸法不正時は <see cref="Error" /> を返します。
    /// </summary>
    /// <param name="shapeKind">形状種別文字列："Circle", "Box", "Point"</param>
    /// <param name="radius">円形半径：Circle 時に使用、> 0 である必要があります</param>
    /// <param name="size">矩形サイズ：Box 時に使用、X, Y > 0 である必要があります</param>
    /// <param name="offset">局所オフセット</param>
    /// <returns>成功時は <see cref="CollisionShape" />、失敗時は <see cref="Error" /></returns>
    public static ErrorOr<CollisionShape> Create(
        string? shapeKind,
        float radius = 0.0f,
        Vec2f size = default,
        Vec2f offset = default)
    {
        if (string.IsNullOrWhiteSpace(shapeKind))
            return Error.Validation("CollisionShape.EmptyKind", "衝突形状種別 (shapeKind) が null または空白です。");

        if (string.Equals(shapeKind, "Circle", StringComparison.OrdinalIgnoreCase))
        {
            if (radius <= 0.0f)
                return Error.Validation("CollisionShape.InvalidRadius",
                    $"Circle の半径 (radius) は 0 より大きい正数である必要があります (指定値: {radius})。");

            return new CollisionShape.Circle(radius, offset);
        }

        if (string.Equals(shapeKind, "Box", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(shapeKind, "Rectangle", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(shapeKind, "AABB", StringComparison.OrdinalIgnoreCase))
        {
            if (size.X <= 0.0f || size.Y <= 0.0f)
                return Error.Validation("CollisionShape.InvalidDimensions",
                    $"Box の幅と高さ (size) は 0 より大きい正数である必要があります (指定値: {size.X}x{size.Y})。");

            return new CollisionShape.Box(size, offset);
        }

        if (string.Equals(shapeKind, "Point", StringComparison.OrdinalIgnoreCase))
            return new CollisionShape.Point(offset);

        return Error.NotFound("CollisionShape.NotFound",
            $"未対応の衝突形状種別です: '{shapeKind}' (有効な値: 'Circle', 'Box', 'Point')。");
    }

    /// <summary>
    ///     形状文字列から衝突形状インスタンスを生成します。
    ///     生成に失敗した場合は <see cref="Error" /> の説明をログ出力し、安全なフォールバック形状を返します。
    /// </summary>
    /// <param name="shapeKind">形状種別文字列</param>
    /// <param name="radius">円形半径</param>
    /// <param name="size">矩形サイズ</param>
    /// <param name="offset">局所オフセット</param>
    /// <returns>構築された衝突形状バリアント</returns>
    public static CollisionShape CreateWithFallback(
        string? shapeKind,
        float radius = 0.0f,
        Vec2f size = default,
        Vec2f offset = default)
    {
        var result = Create(shapeKind, radius, size, offset);
        Debug.Assert(!result.IsError, $"[CollisionShapeFactory 警告] 衝突形状の生成に失敗しました ({result.FirstError.Type}): {result.FirstError.Description}");
        if (!result.IsError) return result.Value;

        return new CollisionShape.Circle(8.0f, offset);
    }
}
