using System.Runtime.CompilerServices;

namespace AnimalBattle.Core;

/// <summary>
///     C++ネイティブ物理エンジンへの高効率なアクセスを提供する静的物理サービスです。
/// </summary>
public static unsafe class PhysicsService {
    // --- 2D 幾何形状交差判定 ---

    /// <summary>
    ///     2つのTransformとCollisionShape間の幾何学的交差判定をパターンマッチングで網羅的に判定します。
    /// </summary>
    /// <param name="t1">第 1 エンティティの位置</param>
    /// <param name="s1">第 1 エンティティの形状バリアント</param>
    /// <param name="t2">第 2 エンティティの位置</param>
    /// <param name="s2">第 2 エンティティの形状バリアント</param>
    /// <returns>交差していれば true、それ以外は false</returns>
    public static bool Intersects(in Transform t1, CollisionShape s1, in Transform t2, CollisionShape s2)
    {
        Vec2f p1 = t1.Position + s1.Offset;
        Vec2f p2 = t2.Position + s2.Offset;

        return (s1.Type, s2.Type) switch {
            (CollisionShapeType.Circle, CollisionShapeType.Circle) =>
                Intersects(new Circle2D(p1, s1.Radius), new Circle2D(p2, s2.Radius)),

            (CollisionShapeType.Box, CollisionShapeType.Box) =>
                Intersects(new Box2D(p1, s1.Size), new Box2D(p2, s2.Size)),

            (CollisionShapeType.Circle, CollisionShapeType.Box) =>
                Intersects(new Circle2D(p1, s1.Radius), new Box2D(p2, s2.Size)),

            (CollisionShapeType.Box, CollisionShapeType.Circle) =>
                Intersects(new Box2D(p1, s1.Size), new Circle2D(p2, s2.Radius)),

            (CollisionShapeType.Point, CollisionShapeType.Circle) =>
                Intersects(p1, new Circle2D(p2, s2.Radius)),

            (CollisionShapeType.Circle, CollisionShapeType.Point) =>
                Intersects(p2, new Circle2D(p1, s1.Radius)),

            (CollisionShapeType.Point, CollisionShapeType.Box) =>
                Intersects(p1, new Box2D(p2, s2.Size)),

            (CollisionShapeType.Box, CollisionShapeType.Point) =>
                Intersects(p2, new Box2D(p1, s1.Size)),

            (CollisionShapeType.Point, CollisionShapeType.Point) =>
                (p1 - p2).LengthSquared() < 0.000001f,

            _ => AssertHelper.FailWithDefault($"[PhysicsService 警告] 未知または未サポートの衝突形状の組み合わせ: ({s1.Type}, {s2.Type})", false)
        };
    }

    /// <summary>
    ///     2 つの円形衝突形状の交差を判定します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Intersects(Circle2D c1, Circle2D c2)
    {
        var diff = c1.Center - c2.Center;
        var combinedRadius = c1.Radius + c2.Radius;
        return diff.LengthSquared() <= combinedRadius * combinedRadius;
    }

    /// <summary>
    ///     2つの軸平行境界ボックスの交差を判定します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Intersects(Box2D b1, Box2D b2)
    {
        var diff = b1.Center - b2.Center;
        var dx = MathF.Abs(diff.X);
        var dy = MathF.Abs(diff.Y);
        return dx <= (b1.Size.X + b2.Size.X) * 0.5f && dy <= (b1.Size.Y + b2.Size.Y) * 0.5f;
    }

    /// <summary>
    ///     円形と軸平行境界ボックスの交差を判定します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Intersects(Circle2D circle, Box2D box)
    {
        var closestX = Math.Clamp(circle.Center.X, box.Left, box.Right);
        var closestY = Math.Clamp(circle.Center.Y, box.Top, box.Bottom);

        Vec2f diff = new(circle.Center.X - closestX, circle.Center.Y - closestY);
        return diff.LengthSquared() <= circle.Radius * circle.Radius;
    }

    /// <summary>
    ///     軸平行境界ボックスと円形衝突形状の交差を判定します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Intersects(Box2D box, Circle2D circle)
    {
        return Intersects(circle, box);
    }

    /// <summary>
    ///     点と円形衝突形状の交差を判定します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Intersects(Vec2f point, Circle2D circle)
    {
        var diff = point - circle.Center;
        return diff.LengthSquared() <= circle.Radius * circle.Radius;
    }

    /// <summary>
    ///     点と軸平行境界ボックスの交差を判定します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Intersects(Vec2f point, Box2D box)
    {
        return point.X >= box.Left && point.X <= box.Right &&
            point.Y >= box.Top && point.Y <= box.Bottom;
    }

    // --- 地形・足場判定 ---

    /// <summary>
    ///     すり抜け床との着地判定および補正Y座標の計算を行います。
    /// </summary>
    public static bool CheckPlatformGround(
        float prevY,
        float nextY,
        Box2D playerBox,
        Box2D platformBox,
        out float adjustedPosY,
        out float adjustedVy)
    {
        var groundY = nextY;
        var vy = 0.0f;
        var isGrounded = NativeBindings.Physics_CheckPlatformGround(
            prevY,
            nextY,
            playerBox,
            platformBox,
            &groundY,
            &vy);

        adjustedPosY = groundY;
        adjustedVy = vy;
        return isGrounded;
    }

    // --- 運動積分およびベクトル数学 ---

    /// <summary>
    ///     垂直速度へ重力を加算し、最大落下速度でクランプします。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ApplyGravity(ref float vy, float gravity, float maxFallSpeed, float deltaTime = 1.0f)
    {
        vy += gravity * deltaTime;
        if (vy > maxFallSpeed) vy = maxFallSpeed;
    }

    /// <summary>
    ///     値を指定した最小値・最大値の範囲内にクランプします。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Clamp(float value, float minVal, float maxVal)
    {
        return Math.Clamp(value, minVal, maxVal);
    }

    /// <summary>
    ///     現在値を目標値へ指定の最大変化量を超えない範囲で線形移動させます。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float MoveTowards(float current, float target, float maxDelta)
    {
        if (MathF.Abs(target - current) <= maxDelta)
            return target;
        return current + MathF.Sign(target - current) * maxDelta;
    }

    /// <summary>
    ///     正弦波と余弦波にプレイヤー追従加重を合成したボスのホバリング調和軌道をインライン計算します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec2f CalculateBossHoverPosition(
        Vec2f currentPos,
        Vec2f basePos,
        Vec2f playerPos,
        float hoverTimer,
        Vec2f hoverSpeed,
        Vec2f hoverAmp,
        float playerPursuitWeight,
        float hoverLerpSpeed,
        float deltaTime)
    {
        var targetX = basePos.X + MathF.Sin(hoverTimer * hoverSpeed.X) * hoverAmp.X +
            (playerPos.X - basePos.X) * playerPursuitWeight;
        var targetY = basePos.Y + MathF.Cos(hoverTimer * hoverSpeed.Y) * hoverAmp.Y;
        var lerpFactor = 1.0f - MathF.Exp(-hoverLerpSpeed * deltaTime);

        var nextX = currentPos.X + (targetX - currentPos.X) * lerpFactor;
        var nextY = currentPos.Y + (targetY - currentPos.Y) * lerpFactor;
        return new Vec2f(nextX, nextY);
    }
}
