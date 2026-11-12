namespace AnimalBattle.Player.Systems;

using System;
using System.Collections.Generic;
using AnimalBattle.Configs;
using AnimalBattle.Events;
using AnimalBattle.Maps;
using AnimalBattle.Player.Components;
using AnimalBattle.Presentation.Rendering.Components;

/// <summary>
///     World 単位で独立して保持されるキネマティック運動物理システム用コンテキスト。
/// </summary>
internal sealed class MovementSystemContext : IDisposable
{
    public required Query<TileMap> TileMapQuery { get; init; }
    public TileMap? CachedTileMap;
    public readonly List<int> CandidatePlatforms = new(16);

    public void Dispose()
    {
        TileMapQuery.Dispose();
    }
}

/// <summary>
///     汎用キネマティック運動物理システム。
///     外力ノックバック減衰、TileMap との片道足場衝突、接地判定および画面境界 Clamp を統一的に処理します。
/// </summary>
public static class MovementSystem
{
    /// <summary>
    ///     キネマティック運動物理システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World" /></returns>
    public static World AddMovementSystem(this World world)
    {
        var tileMapQuery = world.QueryBuilder<TileMap>().Build();
        world.SetDisposable(new MovementSystemContext
        {
            TileMapQuery = tileMapQuery
        });

        world.System<Transform, Velocity, CollisionBox, LocomotionCapability, JumpCapability, Sprite, SpringScale>("MovementSystem")
           .Kind(Ecs.OnUpdate)
           .Without<DeadTag>()
           .Without<HitstopState>()
           .Iter(ExecuteMovement);

        return world;
    }

    private static void ExecuteMovement(
        Iter it,
        Field<Transform> transforms,
        Field<Velocity> velocities,
        Field<CollisionBox> collisionBoxes,
        Field<LocomotionCapability> locos,
        Field<JumpCapability> jumps,
        Field<Sprite> sprites,
        Field<SpringScale> springs)
    {
        var count = it.Count();
        if (count == 0) return;

        var dt = it.DeltaTime();
        var world = it.World();
        var ctx = world.Get<MovementSystemContext>();
        ref readonly var cfg = ref world.Get<ArenaConfig>();

        ctx.CachedTileMap = null;
        ctx.TileMapQuery.Iter(ExtractTileMap);
        var tileMap = ctx.CachedTileMap;
        ctx.CachedTileMap = null;

        for (var i = 0; i < count; i++)
        {
            ref var trans = ref transforms[i];
            ref var vel = ref velocities[i];
            ref var col = ref collisionBoxes[i];
            ref readonly var loco = ref locos[i];
            ref var jump = ref jumps[i];
            ref var sprite = ref sprites[i];
            ref var spring = ref springs[i];

            ExecuteKinematics(ref trans, ref vel, ref col, in loco, ref jump, ref sprite, ref spring, dt, tileMap, cfg, ctx.CandidatePlatforms);
        }
    }

    private static void ExtractTileMap(Iter it, Field<TileMap> tileMaps)
    {
        if (it.Count() == 0) return;
        it.World().Get<MovementSystemContext>().CachedTileMap = tileMaps[0];
    }

    private static void ExecuteKinematics(
        ref Transform trans,
        ref Velocity vel,
        ref CollisionBox col,
        in LocomotionCapability loco,
        ref JumpCapability jump,
        ref Sprite sprite,
        ref SpringScale spring,
        float dt,
        TileMap? tileMap,
        ArenaConfig arenaCfg,
        List<int> candidatePlatforms)
    {
        var wasGrounded = col.IsGrounded;

        // 外力ノックバック速度の減衰処理
        if (vel.Knockback != Vec2f.Zero)
        {
            var decay = col.IsGrounded ? loco.GroundDeceleration : loco.AirDeceleration;
            if (decay > 0.0f)
            {
                var kx = PhysicsService.MoveTowards(vel.Knockback.X, 0.0f, decay * dt);
                var ky = PhysicsService.MoveTowards(vel.Knockback.Y, 0.0f, decay * dt);
                vel.Knockback = new Vec2f(kx, ky);
            }
            else
            {
                vel.Knockback = Vec2f.Zero;
            }
        }

        // 次フレーム目標座標の算出：自主移動速度と外力ノックバックの合算
        var totalVel = vel.Value + vel.Knockback;
        var nextPos = trans.Position + totalVel * dt;

        if (arenaCfg != null)
        {
            nextPos = nextPos with { X = PhysicsService.Clamp(nextPos.X, arenaCfg.BoundaryMinX, arenaCfg.BoundaryMaxX) };
        }

        var currentY = trans.Position.Y;
        Box2D entityBox = new(nextPos, col.Size);

        // 足場プラットフォームとの片道衝突判定
        var hitPlatform = false;
        var platformGroundY = float.MaxValue;
        var platformVy = 0.0f;

        if (tileMap != null)
        {
            var sweepTop = MathF.Min(currentY, nextPos.Y) - col.Size.Y * 0.5f;
            var sweepBottom = MathF.Max(currentY, nextPos.Y) + col.Size.Y * 0.5f;
            var sweepCenter = new Vec2f(nextPos.X, (sweepTop + sweepBottom) * 0.5f);
            var sweepSize = new Vec2f(col.Size.X, sweepBottom - sweepTop);
            Box2D sweepQueryBox = new(sweepCenter, sweepSize);

            candidatePlatforms.Clear();
            tileMap.SpatialHash.Query(sweepQueryBox, candidatePlatforms);

            foreach (var t in candidatePlatforms)
            {
                if (tileMap.SpatialHash.TryGetBox(t, out var platformBox))
                {
                    if (PhysicsService.CheckPlatformGround(
                        currentY,
                        nextPos.Y,
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
                }
            }
        }

        var isGrounded = hitPlatform;
        var adjustedPosY = hitPlatform ? platformGroundY : nextPos.Y;
        var adjustedVy = hitPlatform ? platformVy : vel.Value.Y;

        if (arenaCfg != null)
        {
            var floorLimitY = arenaCfg.FloorLimitY - col.Size.Y / 2.0f;
            if (adjustedPosY >= floorLimitY)
            {
                isGrounded = true;
                adjustedPosY = floorLimitY;
                adjustedVy = 0.0f;
            }
        }

        // 接地瞬間時の押し潰し演出：落下速度が設定閾値以上の場合のみ適用
        if (!wasGrounded && isGrounded)
        {
            jump.RemainingJumps = jump.MaxJumps;
            jump.CoyoteTimer = 0.0f;

            if (vel.Value.Y >= jump.LandSquashVelocityThreshold)
            {
                EventBus.Publish(new PlayerLandedEvent());
                spring.Current = jump.LandSquashScale;
                spring.Velocity = Vec2f.Zero;
                spring.SkipNextUpdate = true;
                sprite.Scale = spring.Current;
                if (sprite.SrcRect.Height > 0)
                {
                    var halfH = sprite.SrcRect.Height * trans.Scale.Y * 0.5f;
                    sprite.SpringOffsetY = halfH * (1.0f - spring.Current.Y);
                }
            }
        }

        if (isGrounded)
        {
            jump.RemainingJumps = jump.MaxJumps;
            if (vel.Knockback.Y > 0.0f)
            {
                vel.Knockback = new Vec2f(vel.Knockback.X, 0.0f);
            }
        }

        trans.Position = new Vec2f(nextPos.X, adjustedPosY);
        vel.Value = new Vec2f(vel.Value.X, adjustedVy);
        col.IsGrounded = isGrounded;
    }
}
