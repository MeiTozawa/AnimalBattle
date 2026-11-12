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
        world.SetDisposable(new DummyPhysicsContext
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
            vel.Knockback = PhysicsService.DecayKnockback(vel.Knockback, knockbackDecel, dt);

            // 目標座標の算出と画面左右境界の制限
            var totalVel = vel.Value + vel.Knockback;
            var nextPos = trans.Position + totalVel * dt;
            nextPos = nextPos with { X = PhysicsService.Clamp(nextPos.X, arenaCfg.BoundaryMinX, arenaCfg.BoundaryMaxX) };

            // プラットフォーム足場およびアリーナ床面との片道衝突判定
            var isGrounded = PhysicsService.ResolvePlatformAndFloorCollision(
                trans.Position.Y,
                nextPos,
                col.Size,
                vel.Value.Y,
                tileMap,
                arenaCfg,
                ctx.CandidatePlatforms,
                out var adjustedPosY,
                out var adjustedVy);

            col.IsGrounded = isGrounded;
            trans.Position = new Vec2f(nextPos.X, adjustedPosY);

            // 接地時と空中時における水平速度の減衰（地上摩擦 vs 空中抵抗）
            vel.Value = isGrounded ?
                new Vec2f(PhysicsService.MoveTowards(vel.Value.X, 0.0f, groundFriction * dt), adjustedVy) :
                new Vec2f(PhysicsService.MoveTowards(vel.Value.X, 0.0f, airDrag * dt), vel.Value.Y);
        }
    }
}
