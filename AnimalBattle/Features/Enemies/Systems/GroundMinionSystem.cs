using AnimalBattle.Maps;
using AnimalBattle.Maps.Navigation;

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
/// 地面近接追撃怪の移動および A* 経路探索・物理処理システム。
/// プラットフォームナビゲーショングラフを用いてプレイヤーを地上追跡し、重力、跳躍、ノックバック減衰、足場衝突を処理します。
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

        world.System<Transform, Velocity, CollisionBox, Sprite, GroundMinionComponent>("GroundMinionSystem")
            .With<GroundMinionTag>()
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
        Field<GroundMinionComponent> minions)
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
            ref var minion = ref minions[i];

            if (minion.RepathCooldown > 0.0f)
                minion.RepathCooldown -= dt;
            if (minion.JumpCooldown > 0.0f)
                minion.JumpCooldown -= dt;

            // プレイヤーへの A* 経路探索と移動制御
            if (playerPos.HasValue)
            {
                var pPos = playerPos.Value;
                var minionFootY = trans.Position.Y + col.Size.Y * 0.5f;
                var playerFootY = pPos.Y + 32.0f;

                var navGraph = tileMap?.NavGraph;
                if (navGraph is not null && navGraph.Nodes.Count > 0)
                {
                    var playerMovedDistSq = (pPos - minion.LastPlayerPos).LengthSquared();
                    if (minion.CachedPlan == null || minion.RepathCooldown <= 0.0f || playerMovedDistSq > 64.0f * 64.0f)
                    {
                        minion.CachedPlan = navGraph.FindPath(
                            trans.Position.X,
                            minionFootY,
                            pPos.X,
                            playerFootY,
                            col.IsGrounded,
                            gCfg.MoveSpeed,
                            minion.ActiveLink);
                        minion.LastPlayerPos = pPos;
                        minion.RepathCooldown = 0.25f;
                    }
                }

                var plan = minion.CachedPlan;

                // 跳躍の実行判定
                if (plan is { Status: NavStatus.ExecuteJump } jumpPlan && minion.JumpCooldown <= 0.0f && col.IsGrounded)
                {
                    var jumpVy = jumpPlan.RequiredVy != 0.0f ? jumpPlan.RequiredVy : -gCfg.JumpForce;
                    var jumpVx = jumpPlan.RequiredVx;
                    vel.Value = new Vec2f(jumpVx, jumpVy);
                    col.IsGrounded = false;
                    minion.JumpCooldown = gCfg.JumpCooldown;
                    minion.ActiveLink = jumpPlan.NextLink;
                    minion.CachedPlan = null;
                }
                else
                {
                    var targetVx = 0.0f;
                    if (plan is { } validPlan)
                    {
                        targetVx = validPlan.RequiredVx;
                        minion.ActiveLink = validPlan.NextLink;
                    }
                    else
                    {
                        // ナビゲーションプラン未生成時の直接追跡フォールバック
                        var diffX = pPos.X - trans.Position.X;
                        if (MathF.Abs(diffX) > 8.0f)
                        {
                            targetVx = MathF.Sign(diffX) * gCfg.MoveSpeed;
                        }
                    }

                    vel.Value = new Vec2f(targetVx, vel.Value.Y);
                }

                if (MathF.Abs(vel.Value.X) > 1.0f)
                {
                    sprite.FlipX = vel.Value.X < 0.0f;
                }
            }
            else
            {
                vel.Value = new Vec2f(0.0f, vel.Value.Y);
                minion.CachedPlan = null;
                minion.ActiveLink = null;
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
                minion.ActiveLink = null;
            }
        }
    }
}
