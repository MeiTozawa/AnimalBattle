using AnimalBattle.Maps;

namespace AnimalBattle.Enemies.Systems;

/// <summary>
/// 地面近接追撃怪の計算用コンテキスト。
/// </summary>
internal sealed class GroundMinionContext : IDisposable
{
    public required Query<TileMap> TileMapQuery { get; init; }
    public required Query<Transform> PlayerQuery { get; init; }
    public TileMap? CachedTileMap;
    public Vec2f? CachedPlayerPos;
    public readonly List<int> CandidatePlatforms = new(16);

    public void Dispose()
    {
        TileMapQuery.Dispose();
        PlayerQuery.Dispose();
    }
}

/// <summary>
/// 地面近接追撃怪の移動および物理処理システム。
/// プレイヤーを地上追跡し、重力、ノックバック減衰、足場衝突を処理します。
/// </summary>
public static class GroundMinionSystem
{
    /// <summary>
    /// 地面近接追撃怪システムを Flecs ワールドへ登録します。
    /// </summary>
    public static World AddGroundMinionSystem(this World world)
    {
        var tileMapQuery = world.QueryBuilder<TileMap>().Build();
        var playerQuery = world.QueryBuilder<Transform>()
            .With<PlayerTag>()
            .Without<DeadTag>()
            .Build();

        world.Set(new GroundMinionContext
        {
            TileMapQuery = tileMapQuery,
            PlayerQuery = playerQuery
        });

        world.System<Transform, Velocity, CollisionBox, Sprite, GroundMinion>("GroundMinionSystem")
            .With<MinionTag>()
            .Without<DeadTag>()
            .Without<HitstopState>()
            .Kind(Ecs.OnUpdate)
            .Iter(ExecuteGroundMinion);

        return world;
    }

    private static void ExecuteGroundMinion(
        Iter it,
        Field<Transform> transforms,
        Field<Velocity> velocities,
        Field<CollisionBox> collisionBoxes,
        Field<Sprite> sprites,
        Field<GroundMinion> minions)
    {
        var count = it.Count();
        if (count == 0) return;

        var dt = it.DeltaTime();
        if (dt <= 0.0f) return;

        var world = it.World();
        var ctx = world.Get<GroundMinionContext>();
        ref readonly var arenaCfg = ref world.Get<ArenaConfig>();
        var minionCfg = world.Get<MinionConfig>();
        var gCfg = minionCfg.GroundMinion;

        // プレイヤー座標のキャッシュ取得
        ctx.CachedPlayerPos = null;
        ctx.PlayerQuery.Iter(static (Iter pIt, Field<Transform> pTrans) =>
        {
            if (pIt.Any())
            {
                pIt.World().Get<GroundMinionContext>().CachedPlayerPos = pTrans[0].Position;
            }
        });
        var playerPos = ctx.CachedPlayerPos;

        // タイルマップのキャッシュ取得
        ctx.CachedTileMap = null;
        ctx.TileMapQuery.Iter(static (Iter tIt, Field<TileMap> maps) =>
        {
            if (tIt.Any())
            {
                tIt.World().Get<GroundMinionContext>().CachedTileMap = maps[0];
            }
        });
        var tileMap = ctx.CachedTileMap;

        for (var i = 0; i < count; i++)
        {
            ref var trans = ref transforms[i];
            ref var vel = ref velocities[i];
            ref var col = ref collisionBoxes[i];
            ref var sprite = ref sprites[i];

            // プレイヤーへの水平追跡
            if (playerPos.HasValue)
            {
                var diffX = playerPos.Value.X - trans.Position.X;
                var dirX = MathF.Sign(diffX);
                vel.Value = new Vec2f(dirX * gCfg.MoveSpeed, vel.Value.Y);
                if (dirX != 0)
                {
                    sprite.FlipX = dirX < 0;
                }
            }
            else
            {
                vel.Value = new Vec2f(0.0f, vel.Value.Y);
            }

            // 空中にいる場合は重力を適用
            if (!col.IsGrounded)
            {
                var curVy = vel.Value.Y;
                PhysicsService.ApplyGravity(ref curVy, 1200.0f, 1200.0f, dt);
                vel.Value = new Vec2f(vel.Value.X, curVy);
            }

            // ノックバック外力の減衰
            if (vel.Knockback != Vec2f.Zero)
            {
                var kx = PhysicsService.MoveTowards(vel.Knockback.X, 0.0f, 900.0f * dt);
                var ky = PhysicsService.MoveTowards(vel.Knockback.Y, 0.0f, 900.0f * dt);
                vel.Knockback = new Vec2f(kx, ky);
            }

            // 移動計算と境界制限
            var totalVel = vel.Value + vel.Knockback;
            var nextPos = trans.Position + totalVel * dt;
            nextPos = nextPos with { X = PhysicsService.Clamp(nextPos.X, arenaCfg.BoundaryMinX, arenaCfg.BoundaryMaxX) };

            var currentY = trans.Position.Y;
            Box2D entityBox = new(nextPos, col.Size);

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

            if (hitPlatform)
            {
                vel.Value = new Vec2f(vel.Value.X, platformVy);
            }
        }
    }
}
