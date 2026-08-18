using System;
using AnimalBattle.Bindings;

namespace AnimalBattle.Core.Physics;

/// <summary>
/// C++ Native 物理エンジン (<see cref="NativeBindings"/>) への高効率なアクセスを提供する静的物理サービス。
/// </summary>
public static unsafe class PhysicsService
{
    // --- 2D 幾何形状交差判定 ---

    /// <summary>
    /// 2 つの円形衝突形状の交差を判定します。
    /// </summary>
    public static bool Intersects(Circle2D c1, Circle2D c2)
    {
        return NativeBindings.Physics_IntersectsCircleCircle(c1, c2);
    }

    /// <summary>
    /// 2 つの軸平行矩形 (AABB) 衝突形状の交差を判定します。
    /// </summary>
    public static bool Intersects(Box2D b1, Box2D b2)
    {
        return NativeBindings.Physics_IntersectsBoxBox(b1, b2);
    }

    /// <summary>
    /// 円形と軸平行矩形 (AABB) 衝突形状の交差を判定します。
    /// </summary>
    public static bool Intersects(Circle2D circle, Box2D box)
    {
        return NativeBindings.Physics_IntersectsCircleBox(circle, box);
    }

    /// <summary>
    /// 軸平行矩形 (AABB) と円形衝突形状の交差を判定します。
    /// </summary>
    public static bool Intersects(Box2D box, Circle2D circle)
    {
        return NativeBindings.Physics_IntersectsCircleBox(circle, box);
    }

    /// <summary>
    /// 点と円形衝突形状の交差を判定します。
    /// </summary>
    public static bool Intersects(Vector2D point, Circle2D circle)
    {
        return NativeBindings.Physics_IntersectsPointCircle(point, circle);
    }

    /// <summary>
    /// 点と軸平行矩形 (AABB) 衝突形状の交差を判定します。
    /// </summary>
    public static bool Intersects(Vector2D point, Box2D box)
    {
        return NativeBindings.Physics_IntersectsPointBox(point, box);
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
    public static Vector2D IntegrateVelocity(Vector2D position, Vector2D velocity, float deltaTime = 1.0f)
    {
        return NativeBindings.Physics_IntegrateVelocity(position, velocity, deltaTime);
    }

    /// <summary>
    /// 垂直速度へ重力を加算し、最大落下速度でクランプします。
    /// </summary>
    public static void ApplyGravity(ref float vy, float gravity, float maxFallSpeed, float deltaTime = 1.0f)
    {
        fixed (float* pVy = &vy)
        {
            NativeBindings.Physics_ApplyGravity(pVy, gravity, maxFallSpeed, deltaTime);
        }
    }

    /// <summary>
    /// 2 点間のユークリッド距離を計算します。
    /// </summary>
    public static float CalculateDistance(Vector2D a, Vector2D b)
    {
        return NativeBindings.Physics_CalculateDistance(a, b);
    }

    /// <summary>
    /// 2 点間のラジアン角度を計算します。
    /// </summary>
    public static float CalculateAngle(Vector2D from, Vector2D to)
    {
        return NativeBindings.Physics_CalculateAngle(from, to);
    }

    /// <summary>
    /// 角度と移動速度から速度ベクトル (Vector2D) を算出します。
    /// </summary>
    public static Vector2D DirectionToVector(float angleRad, float speed)
    {
        return NativeBindings.Physics_DirectionToVector(angleRad, speed);
    }

    /// <summary>
    /// 値を指定した最小値・最大値の範囲内にクランプします。
    /// </summary>
    public static float Clamp(float value, float minVal, float maxVal)
    {
        return NativeBindings.Physics_Clamp(value, minVal, maxVal);
    }

    /// <summary>
    /// 2 値間を線形補間 (Lerp) します。
    /// </summary>
    public static float Lerp(float a, float b, float t)
    {
        return NativeBindings.Physics_Lerp(a, b, t);
    }
}
