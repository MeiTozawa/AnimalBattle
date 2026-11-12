using AnimalBattle.Ai;
using AnimalBattle.Ai.BossSnake;
using AnimalBattle.Ai.BossLion;
using AnimalBattle.Maps;

namespace AnimalBattle.Scenes.Battle;

/// <summary>
///     World 単位で独立して保持される Boss AI およびカメラ更新用コンテキスト。
/// </summary>
internal sealed class BattleAiSystemContext : IDisposable
{
    public required Query<TileMap> TileMapQuery { get; init; }
    public required Query<Transform, Velocity> PlayerQuery { get; init; }
    public required Query AlivePlayerQuery { get; init; }
    public required Query<Transform, Velocity, CollisionBox, Health, Sprite, BossCombatState, BossBrainComponent, CombatProfile> BossQuery { get; init; }
    public required Query CanBossMoveQuery { get; init; }
    public required Query CanBossAttackQuery { get; init; }

    public Vec2f CachedPlayerPos;
    public Vec2f CachedPlayerVel;
    public ulong CachedPlayerEntityId;
    public bool IsPlayerAlive;
    public bool HasPlayerEverSpawned;
    public bool FoundPlayerThisFrame;
    public float CachedDeltaTime;
    public Vec2f CachedBossPos;
    public bool HasBoss;
    public bool CanBossMove;
    public bool CanBossAttack;
    public TileMap? CachedTileMap;
    public readonly List<int> CandidateBossPlatforms = new(16);

    public void Dispose()
    {
        TileMapQuery.Dispose();
        PlayerQuery.Dispose();
        AlivePlayerQuery.Dispose();
        BossQuery.Dispose();
        CanBossMoveQuery.Dispose();
        CanBossAttackQuery.Dispose();
    }
}

/// <summary>
///     戦闘シーンにおける Boss AI の意思決定および動的カメラ追従を処理する純粋 ECS システム。
///     GameState.Battle 状態時にアクティブとなり、OnUpdate フェーズで自機と Boss の状態同期・AI更新を実行します。
/// </summary>
public static class BattleAiSystem
{
    /// <summary>
    ///     Boss AI 思考および動的カメラ追従システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddBattleAiSystem(this World world)
    {
        var tileMapQuery = world.QueryBuilder<TileMap>().Build();

        // プレイヤーエンティティの追跡用クエリ
        var playerQuery = world.QueryBuilder<Transform, Velocity>()
            .With<PlayerTag>()
            .Build();

        // 生存プレイヤー判定用クエリ：DeadTag 除外をアーキタイプ層で高速判定（Entity.Has 撲滅）
        var alivePlayerQuery = world.QueryBuilder()
            .With<PlayerTag>()
            .Without<DeadTag>()
            .Build();

        // Boss 更新用クエリ：HitstopState 中の凍結および DeadTag 除外を徹底
        var bossQuery = world.QueryBuilder<Transform, Velocity, CollisionBox, Health, Sprite, BossCombatState, BossBrainComponent, CombatProfile>()
            .With<BossTag>()
            .Without<HitstopState>()
            .Without<DeadTag>()
            .Build();

        // 行動可能判定クエリ：CannotMoveTag および CannotAttackTag の有無をアーキタイプ層で高速判定
        var canBossMoveQuery = world.QueryBuilder()
            .With<BossTag>()
            .Without<DeadTag>()
            .Without<HitstopState>()
            .Without<CannotMoveTag>()
            .Build();

        var canBossAttackQuery = world.QueryBuilder()
            .With<BossTag>()
            .Without<DeadTag>()
            .Without<HitstopState>()
            .Without<CannotAttackTag>()
            .Build();

        world.SetDisposable(new BattleAiSystemContext
        {
            TileMapQuery = tileMapQuery,
            PlayerQuery = playerQuery,
            AlivePlayerQuery = alivePlayerQuery,
            BossQuery = bossQuery,
            CanBossMoveQuery = canBossMoveQuery,
            CanBossAttackQuery = canBossAttackQuery
        });

        world.System("BattleAiSystem")
            .Kind(Ecs.OnUpdate)
            .Iter(ExecuteBattleAi);

        return world;
    }

    private static void ExecuteBattleAi(Iter it)
    {
        var w = it.World();
        ref readonly var flow = ref w.Get<GameFlow>();
        if (flow.State != GameState.Battle) return;

        var ctx = w.Get<BattleAiSystemContext>();

        // プレイヤーの生存・座標状態を更新
        ctx.FoundPlayerThisFrame = false;
        ctx.PlayerQuery.Iter(CollectPlayerState);
        var gameOver = w.Has<GameOverTag>();
        ctx.IsPlayerAlive = ctx.AlivePlayerQuery.Count() > 0 && !gameOver;

        if (!ctx.FoundPlayerThisFrame && !ctx.HasPlayerEverSpawned) return;

        var dt = it.DeltaTime();
        var camCfg = w.Get<CameraConfig>();

        ctx.CachedDeltaTime = dt;
        ctx.CachedTileMap = null;
        ctx.TileMapQuery.Iter(ExtractTileMap);

        // Boss の移動・攻撃許可状態をアーキタイプクエリから即時判定（Entity.Has 撲滅）
        ctx.CanBossMove = ctx.CanBossMoveQuery.Count() > 0;
        ctx.CanBossAttack = ctx.CanBossAttackQuery.Count() > 0;

        ctx.HasBoss = false;
        ctx.BossQuery.Iter(UpdateBossAi);
        ctx.CachedTileMap = null;

        // ボスが Hitstop 中・死亡演出中・または訓練場等で BossQuery から除外されている場合でも、
        // 直近の BossPos または PlayerPos を使用してカメラ追従・振動減衰（Trauma decay）を継続する
        var targetBossPos = ctx.HasBoss ? ctx.CachedBossPos : ctx.CachedPlayerPos;
        CameraService.Update(
            dt,
            ctx.CachedPlayerPos,
            ctx.CachedPlayerVel,
            targetBossPos,
            camCfg);
    }

    /// <summary>
    ///     プレイヤー状態を無割当：Zero Allocationで収集します。
    /// </summary>
    private static void CollectPlayerState(Iter it, Field<Transform> transforms, Field<Velocity> velocities)
    {
        var count = it.Count();
        if (count == 0) return;

        var w = it.World();
        var ctx = w.Get<BattleAiSystemContext>();

        for (var i = 0; i < count; i++)
        {
            var pe = it.Entity(i);
            ctx.FoundPlayerThisFrame = true;
            ctx.HasPlayerEverSpawned = true;
            ctx.CachedPlayerPos = transforms[i].Position;
            ctx.CachedPlayerVel = velocities[i].Value;
            ctx.CachedPlayerEntityId = pe.Id;
        }
    }

    /// <summary>
    ///     TileMap インスタンスを無割当：Zero Allocationで抽出します。
    /// </summary>
    private static void ExtractTileMap(Iter it, Field<TileMap> tileMaps)
    {
        var count = it.Count();
        if (count == 0) return;
        it.World().Get<BattleAiSystemContext>().CachedTileMap = tileMaps[count - 1];
    }

    /// <summary>
    ///     Boss AI および物理挙動を無割当：Zero Allocationで更新します。
    /// </summary>
    private static void UpdateBossAi(
        Iter it,
        Field<Transform> transforms,
        Field<Velocity> velocities,
        Field<CollisionBox> cols,
        Field<Health> hps,
        Field<Sprite> sprites,
        Field<BossCombatState> states,
        Field<BossBrainComponent> brainComps,
        Field<CombatProfile> profiles)
    {
        var count = it.Count();
        if (count == 0) return;

        var w = it.World();
        var ctx = w.Get<BattleAiSystemContext>();
        var dt = ctx.CachedDeltaTime;
        var tileMap = ctx.CachedTileMap;

        for (var i = 0; i < count; i++)
        {
            var e = it.Entity(i);
            ref var trans = ref transforms[i];
            ref var vel = ref velocities[i];
            ref var col = ref cols[i];
            ref var hp = ref hps[i];
            ref var sprite = ref sprites[i];
            ref var state = ref states[i];
            ref var brainComp = ref brainComps[i];
            ref var profile = ref profiles[i];

            var brain = brainComp.Brain;

            ctx.HasBoss = true;
            ctx.CachedBossPos = trans.Position;

            var bossCanMove = ctx.CanBossMove;
            var effectivePlayerAlive = ctx.IsPlayerAlive && ctx.CanBossAttack;

            if (state.BossType == BossType.BossSnake && brain is BossSnakeBrain bSnakeBrain)
            {
                var bSnakeCfg = w.Get<BossSnakeConfig>();
                var arenaCfg = w.Get<ArenaConfig>();

                // 生存フラグおよびプレイヤーエンティティIDを動的に渡す
                bSnakeBrain.Update(dt, ctx.CachedPlayerPos, ctx.CachedPlayerEntityId, effectivePlayerAlive, hp.Current, hp.Max);

                // 外力ノックバック変位の適用および減衰処理
                if (vel.Knockback != Vec2f.Zero)
                {
                    bSnakeBrain.ApplyKnockback(vel.Knockback * dt);
                    var decay = bSnakeCfg.KnockbackDeceleration > 0.0f ? bSnakeCfg.KnockbackDeceleration : 1200.0f;
                    var kx = PhysicsService.MoveTowards(vel.Knockback.X, 0.0f, decay * dt);
                    var ky = PhysicsService.MoveTowards(vel.Knockback.Y, 0.0f, decay * dt);
                    vel.Knockback = new Vec2f(kx, ky);
                }

                var colSize = col.Size != Vec2f.Zero ? col.Size : bSnakeCfg.CollisionSize;
                var minX = arenaCfg.BoundaryMinX + colSize.X * 0.5f;
                var maxX = arenaCfg.BoundaryMaxX - colSize.X * 0.5f;
                var floorY = arenaCfg.FloorLimitY - colSize.Y * 0.5f;
                var minY = colSize.Y * 0.5f;
                bSnakeBrain.ClampPosition(minX, maxX, minY, floorY);

                if (!bossCanMove)
                {
                    // 移動不許可時は初期スポーン位置を維持
                    bSnakeBrain.ClampPosition(bSnakeCfg.Spawn.X, bSnakeCfg.Spawn.X, bSnakeCfg.Spawn.Y, bSnakeCfg.Spawn.Y);
                }

                trans.Position = bSnakeBrain.Position;
                sprite.FlipX = !bSnakeBrain.FacingRight;

                var fx = w.Get<BossEffectsRenderConfig>();
                trans.Rotation = (float)(MathF.Sin(bSnakeBrain.HoverTimer * fx.BossHoverAngleSpeed) * fx.BossHoverAngleAmp);
            }
            else if (state.BossType == BossType.BossLion && brain is BossLionBrain bLionBrain)
            {
                var isGrounded = col.IsGrounded;
                // 生存フラグを動的に渡す
                bLionBrain.Update(dt, ctx.CachedPlayerPos, ctx.CachedPlayerVel, effectivePlayerAlive, isGrounded, hp.Current, hp.Max, trans.Position);

                var currentLinear = vel.Value;
                var brainVx = bossCanMove ? bLionBrain.Velocity.X : 0.0f;
                currentLinear = new Vec2f(brainVx, currentLinear.Y);
                var isControlledAirborne = bLionBrain.State == BossLionState.PlatformTraverse
                    || (bLionBrain.AbilitySystem.IsCasting && bLionBrain.CurrentTimelineAttack?.Motion.Kind == SkillMotionKind.LeapSmash);

                if (bossCanMove && (isControlledAirborne || isGrounded))
                {
                    currentLinear = new Vec2f(currentLinear.X, bLionBrain.Velocity.Y);
                }
                vel.Value = currentLinear;

                UpdateBossLionMovementAndCollision(bLionBrain, ref trans, ref vel, ref col, dt, w, tileMap, isControlledAirborne, ctx.CandidateBossPlatforms);
                sprite.FlipX = !bLionBrain.FacingRight;
            }

            // BossCombatState の同期
            state.Phase = brain.Phase;
            profile.Phase = brain.Phase;
            state.CurrentSkillTag = brain.CurrentSkillTag;
            state.StateTimer = brain.StateTimer;
            state.TargetPosition = brain.TargetPosition;
            state.FacingRight = brain.FacingRight;
        }
    }

    private static void UpdateBossLionMovementAndCollision(
        BossLionBrain brain,
        ref Transform trans,
        ref Velocity vel,
        ref CollisionBox col,
        float dt,
        World world,
        TileMap? tileMap,
        bool isControlledAirborne,
        List<int> candidateBossPlatforms)
    {
        var bLionCfg = world.Get<BossLionConfig>();
        var arenaCfg = world.Get<ArenaConfig>();

        var linear = vel.Value;

        // 空中落下物理：PlatformTraverse や LeapSmash などの制御空中移動時を除き、通常重力加速度を適用
        if (!col.IsGrounded && !isControlledAirborne)
        {
            var curVy = linear.Y;
            PhysicsService.ApplyGravity(ref curVy, bLionCfg.Gravity, bLionCfg.MaxFallSpeed, dt);
            linear = new Vec2f(linear.X, curVy);
            vel.Value = linear;
        }

        var colSize = col.Size != Vec2f.Zero ? col.Size : bLionCfg.CollisionSize;

        var minX = arenaCfg.BoundaryMinX + colSize.X * 0.5f;
        var maxX = arenaCfg.BoundaryMaxX - colSize.X * 0.5f;

        // 外力ノックバック速度の減衰処理
        if (vel.Knockback != Vec2f.Zero)
        {
            var decay = bLionCfg.KnockbackDeceleration > 0.0f ? bLionCfg.KnockbackDeceleration : 1200.0f;
            var kx = PhysicsService.MoveTowards(vel.Knockback.X, 0.0f, decay * dt);
            var ky = PhysicsService.MoveTowards(vel.Knockback.Y, 0.0f, decay * dt);
            vel.Knockback = new Vec2f(kx, ky);
        }

        var totalLinear = linear + vel.Knockback;
        var nextX = Math.Clamp(trans.Position.X + totalLinear.X * dt, minX, maxX);
        var nextY = trans.Position.Y + totalLinear.Y * dt;
        var nextPos = new Vec2f(nextX, nextY);

        var currentY = trans.Position.Y;
        Box2D bossBox = new(nextPos, colSize);

        var hitPlatform = false;
        var platformGroundY = float.MaxValue;
        var platformVy = 0.0f;

        // 制御空中移動時かつ上昇・滞空：vel.Linear.Y <= 0の間は足場着地判定をスキップ
        var ignorePlatforms = isControlledAirborne && linear.Y <= 0.0f;
        if (!ignorePlatforms && tileMap != null)
        {
            var isLeapSmash = brain.CurrentTimelineAttack?.Motion.Kind == SkillMotionKind.LeapSmash;
            var targetLandingSurfaceY = brain.TargetLandingSurfaceY;

            var sweepTop = MathF.Min(currentY, nextY) - colSize.Y * 0.5f;
            var sweepBottom = MathF.Max(currentY, nextY) + colSize.Y * 0.5f;
            var sweepCenter = new Vec2f(nextX, (sweepTop + sweepBottom) * 0.5f);
            var sweepSize = new Vec2f(colSize.X, sweepBottom - sweepTop);
            Box2D sweepQueryBox = new(sweepCenter, sweepSize);

            candidateBossPlatforms.Clear();
            tileMap.SpatialHash.Query(sweepQueryBox, candidateBossPlatforms);

            for (var p = 0; p < candidateBossPlatforms.Count; p++)
            {
                if (tileMap.SpatialHash.TryGetBox(candidateBossPlatforms[p], out var platformBox))
                {
                    var platTopY = platformBox.Top;

                    // ヒップドロップ：LeapSmash急降下中：目標着地サーフェス：足場上面または床より上層の足場は貫通：スキップ
                    if (isLeapSmash && targetLandingSurfaceY > 0.0f && platTopY < targetLandingSurfaceY - 5.0f)
                    {
                        continue;
                    }

                    if (PhysicsService.CheckPlatformGround(currentY, nextY, bossBox, platformBox, out var groundY, out var pVy))
                    {
                        if (!hitPlatform || groundY < platformGroundY)
                        {
                            hitPlatform = true;
                            platformGroundY = groundY;
                            platformVy = pVy;
                        }
                    }
                }
            }
        }

        var isGrounded = hitPlatform;
        var adjustedPosY = hitPlatform ? platformGroundY : nextY;
        var adjustedVy = hitPlatform ? platformVy : linear.Y;

        var floorLimitY = arenaCfg.FloorLimitY - colSize.Y * 0.5f;
        if (adjustedPosY >= floorLimitY)
        {
            isGrounded = true;
            adjustedPosY = floorLimitY;
            adjustedVy = 0.0f;
        }

        if (isGrounded && vel.Knockback.Y > 0.0f)
        {
            vel.Knockback = new Vec2f(vel.Knockback.X, 0.0f);
        }

        trans.Position = new Vec2f(nextX, adjustedPosY);
        vel.Value = new Vec2f(linear.X, adjustedVy);
        col.IsGrounded = isGrounded;
    }
}
