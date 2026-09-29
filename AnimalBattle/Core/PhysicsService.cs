using System;
using System.Runtime.CompilerServices;
using AnimalBattle.Bindings;
using AnimalBattle.Components;

namespace AnimalBattle.Core;

/// <summary>
/// C++ Native 物理エンジン (<see cref="NativeBindings"/>) への高効率なアクセスを提供する静的物理サービス。
/// </summary>
public static unsafe class PhysicsService
{
    // --- 2D 幾何形状交差判定 ---

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
        Vec2f p1 = new(t1.X + s1.OffsetX, t1.Y + s1.OffsetY);
        Vec2f p2 = new(t2.X + s2.OffsetX, t2.Y + s2.OffsetY);

        return (s1.Type, s2.Type) switch
        {
            (CollisionShapeType.Circle, CollisionShapeType.Circle) =>
                Intersects(new Circle2D(p1, s1.Radius), new Circle2D(p2, s2.Radius)),

            (CollisionShapeType.Box, CollisionShapeType.Box) =>
                Intersects(new Box2D(p1, new Vec2f(s1.Width, s1.Height)), new Box2D(p2, new Vec2f(s2.Width, s2.Height))),

            (CollisionShapeType.Circle, CollisionShapeType.Box) =>
                Intersects(new Circle2D(p1, s1.Radius), new Box2D(p2, new Vec2f(s2.Width, s2.Height))),

            (CollisionShapeType.Box, CollisionShapeType.Circle) =>
                Intersects(new Box2D(p1, new Vec2f(s1.Width, s1.Height)), new Circle2D(p2, s2.Radius)),

            (CollisionShapeType.Point, CollisionShapeType.Circle) =>
                Intersects(p1, new Circle2D(p2, s2.Radius)),

            (CollisionShapeType.Circle, CollisionShapeType.Point) =>
                Intersects(p2, new Circle2D(p1, s1.Radius)),

            (CollisionShapeType.Point, CollisionShapeType.Box) =>
                Intersects(p1, new Box2D(p2, new Vec2f(s2.Width, s2.Height))),

            (CollisionShapeType.Box, CollisionShapeType.Point) =>
                Intersects(p2, new Box2D(p1, new Vec2f(s1.Width, s1.Height))),

            (CollisionShapeType.Point, CollisionShapeType.Point) =>
                MathF.Abs(p1.X - p2.X) < 0.001f && MathF.Abs(p1.Y - p2.Y) < 0.001f,

            _ => false
        };
    }

    /// <summary>
    /// 2 つの円形衝突形状の交差を判定します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Intersects(Circle2D c1, Circle2D c2)
    {
        Vec2f diff = c1.Center - c2.Center;
        float combinedRadius = c1.Radius + c2.Radius;
        return diff.LengthSquared() <= (combinedRadius * combinedRadius);
    }

    /// <summary>
    /// 2 つの軸平行矩形 (AABB) 衝突形状の交差を判定します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Intersects(Box2D b1, Box2D b2)
    {
        Vec2f diff = b1.Center - b2.Center;
        float dx = MathF.Abs(diff.X);
        float dy = MathF.Abs(diff.Y);
        return dx <= (b1.Size.X + b2.Size.X) * 0.5f && dy <= (b1.Size.Y + b2.Size.Y) * 0.5f;
    }

    /// <summary>
    /// 円形と軸平行矩形 (AABB) 衝突形状の交差を判定します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Intersects(Circle2D circle, Box2D box)
    {
        float closestX = Math.Clamp(circle.Center.X, box.Left, box.Right);
        float closestY = Math.Clamp(circle.Center.Y, box.Top, box.Bottom);

        Vec2f diff = new(circle.Center.X - closestX, circle.Center.Y - closestY);
        return diff.LengthSquared() <= (circle.Radius * circle.Radius);
    }

    /// <summary>
    /// 軸平行矩形 (AABB) と円形衝突形状の交差を判定します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Intersects(Box2D box, Circle2D circle)
    {
        return Intersects(circle, box);
    }

    /// <summary>
    /// 点と円形衝突形状の交差を判定します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Intersects(Vec2f point, Circle2D circle)
    {
        Vec2f diff = point - circle.Center;
        return diff.LengthSquared() <= (circle.Radius * circle.Radius);
    }

    /// <summary>
    /// 点と軸平行矩形 (AABB) 衝突形状の交差を判定します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Intersects(Vec2f point, Box2D box)
    {
        return point.X >= box.Left && point.X <= box.Right &&
               point.Y >= box.Top && point.Y <= box.Bottom;
    }

    // --- 地形・足場判定 ---

    /// <summary>
    /// ワンウェイ足場（すり抜け床）との着地判定および補正 Y 座標の計算を行います。
    /// </summary>
    public static bool CheckPlatformGround(
        float prevY,
        float nextY,
        Box2D playerBox,
        Box2D platformBox,
        out float adjustedPosY,
        out float adjustedVy)
    {
        float groundY = nextY;
        float vy = 0.0f;
        bool isGrounded = NativeBindings.Physics_CheckPlatformGround(
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
    /// 速度ベクトルを位置座標へ積分加算します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec2f IntegrateVelocity(Vec2f position, Vec2f velocity, float deltaTime = 1.0f)
    {
        return position + velocity * deltaTime;
    }

    /// <summary>
    /// 垂直速度へ重力を加算し、最大落下速度でクランプします。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ApplyGravity(ref float vy, float gravity, float maxFallSpeed, float deltaTime = 1.0f)
    {
        vy += gravity * deltaTime;
        if (vy > maxFallSpeed)
        {
            vy = maxFallSpeed;
        }
    }

    /// <summary>
    /// 2 点間のユークリッド距離を計算します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float CalculateDistance(Vec2f a, Vec2f b)
    {
        return (a - b).Length();
    }

    /// <summary>
    /// 2 点間のラジアン角度を計算します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float CalculateAngle(Vec2f from, Vec2f to)
    {
        return MathF.Atan2(to.Y - from.Y, to.X - from.X);
    }

    /// <summary>
    /// 角度と移動速度から速度ベクトル (Vec2f) を算出します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec2f DirectionToVector(float angleRad, float speed)
    {
        return new(MathF.Cos(angleRad) * speed, MathF.Sin(angleRad) * speed);
    }

    /// <summary>
    /// 値を指定した最小値・最大値の範囲内にクランプします。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Clamp(float value, float minVal, float maxVal)
    {
        return Math.Clamp(value, minVal, maxVal);
    }

    /// <summary>
    /// 2 値間を線形補間 (Lerp) します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
}
