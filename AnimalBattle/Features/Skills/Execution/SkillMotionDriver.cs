using AnimalBattle.Maps.Navigation;

namespace AnimalBattle.Skills;

/// <summary>
/// 固有位移原語（EaseToTarget, LeapSmash 等）の運動軌道を計算するドライバークラス。
/// </summary>
public static class SkillMotionDriver
{
    /// <summary>
    /// 目標座標へのイージング補間座標を計算します。
    /// </summary>
    public static Vec2f EvaluateEaseToTarget(Vec2f startPos, Vec2f targetPos, EaseType easeType, float progress)
    {
        var easeProgress = EasingMath.Evaluate(easeType, Math.Clamp(progress, 0.0f, 1.0f));
        return new Vec2f(
            startPos.X + (targetPos.X - startPos.X) * easeProgress,
            startPos.Y + (targetPos.Y - startPos.Y) * easeProgress);
    }

    /// <summary>
    /// 泰山圧頂（LeapSmash）の 3 段階移動速度を計算します。
    /// </summary>
    public static Vec2f EvaluateLeapSmashVelocity(
        float elapsed,
        float dt,
        Vec2f startPos,
        Vec2f targetPos,
        Vec2f currentPos,
        float flyDuration,
        float peakY,
        float hangDuration,
        float descentSpeed,
        bool isGrounded)
    {
        // 1. 上昇飛翔フェーズ (0.0 ～ FlyDuration)
        if (elapsed < flyDuration)
        {
            var progress = Math.Clamp(elapsed / flyDuration, 0.0f, 1.0f);
            var easeProgress = MathF.Sin(progress * MathF.PI * 0.5f); // Smooth Sine EaseOut

            var desiredX = startPos.X + (targetPos.X - startPos.X) * easeProgress;
            var desiredY = startPos.Y + (peakY - startPos.Y) * easeProgress;

            var vx = dt > 0.0f ? (desiredX - currentPos.X) / dt : 0.0f;
            var vy = dt > 0.0f ? (desiredY - currentPos.Y) / dt : 0.0f;
            return new Vec2f(vx, vy);
        }

        // 2. 最高点懸停フェーズ (FlyDuration ～ FlyDuration + ApexHangDuration)
        if (elapsed < flyDuration + hangDuration)
        {
            var desiredX = targetPos.X;
            var desiredY = peakY;
            var vx = dt > 0.0f ? (desiredX - currentPos.X) / dt : 0.0f;
            var vy = dt > 0.0f ? (desiredY - currentPos.Y) / dt : 0.0f;
            return new Vec2f(vx, vy);
        }

        // 3. 垂直急降下叩きつけフェーズ
        if (isGrounded && elapsed > flyDuration + hangDuration + 0.05f)
        {
            return Vec2f.Zero;
        }

        return new Vec2f(0.0f, descentSpeed);
    }

    /// <summary>
    /// 対象 X 座標およびプレイヤー足元 Y 座標に基づき、最適な着地サーフェス Y 座標を算出します。
    /// </summary>
    public static float CalculateTargetLandingSurfaceY(World world, PlatformNavigationGraph? navGraph, float targetX, float playerFootY)
    {
        var arenaCfg = world.Get<ArenaConfig>();
        var floorY = arenaCfg.FloorLimitY;
        var bestSurfaceY = floorY;

        if (navGraph is not null && navGraph.Nodes.Count > 0)
        {
            const float tolerance = 5.0f;
            foreach (var node in navGraph.Nodes)
            {
                if (node.ContainsX(targetX, 15.0f))
                {
                    if (node.TopY >= playerFootY - tolerance)
                    {
                        if (node.TopY < bestSurfaceY)
                        {
                            bestSurfaceY = node.TopY;
                        }
                    }
                }
            }
        }

        return bestSurfaceY;
    }
}
