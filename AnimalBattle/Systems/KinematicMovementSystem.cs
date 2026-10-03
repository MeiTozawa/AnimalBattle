using AnimalBattle.Components;
using AnimalBattle.Configs;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     汎用キネマティック運動物理システム。
///     <see cref="CharacterMovementIntent"/> を消費し、重力、跳躍インパルス、足場衝突および画面境界を統一的に処理します。
/// </summary>
public static class KinematicMovementSystem
{
    /// <summary>
    ///     単一エンティティのキネマティック運動物理を 1 フレーム分更新します。
    /// </summary>
    /// <param name="entity">対象 Flecs エンティティ句柄。</param>
    /// <param name="dt">デルタタイム（秒）。</param>
    /// <param name="platformQuery">足場プラットフォーム検索クエリ。</param>
    /// <param name="arenaCfg">アリーナ境界設定（省略可能）。</param>
    /// <param name="effectWorld">エフェクト生成用の Flecs ワールド（省略可能）。</param>
    public static void UpdateEntity(
        Entity entity,
        float dt,
        Query<Transform, CollisionBox> platformQuery,
        ArenaConfig? arenaCfg = null,
        World? effectWorld = null)
    {
        if (!entity.IsAlive() ||
            !entity.Has<Transform>() ||
            !entity.Has<Velocity>() ||
            !entity.Has<CollisionBox>() ||
            !entity.Has<CharacterMovementIntent>() ||
            !entity.Has<JumpCapability>())
            return;

        ref var trans = ref entity.GetMut<Transform>();
        ref var vel = ref entity.GetMut<Velocity>();
        ref var col = ref entity.GetMut<CollisionBox>();
        ref var intent = ref entity.GetMut<CharacterMovementIntent>();
        ref var jump = ref entity.GetMut<JumpCapability>();

        // 1. 水平移動意図の適用
        vel.Vx = intent.MoveX * jump.MoveSpeed;

        // 2. 跳躍意図の適用
        if (intent.JumpTriggered)
        {
            if (col.IsGrounded)
            {
                vel.Vy = jump.FirstJumpVelocity;
                col.IsGrounded = false;
                jump.RemainingJumps = jump.MaxJumps - 1;
            }
            else if (jump.RemainingJumps > 0)
            {
                vel.Vy = jump.SecondJumpVelocity;
                jump.RemainingJumps--;
                if (effectWorld.HasValue)
                {
                    CombatEffectHelper.SpawnJumpRingEffect(effectWorld.Value, trans.X, trans.Y + col.Height / 2.0f);
                }
            }
        }

        // 3. 重力加速度の適用
        var currentVy = vel.Vy;
        PhysicsService.ApplyGravity(ref currentVy, jump.Gravity, jump.MaxFallSpeed, dt);
        vel.Vy = currentVy;

        // 4. 次フレーム目標座標の算出
        var nextX = trans.X + vel.Vx * dt;
        var nextY = trans.Y + vel.Vy * dt;

        if (arenaCfg != null)
        {
            nextX = PhysicsService.Clamp(nextX, arenaCfg.BoundaryMinX, arenaCfg.BoundaryMaxX);
        }

        var currentY = trans.Y;
        Box2D entityBox = new(nextX, nextY, col.Width, col.Height);

        // 5. 足場プラットフォームとの片道衝突判定
        var hitPlatform = false;
        var platformGroundY = float.MaxValue;
        var platformVy = 0.0f;

        platformQuery.Each((ref platTrans, ref platCol) =>
        {
            Box2D platformBox = new(platTrans.X, platTrans.Y, platCol.Width, platCol.Height);
            if (PhysicsService.CheckPlatformGround(
                currentY,
                nextY,
                entityBox,
                platformBox,
                out var groundY,
                out var vy))
            {
                if (!hitPlatform || groundY < platformGroundY)
                {
                    hitPlatform = true;
                    platformGroundY = groundY;
                    platformVy = vy;
                }
            }
        });

        var isGrounded = hitPlatform;
        var adjustedPosY = hitPlatform ? platformGroundY : nextY;
        var adjustedVy = hitPlatform ? platformVy : vel.Vy;

        if (arenaCfg != null)
        {
            var floorLimitY = arenaCfg.FloorLimitY - col.Height / 2.0f;
            if (adjustedPosY >= floorLimitY)
            {
                isGrounded = true;
                adjustedPosY = floorLimitY;
                adjustedVy = 0.0f;
            }
        }

        if (isGrounded)
        {
            jump.RemainingJumps = jump.MaxJumps;
        }

        trans.X = nextX;
        trans.Y = adjustedPosY;
        vel.Vy = adjustedVy;
        col.IsGrounded = isGrounded;
    }
}
