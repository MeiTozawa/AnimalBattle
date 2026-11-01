namespace AnimalBattle.Enemies.Systems;

/// <summary>
/// 空中浮遊弾幕怪の計算および描画用コンテキスト。
/// </summary>
internal sealed class FlyingMinionContext : IDisposable
{
    public required Query<Transform> PlayerQuery { get; init; }
    public required Query<Transform, Sprite, CollisionBox, FlyingMinionComponent> MinionQuery { get; init; }

    public void Dispose()
    {
        PlayerQuery.Dispose();
        MinionQuery.Dispose();
    }
}

/// <summary>
/// 空中浮遊弾幕怪の運動制御、物理更新およびスキル駆動システム群。
/// 移動・物理・スキル発動を独立パイプラインへ分離し、
/// CannotMoveTag および CannotAttackTag によるアーキタイプ層での完全ゼロブランチ制御を実現します。
/// </summary>
public static class FlyingMinionSystem
{
    /// <summary>
    /// 空中浮遊弾幕怪システム群を Flecs ワールドへ登録します。
    /// </summary>
    public static World AddFlyingMinionSystem(this World world)
    {
        var playerQuery = world.QueryBuilder<Transform>()
            .With<PlayerTag>()
            .Without<DeadTag>()
            .Build();

        var minionQuery = world.QueryBuilder<Transform, Sprite, CollisionBox, FlyingMinionComponent>()
            .With<FlyingMinionTag>()
            .With<MinionTag>()
            .Without<DeadTag>()
            .Build();

        world.Set(new FlyingMinionContext
        {
            PlayerQuery = playerQuery,
            MinionQuery = minionQuery
        });

        // 1. 能動的浮遊移動システム：CannotMoveTag 保持体はアーキタイプ単位で自動スキップ
        world.System<Transform, Velocity, Sprite, FlyingMinionComponent>("FlyingMinionMovementSystem")
            .With<FlyingMinionTag>()
            .With<MinionTag>()
            .Without<DeadTag>()
            .Without<HitstopState>()
            .Without<CannotMoveTag>()
            .Kind(Ecs.OnUpdate)
            .Iter(ExecuteFlyingMinionMovement);

        // 2. 受動的物理・境界制限システム：静止待機中も含め全浮遊怪のノックバック外力と位置拘束を更新
        world.System<Transform, Velocity>("FlyingMinionPhysicsSystem")
            .With<FlyingMinionTag>()
            .With<MinionTag>()
            .Without<DeadTag>()
            .Without<HitstopState>()
            .Kind(Ecs.OnUpdate)
            .Iter(ExecuteFlyingMinionPhysics);

        // 3. スキル駆動システム：CannotAttackTag 保持体はアーキタイプ単位で自動スキップ
        world.System<Transform, Sprite, FlyingMinionComponent>("FlyingMinionAbilitySystem")
            .With<FlyingMinionTag>()
            .With<MinionTag>()
            .Without<DeadTag>()
            .Without<HitstopState>()
            .Without<CannotAttackTag>()
            .Kind(Ecs.OnUpdate)
            .Iter(ExecuteFlyingMinionAbility);

        // 4. 予兆線描画
        world.System("FlyingMinionTelegraphRenderSystem")
            .Kind(Ecs.OnStore)
            .Iter(ExecuteFlyingMinionTelegraph);

        return world;
    }

    private static void ExecuteFlyingMinionMovement(
        Iter it,
        Field<Transform> transforms,
        Field<Velocity> velocities,
        Field<Sprite> sprites,
        Field<FlyingMinionComponent> minions)
    {
        var count = it.Count();
        if (count == 0) return;

        var dt = it.DeltaTime();
        if (dt <= 0.0f) return;

        var world = it.World();
        var ctx = world.Get<FlyingMinionContext>();
        var minionCfg = world.Get<MinionConfig>();
        var fCfg = minionCfg.FlyingMinion;

        // 生存プレイヤー座標一覧の収集
        var players = new List<Vec2f>(2);
        ctx.PlayerQuery.Iter((Iter pIt, Field<Transform> pTrans) =>
        {
            var pCount = pIt.Count();
            for (var p = 0; p < pCount; p++)
            {
                players.Add(pTrans[p].Position);
            }
        });

        for (var i = 0; i < count; i++)
        {
            ref var trans = ref transforms[i];
            ref var vel = ref velocities[i];
            ref var sprite = ref sprites[i];
            ref var minion = ref minions[i];

            minion.HoverTimer += dt;

            // 最寄りプレイヤーの選定
            Vec2f? nearestPlayerPos = null;
            var minPlayerDistSq = float.MaxValue;
            for (var p = 0; p < players.Count; p++)
            {
                var distSq = (players[p] - trans.Position).LengthSquared();
                if (distSq < minPlayerDistSq)
                {
                    minPlayerDistSq = distSq;
                    nearestPlayerPos = players[p];
                }
            }

            var dirX = 0.0f;
            var targetY = trans.Position.Y;
            if (nearestPlayerPos.HasValue)
            {
                var diffX = nearestPlayerPos.Value.X - trans.Position.X;
                if (MathF.Abs(diffX) > fCfg.HoverDeadZoneX)
                {
                    dirX = MathF.Sign(diffX);
                    if (minion.Asc?.IsCasting != true)
                    {
                        sprite.FlipX = dirX < 0.0f;
                    }
                }

                if (minion.Asc?.IsCasting == true)
                {
                    dirX = 0.0f;
                }

                targetY = nearestPlayerPos.Value.Y - 120.0f;
            }

            var vy = (targetY - trans.Position.Y) * 1.5f + MathF.Cos(minion.HoverTimer * fCfg.HoverFreq) * (fCfg.HoverAmpY * fCfg.HoverFreq);
            vel.Value = new Vec2f(dirX * fCfg.HoverSpeed, vy);
        }
    }

    private static void ExecuteFlyingMinionPhysics(
        Iter it,
        Field<Transform> transforms,
        Field<Velocity> velocities)
    {
        var count = it.Count();
        if (count == 0) return;

        var dt = it.DeltaTime();
        if (dt <= 0.0f) return;

        var world = it.World();
        ref readonly var arenaCfg = ref world.Get<ArenaConfig>();

        for (var i = 0; i < count; i++)
        {
            ref var trans = ref transforms[i];
            ref var vel = ref velocities[i];

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
            nextPos = new Vec2f(
                PhysicsService.Clamp(nextPos.X, arenaCfg.BoundaryMinX, arenaCfg.BoundaryMaxX),
                Math.Clamp(nextPos.Y, 60.0f, arenaCfg.FloorLimitY));
            trans.Position = nextPos;
        }
    }

    private static void ExecuteFlyingMinionAbility(
        Iter it,
        Field<Transform> transforms,
        Field<Sprite> sprites,
        Field<FlyingMinionComponent> minions)
    {
        var count = it.Count();
        if (count == 0) return;

        var dt = it.DeltaTime();
        if (dt <= 0.0f) return;

        var world = it.World();
        var ctx = world.Get<FlyingMinionContext>();
        var minionCfg = world.Get<MinionConfig>();
        var fCfg = minionCfg.FlyingMinion;

        // 生存プレイヤー座標一覧の収集
        var players = new List<Vec2f>(2);
        ctx.PlayerQuery.Iter((Iter pIt, Field<Transform> pTrans) =>
        {
            var pCount = pIt.Count();
            for (var p = 0; p < pCount; p++)
            {
                players.Add(pTrans[p].Position);
            }
        });

        for (var i = 0; i < count; i++)
        {
            ref var minion = ref minions[i];
            if (minion.Asc == null) continue;

            minion.Asc.Update(dt);

            if (minion.Asc.IsCasting || players.Count == 0) continue;

            ref var trans = ref transforms[i];
            ref var sprite = ref sprites[i];

            // 最寄りプレイヤーの選定
            Vec2f? nearestPlayerPos = null;
            var minPlayerDistSq = float.MaxValue;
            for (var p = 0; p < players.Count; p++)
            {
                var distSq = (players[p] - trans.Position).LengthSquared();
                if (distSq < minPlayerDistSq)
                {
                    minPlayerDistSq = distSq;
                    nearestPlayerPos = players[p];
                }
            }

            if (!nearestPlayerPos.HasValue) continue;

            if (!minion.Asc.TryGetAbility(fCfg.SkillTag, out _))
            {
                var patternCfg = world.Get<BossSnakePatternConfig>();
                MinionSpawner.AttachFlyingMinionAbility(world, it.Entity(i), minion.Asc, fCfg, patternCfg);
            }

            var diffX = nearestPlayerPos.Value.X - trans.Position.X;
            var facingDir = MathF.Abs(diffX) > fCfg.HoverDeadZoneX
                ? MathF.Sign(diffX)
                : (!sprite.FlipX ? 1.0f : -1.0f);
            if (minion.Asc.TryActivateAbility(fCfg.SkillTag, facingDir))
            {
                minion.TargetPos = nearestPlayerPos.Value;
            }
        }
    }

    private static void ExecuteFlyingMinionTelegraph(Iter it)
    {
        var world = it.World();
        ref readonly var flow = ref world.Get<GameFlow>();
        if (!flow.IsGameplay) return;

        var ctx = world.Get<FlyingMinionContext>();
        var players = new List<Vec2f>(2);
        ctx.PlayerQuery.Iter((Iter pIt, Field<Transform> pTrans) =>
        {
            var pCount = pIt.Count();
            for (var p = 0; p < pCount; p++)
            {
                players.Add(pTrans[p].Position);
            }
        });

        ref readonly var renderCfg = ref world.Get<RenderConfig>();
        var bossFx = renderCfg.BossEffects;
        var arenaCfg = world.Get<ArenaConfig>();

        ctx.MinionQuery.Iter((Iter mIt, Field<Transform> trans, Field<Sprite> sprites, Field<CollisionBox> cols, Field<FlyingMinionComponent> minions) =>
        {
            var count = mIt.Count();
            for (var i = 0; i < count; i++)
            {
                ref readonly var m = ref minions[i];
                if (m.Asc == null || !m.Asc.HasTag(GameplayTag.Telegraph)) continue;

                var currentAbility = m.Asc.CurrentAbility;
                if (currentAbility == null) continue;

                ref readonly var t = ref trans[i];
                ref readonly var s = ref sprites[i];
                ref readonly var c = ref cols[i];

                Vec2f finalTarget;
                if (m.TargetPos != Vec2f.Zero)
                {
                    finalTarget = m.TargetPos;
                }
                else
                {
                    var nearest = Vec2f.Zero;
                    var minDistSq = float.MaxValue;
                    for (var p = 0; p < players.Count; p++)
                    {
                        var d = (players[p] - t.Position).LengthSquared();
                        if (d < minDistSq)
                        {
                            minDistSq = d;
                            nearest = players[p];
                        }
                    }
                    finalTarget = nearest;
                }

                AbilityTelegraphContext tCtx = new(
                    t.Position,
                    finalTarget,
                    !s.FlipX,
                    m.Asc.Progress,
                    m.HoverTimer,
                    c.Size.Y,
                    bossFx,
                    arenaCfg);

                currentAbility.DrawTelegraph(in tCtx);
            }
        });
    }
}
