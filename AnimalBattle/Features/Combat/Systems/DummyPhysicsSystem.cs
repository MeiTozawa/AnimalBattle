using AnimalBattle.Combat.Components;
using AnimalBattle.Maps;

namespace AnimalBattle.Combat.Systems;

/// <summary>
///     木桩エンティティの物理計算用コンテキスト。
/// </summary>
internal sealed class DummyPhysicsContext : IDisposable
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
///     手触り検証用木桩のキネマティック運動物理システム。
///     重力、ノックバック減衰、足場衝突および境界Clampを処理し、受撃時の浮空・着地を再現します。
/// </summary>
public static class DummyPhysicsSystem
{
    /// <summary>
    ///     木桩物理更新システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddDummyPhysicsSystem(this World world)
    {
        var tileMapQuery = world.QueryBuilder<TileMap>().Build();
        world.Set(new DummyPhysicsContext
        {
            TileMapQuery = tileMapQuery
        });

        world.System<Transform, Velocity, CollisionBox>("DummyPhysicsSystem")
            .With<DummyTag>()
            .Without<DeadTag>()
            .Without<HitstopState>()
            .Kind(Ecs.OnUpdate)
            .Iter(ExecuteDummyPhysics);

        return world;
    }

    private static void ExecuteDummyPhysics(
        Iter it,
        Field<Transform> transforms,
        Field<Velocity> velocities,
        Field<CollisionBox> collisionBoxes)
    {
        var count = it.Count();
        if (count == 0) return;

        var dt = it.DeltaTime();
        if (dt <= 0.0f) return;

        var world = it.World();
        var ctx = world.Get<DummyPhysicsContext>();
        ref readonly var arenaCfg = ref world.Get<ArenaConfig>();
        var dummyCfg = world.Get<DummyConfig>();
        var gravity = dummyCfg.Gravity;
        var maxFallSpeed = dummyCfg.MaxFallSpeed;
        var knockbackDecel = dummyCfg.KnockbackDeceleration;
        var groundFriction = dummyCfg.GroundFrictionDeceleration;
        var airDrag = dummyCfg.AirDragDeceleration;

        ctx.CachedTileMap = null;
        ctx.TileMapQuery.Iter(static (Iter tileIt, Field<TileMap> maps) =>
        {
            if (tileIt.Any())
            {
                tileIt.World().Get<DummyPhysicsContext>().CachedTileMap = maps[0];
            }
        });
        var tileMap = ctx.CachedTileMap;
        ctx.CachedTileMap = null;

        for (var i = 0; i < count; i++)
        {
            ref var trans = ref transforms[i];
            ref var vel = ref velocities[i];
            ref var col = ref collisionBoxes[i];

            // 重力の適用（空中にいる場合）
            if (!col.IsGrounded)
            {
                var curVy = vel.Value.Y;
                PhysicsService.ApplyGravity(ref curVy, gravity, maxFallSpeed, dt);
                vel.Value = new Vec2f(vel.Value.X, curVy);
            }

            // ノックバック外力の減衰
            if (vel.Knockback != Vec2f.Zero)
            {
                var kx = PhysicsService.MoveTowards(vel.Knockback.X, 0.0f, knockbackDecel * dt);
                var ky = PhysicsService.MoveTowards(vel.Knockback.Y, 0.0f, knockbackDecel * dt);
                vel.Knockback = new Vec2f(kx, ky);
            }

            // 目標座標の算出と画面左右境界の制限
            var totalVel = vel.Value + vel.Knockback;
            var nextPos = trans.Position + totalVel * dt;
            nextPos = nextPos with { X = PhysicsService.Clamp(nextPos.X, arenaCfg.BoundaryMinX, arenaCfg.BoundaryMaxX) };

            var currentY = trans.Position.Y;
            Box2D entityBox = new(nextPos, col.Size);

            // プラットフォーム足場との片道衝突判定
            var hitPlatform = false;
            var platformGroundY = float.MaxValue;
            var platformVy = 0.0f;

            if (tileMap != null)
            {
                var sweepTop = MathF.Min(currentY, nextPos.Y) - col.Size.Y * 0.5f;
                var sweepBottom = MathF.Max(currentY, nextPos.Y) + col.Size.Y * 0.5f;
                Box2D sweepQueryBox = new(new Vec2f(nextPos.X, (sweepTop + sweepBottom) * 0.5f), new Vec2f(col.Size.X, sweepBottom - sweepTop));

                ctx.CandidatePlatforms.Clear();
                tileMap.SpatialHash.Query(sweepQueryBox, ctx.CandidatePlatforms);

                foreach (var platformIdx in ctx.CandidatePlatforms)
                {
                    if (tileMap.SpatialHash.TryGetBox(platformIdx, out var platformBox))
                    {
                        if (PhysicsService.CheckPlatformGround(currentY, nextPos.Y, entityBox, platformBox, out var groundY, out var vy))
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

            col.IsGrounded = hitPlatform;
            var adjustedPosY = hitPlatform ? platformGroundY : nextPos.Y;
            trans.Position = new Vec2f(nextPos.X, adjustedPosY);

            // 接地時と空中時における水平速度の減衰（地上摩擦 vs 空中抵抗）
            vel.Value = hitPlatform ?
                new Vec2f(PhysicsService.MoveTowards(vel.Value.X, 0.0f, groundFriction * dt), platformVy) :
                new Vec2f(PhysicsService.MoveTowards(vel.Value.X, 0.0f, airDrag * dt), vel.Value.Y);
        }
    }
}
